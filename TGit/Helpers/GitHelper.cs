using Community.VisualStudio.Toolkit;
using Microsoft.Win32;
using TGit.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TGit.Helpers
{
    public static class GitHelper
    {
        private static GitConfig _gitConfig;
        private static string _solutionFullPath;

        public static async Task<string> GetCommitMessage(string commitMessageTemplate)
        {
            if (string.IsNullOrEmpty(commitMessageTemplate))
            {
                return string.Empty;
            }

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            var version = await VS.Shell.GetVsVersionAsync();

            var commitMessage = commitMessageTemplate;
            commitMessage = commitMessage.Replace("$(BranchName)", await GetCurrentBranchName(false));
            commitMessage = commitMessage.Replace("$(FeatureName)", await GetCurrentBranchName(true));
            commitMessage = commitMessage.Replace("$(DevEnvDir)", (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\SxS\\VS7\\", version.ToString(), string.Empty));
            commitMessage = commitMessage.Replace("$(SolutionDir)", Path.GetDirectoryName(solution?.FullPath));
            commitMessage = commitMessage.Replace("$(SolutionPath)", string.IsNullOrEmpty(solution?.FullPath) ? string.Empty : Path.GetFullPath(solution.FullPath));
            commitMessage = commitMessage.Replace("$(SolutionName)", solution?.Name);
            commitMessage = commitMessage.Replace("$(SolutionFileName)", Path.GetFileName(solution?.FullPath));
            commitMessage = commitMessage.Replace("$(SolutionExt)", Path.GetExtension(solution?.FullPath));
            commitMessage = commitMessage.Replace("$(VSInstallDir)", (string)Registry.GetValue($"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\{version}", "InstallDir", string.Empty));
            commitMessage = commitMessage.Replace("$(FxCopDir)", (string)Registry.GetValue($"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\{version}\\Edev", "FxCopDir", string.Empty));
            return commitMessage;
        }

        public static async Task<string> GetCurrentBranchName(bool trimPrefix)
        {
            var branchName = await ProcessHelper.GetGitOutputAsync("symbolic-ref", "-q", "--short", "HEAD");

            if (string.IsNullOrEmpty(branchName))
            {
                return string.Empty;
            }

            var gitConfig = await GetGitConfig();

            if (!string.IsNullOrEmpty(gitConfig.FeaturePrefix) && branchName.StartsWith(gitConfig.FeaturePrefix, StringComparison.Ordinal) && trimPrefix)
            {
                return branchName.Substring(gitConfig.FeaturePrefix.Length);
            }
            if (!string.IsNullOrEmpty(gitConfig.ReleasePrefix) && branchName.StartsWith(gitConfig.ReleasePrefix, StringComparison.Ordinal) && trimPrefix)
            {
                return branchName.Substring(gitConfig.ReleasePrefix.Length);
            }
            if (!string.IsNullOrEmpty(gitConfig.HotfixPrefix) && branchName.StartsWith(gitConfig.HotfixPrefix, StringComparison.Ordinal) && trimPrefix)
            {
                return branchName.Substring(gitConfig.HotfixPrefix.Length);
            }

            return branchName;
        }

        public static async Task<IDictionary<string, string>> GetSshEnvironment(string workingDir)
        {
            var result = await ProcessHelper.QueryGitAsync(workingDir, "config", "--get", "remote.origin.puttykeyfile");
            var remoteOriginPuttyKeyfile = result.Succeeded ? result.Output.TrimEnd('\r', '\n') : string.Empty;

            if (string.IsNullOrEmpty(remoteOriginPuttyKeyfile))
            {
                return null;
            }

            using (Process.Start(new ProcessStartInfo
            {
                FileName = "pageant.exe",
                Arguments = GitProcess.QuoteArgument(remoteOriginPuttyKeyfile),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            })) { }
            return new Dictionary<string, string> { { "GIT_SSH", FileHelper.GetTortoiseGitPlink() } };
        }

        /// <summary>
        /// Get GIT config.
        /// </summary>
        /// <remarks>
        /// - Cache received config based on the currently opened solution file path.
        /// - If the solution file path is different from last time we got the git config,
        ///   it means we have a new solution and should also get new git config.
        /// </remarks>
        /// <returns>Strong typed GIT config about gitflow, svn and bugtraq.</returns>
        public static async Task<GitConfig> GetGitConfig(bool refresh = false)
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();

            if (refresh || _gitConfig == null ||
                string.IsNullOrEmpty(_solutionFullPath) ||
                !_solutionFullPath.Equals(solution?.FullPath))
            {
                _gitConfig = new GitConfig(await ProcessHelper.GetGitOutputAsync("config", "--get-regexp", "gitflow|bugtraq|svn-remote"));
                _solutionFullPath = solution?.FullPath;
            }

            return _gitConfig;
        }

        public static void InvalidateGitConfig()
        {
            _gitConfig = null;
            _solutionFullPath = null;
        }

        public static Task<bool> RemoteBranchExists(string branch)
            => ProcessHelper.GitSucceededAsync("show-ref", "--verify", "--quiet", "refs/remotes/origin/" + branch);

        public static async Task<bool> ValidateBranchName(string branch)
        {
            if (!string.IsNullOrEmpty(branch) && !branch.StartsWith("-", StringComparison.Ordinal) &&
                !branch.Contains("@{") && await ProcessHelper.GitSucceededAsync("check-ref-format", "--branch", branch)) return true;
            await VS.MessageBox.ShowErrorAsync("TGit error", "Invalid branch name: " + branch);
            return false;
        }

        public static async Task<bool> ValidateTagName(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !tag.StartsWith("-", StringComparison.Ordinal) &&
                await ProcessHelper.GitSucceededAsync("check-ref-format", "refs/tags/" + tag)) return true;
            await VS.MessageBox.ShowErrorAsync("TGit error", "Invalid tag name: " + tag);
            return false;
        }

        public static async Task<string> GetDefaultBranchName()
        {
            var remoteHead = await ProcessHelper.GetGitOutputAsync("symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD");
            if (remoteHead.StartsWith("origin/", StringComparison.Ordinal)) return remoteHead.Substring("origin/".Length);
            return await ProcessHelper.GitSucceededAsync("show-ref", "--verify", "--quiet", "refs/heads/main") ? "main" : "master";
        }

        /// <summary>
        /// Check if GitFlow is initialized
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> IsGitFlow()
        {
            var gitConfig = await GetGitConfig();
            return !string.IsNullOrEmpty(gitConfig.MasterBranch);
        }

        /// <summary>
        /// Check if GitFlow is initialized
        /// </summary>
        /// <returns></returns>
        public static bool IsGitFlow(GitConfig gitConfig)
            => !string.IsNullOrEmpty(gitConfig.MasterBranch);

        /// <summary>
        /// Check if Git SVN is used for this repo
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> IsGitSvn()
        {
            var gitConfig = await GetGitConfig();
            return !string.IsNullOrEmpty(gitConfig.SvnUrl);
        }

        /// <summary>
        /// Do we have any stashes available
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> HasStash()
            => !string.IsNullOrEmpty(await ProcessHelper.GetGitOutputAsync("stash", "list"));
    }
}
