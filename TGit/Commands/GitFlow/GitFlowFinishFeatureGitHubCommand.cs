using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.finishFeatureGitHub)]
    internal sealed class GitFlowFinishFeatureGitHubCommand : BaseCommand<GitFlowFinishFeatureGitHubCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var branch = await GitHelper.GetCurrentBranchName(false);
            var baseBranch = await GitHelper.GetDefaultBranchName();
            if (string.IsNullOrEmpty(branch) || branch == baseBranch)
            {
                await VS.MessageBox.ShowErrorAsync("Switch to a feature branch before finishing it.");
                return;
            }
            if (!await GitHelper.ValidateBranchName(baseBranch) || !await GitHelper.ValidateBranchName(branch)) return;
            await CommandHelper.SaveFiles();
            var options = await General.GetLiveInstanceAsync();
            await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.Finish(baseBranch, branch, options.PullChanges),
                "Finishing feature " + branch, branch,
                new[] { new[] { "push", "origin", baseBranch } });
        }

    }
}
