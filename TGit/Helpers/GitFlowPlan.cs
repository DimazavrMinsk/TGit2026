using System.Collections.Generic;
using TGit.Models;

namespace TGit.Helpers
{
    public static class GitFlowPlan
    {
        public static List<string[]> Start(string baseBranch, string newBranch, bool pull)
        {
            var commands = new List<string[]> { new[] { "checkout", baseBranch } };
            if (pull) commands.Add(new[] { "pull" });
            commands.Add(new[] { "checkout", "-b", newBranch, baseBranch });
            return commands;
        }

        public static List<string[]> Finish(string targetBranch, string sourceBranch, bool pull)
        {
            var commands = new List<string[]> { new[] { "checkout", targetBranch } };
            if (pull) commands.Add(new[] { "pull" });
            commands.Add(new[] { "merge", "--no-ff", "--no-edit", sourceBranch });
            return commands;
        }

        public static List<string[]> FinishVersion(string master, string develop, string branch,
            string tag, string tagMessage, bool pull)
        {
            var commands = Finish(master, branch, pull);
            commands.Add(tagMessage == null ? new[] { "tag", tag } : new[] { "tag", "-a", tag, "-m", tagMessage });
            commands.AddRange(Finish(develop, branch, pull));
            return commands;
        }

        public static List<string[]> Configure(GitConfig config, bool localDevelopExists, bool remoteDevelopExists)
        {
            var commands = new List<string[]>
            {
                new[] { "config", "--replace-all", "gitflow.branch.master", config.MasterBranch },
                new[] { "config", "--replace-all", "gitflow.branch.develop", config.DevelopBranch },
                new[] { "config", "--replace-all", "gitflow.prefix.feature", config.FeaturePrefix },
                new[] { "config", "--replace-all", "gitflow.prefix.release", config.ReleasePrefix },
                new[] { "config", "--replace-all", "gitflow.prefix.hotfix", config.HotfixPrefix },
                new[] { "config", "--replace-all", "gitflow.prefix.versiontag", config.TagPrefix }
            };
            if (!localDevelopExists)
            {
                commands.Add(remoteDevelopExists
                    ? new[] { "checkout", "-b", config.DevelopBranch, "--track", "origin/" + config.DevelopBranch }
                    : new[] { "checkout", "-b", config.DevelopBranch, config.MasterBranch });
            }
            return commands;
        }

        public static List<string[]> Cleanup(string branch, bool deleteLocal, bool deleteRemote,
            bool push, IEnumerable<string[]> pushCommands)
        {
            var commands = new List<string[]>();
            if (string.IsNullOrEmpty(branch)) return commands;
            if (push && pushCommands != null) commands.AddRange(pushCommands);
            if (deleteLocal) commands.Add(new[] { "branch", "-d", branch });
            if (deleteRemote) commands.Add(new[] { "push", "origin", "--delete", branch });
            return commands;
        }
    }
}
