using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.finishFeature)]
    internal sealed class GitFlowFinishFeatureCommand : BaseCommand<GitFlowFinishFeatureCommand>
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
            if (string.IsNullOrEmpty(branch) || !branch.StartsWith(config.FeaturePrefix, StringComparison.Ordinal) ||
                branch == config.MasterBranch || branch == config.DevelopBranch)
            {
                await VS.MessageBox.ShowErrorAsync("Current branch is not a feature branch.");
                return;
            }
            if (!await GitHelper.ValidateBranchName(config.DevelopBranch) || !await GitHelper.ValidateBranchName(branch)) return;
            await CommandHelper.SaveFiles();
            var options = await General.GetLiveInstanceAsync();
            await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.Finish(config.DevelopBranch, branch, options.PullChanges),
                "Finishing feature " + branch.Substring(config.FeaturePrefix.Length), branch,
                new[] { new[] { "push", "origin", config.DevelopBranch } });
        }

    }
}
