using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.finishHotfix)]
    internal sealed class GitFlowFinishHotfixCommand : BaseCommand<GitFlowFinishHotfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var config = await GitHelper.GetGitConfig(refresh: true);
            if (!GitHelper.IsGitFlow(config))
            {
                await VS.MessageBox.ShowErrorAsync("GitFlow is not initialized.");
                return;
            }
            var branch = await GitHelper.GetCurrentBranchName(false);
            if (string.IsNullOrEmpty(branch) || !branch.StartsWith(config.HotfixPrefix, StringComparison.Ordinal) ||
                branch == config.MasterBranch || branch == config.DevelopBranch)
            {
                await VS.MessageBox.ShowErrorAsync("Current branch is not a hotfix branch.");
                return;
            }
            var version = branch.Substring(config.HotfixPrefix.Length);
            var tag = config.TagPrefix + version;
            if (!await GitHelper.ValidateBranchName(config.MasterBranch) || !await GitHelper.ValidateBranchName(config.DevelopBranch) ||
                !await GitHelper.ValidateBranchName(branch) || !await GitHelper.ValidateTagName(tag)) return;
            var options = await General.GetLiveInstanceAsync();
            var message = options.UseAnnotatedTag ? Interaction.InputBox("Tag message:", "Finish hotfix") : null;
            if (options.UseAnnotatedTag && string.IsNullOrEmpty(message)) return;
            await CommandHelper.SaveFiles();
            await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.FinishVersion(config.MasterBranch, config.DevelopBranch,
                branch, tag, message, options.PullChanges), "Finishing hotfix " + version, branch,
                new[] { new[] { "push", "origin", config.DevelopBranch }, new[] { "push", "origin", config.MasterBranch },
                    new[] { "push", "origin", "refs/tags/" + tag } });
        }

    }
}
