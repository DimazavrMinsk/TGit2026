using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.diskBrowser)]
    internal sealed class DiskBrowserCommand : BaseCommand<DiskBrowserCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CommandHelper.SaveFiles();

            ProcessHelper.Start(await FileHelper.GetSolutionDir());
        }
    }
}
