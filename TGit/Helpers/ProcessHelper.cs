using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Helpers
{
    public static class ProcessHelper
    {
        public static async Task<GitProcessResult> QueryGitAsync(string workingDir, params string[] arguments)
        {
            if (string.IsNullOrEmpty(workingDir)) return new GitProcessResult(-1, string.Empty, string.Empty);
            var gitPath = FileHelper.GetMSysGit();
            try
            {
                return await GitProcess.RunAsync(gitPath, workingDir, arguments);
            }
            catch (Exception e) when (e is Win32Exception || e is IOException || e is InvalidOperationException)
            {
                var errorCode = e is Win32Exception win32 ? $" (Windows error {win32.NativeErrorCode})" : string.Empty;
                var error = $"Unable to run Git '{gitPath}' in '{workingDir}'{errorCode}. {e.Message}";
                ActivityLog.LogError("TGit", $"{error}{Environment.NewLine}{e}");
                return new GitProcessResult(-1, string.Empty, error);
            }
        }

        public static async Task<string> GetGitOutputAsync(params string[] arguments)
        {
            var result = await QueryGitAsync(await FileHelper.GetSolutionDir(), arguments);
            return result.Succeeded ? result.Output.TrimEnd('\r', '\n') : string.Empty;
        }

        public static async Task<bool> GitSucceededAsync(params string[] arguments)
            => (await QueryGitAsync(await FileHelper.GetSolutionDir(), arguments)).Succeeded;

        public static async Task<bool> RunGitCommandsAsync(IEnumerable<string[]> commands, string title,
            string branchName = "", IEnumerable<string[]> pushCommands = null, OutputBox outputBox = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var workingDir = outputBox?.WorkingDirectory ?? await FileHelper.GetSolutionDir();
            if (string.IsNullOrEmpty(workingDir)) return false;

            var userName = await QueryGitAsync(workingDir, "config", "user.name");
            var userEmail = await QueryGitAsync(workingDir, "config", "user.email");
            if (userName.ExitCode == -1 || userEmail.ExitCode == -1)
            {
                await VS.MessageBox.ShowErrorAsync("TGit error", userName.Error + userEmail.Error);
                return false;
            }
            if (!userName.Succeeded || !userEmail.Succeeded ||
                string.IsNullOrWhiteSpace(userName.Output) || string.IsNullOrWhiteSpace(userEmail.Output))
            {
                using (var credentials = new Credentials(workingDir))
                {
                    if (credentials.ShowDialog() != DialogResult.OK) return false;
                }
            }

            var box = outputBox ?? new OutputBox(workingDir, branchName, pushCommands);
            if (box.IsDisposed) return false;
            if (!string.IsNullOrEmpty(title)) box.Text = title;
            if (!box.Visible) box.Show();
            box.BeginOperation();
            var stopwatch = Stopwatch.StartNew();
            var succeeded = false;
            try
            {
                var environment = await GitHelper.GetSshEnvironment(workingDir);
                succeeded = await GitProcess.RunSequenceAsync(FileHelper.GetMSysGit(), workingDir,
                    commands, environment, box.AppendLine);
                return succeeded;
            }
            catch (Exception e)
            {
                ActivityLog.LogError("TGit", e.ToString());
                box.AppendLine("Error: " + e.Message);
                return false;
            }
            finally
            {
                box.CompleteOperation(succeeded, stopwatch.ElapsedMilliseconds);
            }
        }

        public static async Task StartTortoiseGitProc(string args)
        {
            var tortoiseGitProc = FileHelper.GetTortoiseGitProc();
            try
            {
                Process.Start(tortoiseGitProc, args);
            }
            catch (Exception e)
            {
                await VS.MessageBox.ShowErrorAsync($"{tortoiseGitProc} not found", e.Message);
            }
        }

        public static void Start(string application) => Process.Start(application);

        // TortoiseGit parses /key:"value" itself and escapes embedded quotes by doubling them.
        public static string FormatTortoiseArgument(string name, string value)
            => "/" + name + ":\"" + value.Replace("\"", "\"\"") + "\"";

        public static async Task RunTortoiseGitCommand(string command, string args = "")
        {
            var solutionDir = await FileHelper.GetSolutionDir();
            if (!string.IsNullOrEmpty(solutionDir))
            {
                await RunTortoiseGitFileCommand(command, args, solutionDir);
            }
        }

        public static async Task RunTortoiseGitFileCommand(string command, string args = "", string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath)) filePath = await FileHelper.GetActiveDocumentFilePath();
            if (string.IsNullOrEmpty(filePath)) return;
            var options = await General.GetLiveInstanceAsync();
            await StartTortoiseGitProc($"/command:{command} {FormatTortoiseArgument("path", filePath)} {args} /closeonend:{options.CloseOnEnd}");
        }
    }
}
