using System;

namespace TGit.Models
{
    public class GitConfig
    {
        public string MasterBranch = string.Empty;
        public string DevelopBranch = string.Empty;
        public string FeaturePrefix = string.Empty;
        public string ReleasePrefix = string.Empty;
        public string HotfixPrefix = string.Empty;
        public string TagPrefix = string.Empty;
        public string BugTraqMessage = string.Empty;
        public string SvnUrl = string.Empty;

        public GitConfig() { }

        public GitConfig(string input)
        {
            foreach (var line in (input ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = line.IndexOfAny(new[] { ' ', '\t' });
                if (separator < 0) continue;
                var key = line.Substring(0, separator);
                var value = line.Substring(separator + 1);
                switch (key)
                {
                    case "gitflow.branch.master": MasterBranch = value; break;
                    case "gitflow.branch.develop": DevelopBranch = value; break;
                    case "gitflow.prefix.feature": FeaturePrefix = value; break;
                    case "gitflow.prefix.release": ReleasePrefix = value; break;
                    case "gitflow.prefix.hotfix": HotfixPrefix = value; break;
                    case "gitflow.prefix.versiontag": TagPrefix = value; break;
                    case "bugtraq.message": BugTraqMessage = value; break;
                    case "svn-remote.svn.url": SvnUrl = value; break;
                }
            }
        }
    }
}
