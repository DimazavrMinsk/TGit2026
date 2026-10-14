using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.deleteKeepContext)]
    internal sealed class DeleteKeepFileCommand : BaseCommand<DeleteKeepFileCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ProcessHelper.RunTortoiseGitFileCommand("remove", "/keep");
        }
    }
}
