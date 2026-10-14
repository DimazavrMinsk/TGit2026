using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.startRelease)]
    internal sealed class GitFlowStartReleaseCommand : BaseCommand<GitFlowStartReleaseCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var version = Interaction.InputBox("Release Version:", "Start New Release");
            if (string.IsNullOrEmpty(version)) return;
            var config = await GitHelper.GetGitConfig(refresh: true);
            if (!GitHelper.IsGitFlow(config))
            {
                await VS.MessageBox.ShowErrorAsync("GitFlow is not initialized.");
                return;
            }
            var branch = config.ReleasePrefix + version;
            if (!await GitHelper.ValidateBranchName(config.DevelopBranch) || !await GitHelper.ValidateBranchName(branch)) return;
            await CommandHelper.SaveFiles();
            var options = await General.GetLiveInstanceAsync();
            await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.Start(config.DevelopBranch, branch, options.PullChanges),
                "Starting release " + version);
        }
        protected override void BeforeQueryStatus(EventArgs e)
            => CommandHelper.RunAsync(async () => Command.Visible =
                await FileHelper.HasSolutionDir() && await GitHelper.IsGitFlow(), false);
    }
}
