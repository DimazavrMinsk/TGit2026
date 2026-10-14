using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.init)]
    internal sealed class GitFlowInitCommand : BaseCommand<GitFlowInitCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            using (var dialog = new FlowDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                var config = dialog.GitConfig;
                var localExists = await ProcessHelper.GitSucceededAsync("show-ref", "--verify", "--quiet", "refs/heads/" + config.DevelopBranch);
                var remoteExists = await GitHelper.RemoteBranchExists(config.DevelopBranch);
                await CommandHelper.SaveFiles();
                try
                {
                    await ProcessHelper.RunGitCommandsAsync(GitFlowPlan.Configure(config, localExists, remoteExists), "Configuring GitFlow");
                }
                finally
                {
                    GitHelper.InvalidateGitConfig();
                }
            }
        }
        protected override void BeforeQueryStatus(EventArgs e)
            => CommandHelper.RunAsync(async () =>
            {
                Command.Enabled = await FileHelper.HasSolutionDir();
                Command.Text = await GitHelper.IsGitFlow() ? "Config" : "Init";
            }, false);
    }
}
