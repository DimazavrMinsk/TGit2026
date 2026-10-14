using Microsoft.VisualStudio.Shell;
using TGit.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGit
{
    public partial class Credentials : Form
    {
        private readonly string _workingDirectory;

        public Credentials(string workingDirectory)
        {
            InitializeComponent();
            _workingDirectory = workingDirectory;
            okButton.DialogResult = DialogResult.None;
            emailTextBox.KeyDown += EmailTextBox_KeyDown;
        }

        private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                okButton_Click(null, null);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (!okButton.Enabled) return;
            CommandHelper.RunAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text) || string.IsNullOrWhiteSpace(emailTextBox.Text))
                {
                    MessageBox.Show(this, "Enter a name and email address.", "TGit");
                    return;
                }
                okButton.Enabled = false;
                try
                {
                    var name = await ProcessHelper.QueryGitAsync(_workingDirectory, "config", "user.name", nameTextBox.Text);
                    var email = name.Succeeded
                        ? await ProcessHelper.QueryGitAsync(_workingDirectory, "config", "user.email", emailTextBox.Text)
                        : name;
                    if (IsDisposed) return;
                    if (!name.Succeeded || !email.Succeeded)
                    {
                        MessageBox.Show(this, email.Error, "TGit error");
                        return;
                    }
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
