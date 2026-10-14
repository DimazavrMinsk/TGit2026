using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TGit.Commands
{
    [Command(PackageGuids.guidTGitCmdSetString, PackageIds.commitContext)]
    internal sealed class CommitFileCommand : BaseCommand<CommitFileCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CommandHelper.SaveFiles();

            var options = await General.GetLiveInstanceAsync();
            var commitMessage = await GitHelper.GetCommitMessage(options.CommitMessage);
            var bugId = await GitHelper.GetCommitMessage(options.BugId);
            var gitConfig = await GitHelper.GetGitConfig();

            var args = $"{(string.IsNullOrEmpty(commitMessage) ? string.Empty : $"/logmsg:\"{commitMessage}\"")} " +
                $"{(!string.IsNullOrEmpty(bugId) && !string.IsNullOrEmpty(gitConfig.BugTraqMessage) ? $"/bugid:\"{bugId}\"" : string.Empty)}";

            await ProcessHelper.RunTortoiseGitFileCommand("commit", args);
        }
    }
}
