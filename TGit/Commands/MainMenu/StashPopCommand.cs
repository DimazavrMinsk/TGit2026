using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.stashPop)]
    internal sealed class StashPopCommand : BaseCommand<StashPopCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CommandHelper.SaveFiles();

            await ProcessHelper.RunTortoiseGitCommand("stashpop");
        }

        protected override void BeforeQueryStatus(EventArgs e)
            => CommandHelper.RunAsync(async () => Command.Enabled = await GitHelper.HasStash(), false);
    }
}
