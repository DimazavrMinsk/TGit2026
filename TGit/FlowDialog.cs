using TGit.Helpers;
using TGit.Models;
using System;
using System.Windows.Forms;

namespace TGit
{
    public partial class FlowDialog : Form
    {
        public GitConfig GitConfig;

        public FlowDialog()
        {
            InitializeComponent();
            okButton.DialogResult = DialogResult.None;

            Load += FlowDialog_Load;
        }

        private void FlowDialog_Load(object sender, EventArgs e)
            => CommandHelper.RunAsync(LoadOptionsAsync);

        private async System.Threading.Tasks.Task LoadOptionsAsync()
        {
            okButton.Enabled = false;
            var gitConfig = await GitHelper.GetGitConfig(refresh: true);
            if (IsDisposed) return;

            developTextBox.Text = string.IsNullOrEmpty(gitConfig.DevelopBranch) ? "develop" : gitConfig.DevelopBranch;
            masterTextBox.Text = string.IsNullOrEmpty(gitConfig.MasterBranch) ? "master" : gitConfig.MasterBranch;
            featureTextBox.Text = string.IsNullOrEmpty(gitConfig.FeaturePrefix) ? "feature/" : gitConfig.FeaturePrefix;
            hotfixTextBox.Text = string.IsNullOrEmpty(gitConfig.HotfixPrefix) ? "hotfix/" : gitConfig.HotfixPrefix;
            releaseTextBox.Text = string.IsNullOrEmpty(gitConfig.ReleasePrefix) ? "release/" : gitConfig.ReleasePrefix;
            tagTextBox.Text = gitConfig.TagPrefix;
            okButton.Enabled = true;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (!okButton.Enabled) return;
            CommandHelper.RunAsync(async () =>
            {
                okButton.Enabled = false;
                try
                {
                    var config = new GitConfig
                    {
                        DevelopBranch = developTextBox.Text,
                        MasterBranch = masterTextBox.Text,
                        FeaturePrefix = featureTextBox.Text,
                        HotfixPrefix = hotfixTextBox.Text,
                        ReleasePrefix = releaseTextBox.Text,
                        TagPrefix = tagTextBox.Text
                    };
                    if (!await GitHelper.ValidateBranchName(config.MasterBranch) ||
                        !await GitHelper.ValidateBranchName(config.DevelopBranch) ||
                        !await GitHelper.ValidateBranchName(config.FeaturePrefix + "tgit-validation") ||
                        !await GitHelper.ValidateBranchName(config.ReleasePrefix + "tgit-validation") ||
                        !await GitHelper.ValidateBranchName(config.HotfixPrefix + "tgit-validation") ||
                        !await GitHelper.ValidateTagName(config.TagPrefix + "tgit-validation")) return;
                    if (config.MasterBranch == config.DevelopBranch)
                    {
                        MessageBox.Show(this, "Master and develop must be different branches.", "TGit error");
                        return;
                    }
                    if (IsDisposed) return;
                    GitConfig = config;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                finally
                {
                    if (!IsDisposed) okButton.Enabled = true;
                }
            });
        }
    }
}
