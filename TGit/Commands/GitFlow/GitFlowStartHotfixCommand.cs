using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.startHotfix)]
    internal sealed class GitFlowStartHotfixCommand : BaseCommand<GitFlowStartHotfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var version = Interaction.InputBox("Hotfix Version:", "Start New Hotfix");
            if (string.IsNullOrEmpty(version)) return;
            var config = await GitHelper.GetGitConfig(refresh: true);
            if (!GitHelper.IsGitFlow(config))
            {
                await VS.MessageBox.ShowErrorAsync("GitFlow is not initialized.");
                return;
            }
            var branch = config.HotfixPrefix + version;
            if (!await GitHelper.ValidateBranchName(config.MasterBranch) || !await GitHelper.ValidateBranchName(branch)) return;
            await CommandHelper.SaveFiles();
            var options = await General.GetLiveInstanceAsync();
            await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.Start(config.MasterBranch, branch, options.PullChanges),
                "Starting hotfix " + version);
        }
        protected override void BeforeQueryStatus(EventArgs e)
            => CommandHelper.RunAsync(async () => Command.Visible =
                await FileHelper.HasSolutionDir() && await GitHelper.IsGitFlow(), false);
    }
}
