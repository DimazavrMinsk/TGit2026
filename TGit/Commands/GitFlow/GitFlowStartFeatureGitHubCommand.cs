using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.startFeatureGitHub)]
    internal sealed class GitFlowStartFeatureGitHubCommand : BaseCommand<GitFlowStartFeatureGitHubCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var featureName = Interaction.InputBox("Feature Name:", "Start New Feature");
            if (string.IsNullOrEmpty(featureName)) return;
            var baseBranch = await GitHelper.GetDefaultBranchName();
            if (!await GitHelper.ValidateBranchName(baseBranch) || !await GitHelper.ValidateBranchName(featureName)) return;
            await CommandHelper.SaveFiles();
            var options = await General.GetLiveInstanceAsync();
            await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.Start(baseBranch, featureName, options.PullChanges),
                "Starting feature " + featureName);
        }

    }
}
