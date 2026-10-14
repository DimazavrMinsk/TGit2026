using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace TGit.Helpers
{
    public static class CommandHelper
    {
        private static JoinableTaskFactory _taskFactory;

        public static void Initialize(JoinableTaskFactory taskFactory)
            => _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));

        public static void AddMenuCommand(IMenuCommandService menuCommandService, int commandId, EventHandler eventHandler)
        {
            var menuCommand = new OleMenuCommand(null, new CommandID(PackageGuids.guidTGitCmdSet, commandId));
            menuCommand.BeforeQueryStatus += eventHandler;
            menuCommandService.AddCommand(menuCommand);
        }

        public static void RunAsync(Func<Task> action, bool showAlert = true)
        {
            _taskFactory.RunAsync(async () =>
            {
                try
                {
                    await _taskFactory.SwitchToMainThreadAsync();
                    await action();
                }
                catch (Exception exception)
                {
                    ActivityLog.LogError("TGit", exception.ToString());
                    if (showAlert) await VS.MessageBox.ShowErrorAsync("TGit error", exception.Message);
                }
            }).FileAndForget("TGit/Command");
        }

        public static void SolutionVisibility_BeforeQueryStatus(object sender, EventArgs e)
            => RunAsync(async () => ((OleMenuCommand)sender).Visible = await FileHelper.HasSolutionDir(), false);

        public static void GitHubFlow_BeforeQueryStatus(object sender, EventArgs e)
            => RunAsync(async () => ((OleMenuCommand)sender).Visible =
                await FileHelper.HasSolutionDir() && !await GitHelper.IsGitFlow(), false);

        public static void GitSvn_BeforeQueryStatus(object sender, EventArgs e)
            => RunAsync(async () => ((OleMenuCommand)sender).Visible =
                await FileHelper.HasSolutionDir() && await GitHelper.IsGitSvn(), false);

        public static async Task<bool> SaveFiles()
        {
            var options = await General.GetLiveInstanceAsync();

            if (!options.SaveFiles)
            {
                return false;
            }

            return await KnownCommands.File_SaveAll.ExecuteAsync();
        }
    }
}
