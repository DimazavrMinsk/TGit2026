using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.prefDiffContext)]
    internal sealed class PrefDiffFileCommand : BaseCommand<PrefDiffFileCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CommandHelper.SaveFiles();

            var filePath = await FileHelper.GetActiveDocumentFilePath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            var exactFilePath = FileHelper.GetExactPathName(filePath);

            var result = await ProcessHelper.QueryGitAsync(Path.GetDirectoryName(exactFilePath),
                "log", "-2", "--pretty=format:%h", "--", Path.GetFileName(exactFilePath));
            var revisions = result.Output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (!result.Succeeded || revisions.Length < 2)
            {
                await VS.MessageBox.ShowErrorAsync("Could not determine the last committed revision!");
                return;
            }

            await ProcessHelper.RunTortoiseGitFileCommand("diff", $"/startrev:{revisions[1]} /endrev:{revisions[0]}", exactFilePath);
        }
    }
}
