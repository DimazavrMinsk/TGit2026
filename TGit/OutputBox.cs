using TGit.Extensions;
using TGit.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TGit
{
    public sealed partial class OutputBox : Form
    {
        private readonly string _branchName;
        private readonly IEnumerable<string[]> _pushCommands;
        private bool _succeeded;
        private bool _cleanupAttempted;
        private bool _optionsLoaded;
        private bool _busy = true;
        public string WorkingDirectory { get; }

        public OutputBox(string workingDirectory, string branchName, IEnumerable<string[]> pushCommands)
        {
            InitializeComponent();
            WorkingDirectory = workingDirectory;
            _branchName = branchName;
            _pushCommands = pushCommands?.ToArray();
            Load += OutputBox_Load;
            richTextBox.TextChanged += TextBox_TextChanged;
        }

        private void OutputBox_Load(object sender, EventArgs e)
            => CommandHelper.RunAsync(async () =>
            {
                if (string.IsNullOrEmpty(_branchName))
                {
                    localBranchCheckBox.Visible = false;
                    remoteBranchCheckBox.Visible = false;
                    pushCheckBox.Visible = false;
                    richTextBox.Height += 30;
                }
                else
                {
                    var remote = await ProcessHelper.QueryGitAsync(WorkingDirectory, "show-ref", "--verify", "--quiet",
                        "refs/remotes/origin/" + _branchName);
                    var options = await General.GetLiveInstanceAsync();
                    if (IsDisposed) return;
                    remoteBranchCheckBox.Enabled = remote.Succeeded;
                    localBranchCheckBox.Checked = options.DeleteLocalBranch;
                    remoteBranchCheckBox.Checked = remote.Succeeded && options.DeleteRemoteBranch;
                    pushCheckBox.Enabled = _pushCommands != null && _pushCommands.Any();
                    pushCheckBox.Checked = pushCheckBox.Enabled && options.PushChanges;
                }
                _optionsLoaded = true;
                if (!IsDisposed) okButton.Enabled = !_busy;
            });

        private void PostToUi(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            if (!InvokeRequired)
            {
                action();
                return;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (!IsDisposed && !Disposing) action();
                }));
            }
            catch (InvalidOperationException)
            {
                // The window was closed between checking its handle and posting the update.
            }
        }

        public void AppendLine(string line) => PostToUi(() =>
        {
            richTextBox.AppendText(line + Environment.NewLine, line.StartsWith(">", StringComparison.Ordinal));
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.ScrollToCaret();
        });

        public void BeginOperation()
        {
            _busy = true;
            _succeeded = false;
            okButton.Enabled = false;
        }

        public void CompleteOperation(bool succeeded, long elapsedMilliseconds) => PostToUi(() =>
        {
            _busy = false;
            _succeeded = succeeded;
            richTextBox.AppendText($"{Environment.NewLine}{(succeeded ? "Success" : "Error")} ({elapsedMilliseconds} ms){Environment.NewLine}",
                succeeded ? Color.Blue : Color.Red, true);
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.ScrollToCaret();
            okButton.Enabled = _optionsLoaded;
        });

        private void OkButton_Click(object sender, EventArgs e)
            => CommandHelper.RunAsync(async () =>
            {
                if (_busy) return;
                if (!_succeeded || _cleanupAttempted)
                {
                    Close();
                    return;
                }
                var commands = GitFlowPlan.Cleanup(_branchName, localBranchCheckBox.Checked,
                    remoteBranchCheckBox.Enabled && remoteBranchCheckBox.Checked,
                    pushCheckBox.Enabled && pushCheckBox.Checked, _pushCommands);
                if (commands.Count == 0)
                {
                    Close();
                    return;
                }
                _cleanupAttempted = true;
                _busy = true;
                okButton.Enabled = false;
                await ProcessHelper.RunGitCommandsAsync(commands, string.Empty, outputBox: this);
                if (!IsDisposed)
                {
                    _busy = false;
                    okButton.Enabled = true;
                }
            });

        private void ResolveButton_Click(object sender, EventArgs e)
            => CommandHelper.RunAsync(() => ProcessHelper.RunTortoiseGitFileCommand("resolve", filePath: WorkingDirectory));

        private void StashButton_Click(object sender, EventArgs e)
            => CommandHelper.RunAsync(() => ProcessHelper.RunTortoiseGitFileCommand("repostatus", filePath: WorkingDirectory));

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox.Text.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (richTextBox.Text.IndexOf("fix conflicts", StringComparison.OrdinalIgnoreCase) >= 0 && !flowLayoutPanel1.Controls.Find("Resolve", true).Any())
            {
                var button = new Button { Text = "Resolve", Name = "Resolve", AutoSize = true };
                button.Click += ResolveButton_Click;
                flowLayoutPanel1.Controls.Add(button);
            }
            if (richTextBox.Text.IndexOf("stash", StringComparison.OrdinalIgnoreCase) >= 0 && !flowLayoutPanel1.Controls.Find("Stash", true).Any())
            {
                var button = new Button { Text = "Show changes", Name = "Stash", AutoSize = true };
                button.Click += StashButton_Click;
                flowLayoutPanel1.Controls.Add(button);
            }
        }
    }
}
