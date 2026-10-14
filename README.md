# TGit2026
Control TortoiseGit from within Visual Studio

Current version: **1.1.0**.

This project is based on [TGit by sboulema](https://github.com/sboulema/TGit).
Many thanks to the original author, sboulema, for the work put into creating
and developing TGit.

## What's new in 1.1.0

- GitFlow runs Git directly with separate arguments, stops at the first failed
  command, and reads standard output and standard error concurrently.
- Finishing operations respect each push and branch-deletion checkbox. Failed
  operations do not run follow-up cleanup.
- Start Feature and GitHub Flow finishing work with the updated process runner.
  GitHub Flow supports the default branch selected by `origin/HEAD`, with local
  `main` and then `master` as fallbacks.
- GitFlow Init/Config replaces existing settings and refreshes the configuration
  used by the extension. Git executable discovery accepts a directory or a full
  `git.exe` path from TortoiseGit's setting.
- Previous-revision comparison uses the full file path and handles filenames
  containing spaces and Unicode. GitFlow output windows remain associated with
  the repository where the operation started.
- The solution includes 13 regression scenarios covering process handling and
  local Git workflows.

## Visual Studio 2026

This version targets Visual Studio 2022 and Visual Studio 2026 on x64. The 64-bit
VSIX target uses `[17.0,)`, following Microsoft's
[API compatibility model](https://learn.microsoft.com/en-us/visualstudio/extensibility/migration/extension-compatibility?view=visualstudio).
Visual Studio 2026 supports the 17.x APIs used by this extension; no 18.x SDK
dependency is required. The project uses `Community.VisualStudio.Toolkit.17`
and no longer targets Visual Studio 2017/2019. Native ARM64 is not an installation target.
This describes the declared installation targets; verify runtime behavior using
the checks below.

### Building

Open `TGIT.sln` in Visual Studio 2026 with the **.NET desktop development**
workload installed, restore NuGet packages, and build the Release configuration.
The **Visual Studio extension development** workload is recommended for VSIX
project tooling and debugging in an experimental instance.

Alternatively, run this command from a **Developer PowerShell for VS 2026**
in this directory:

```powershell
msbuild .\TGIT.sln /restore /t:Rebuild /p:Configuration=Release /p:DeployExtension=false /p:RestoreConfigFile=nuget.config
```

The installer is `TGit\bin\Release\TGit2026.vsix`. The project obtains the VSSDK
build tools and .NET Framework 4.8 reference assemblies from NuGet, so a
separate 4.8 targeting pack is not required. Use full-framework MSBuild
from Visual Studio, rather than `dotnet build`, for this classic VSIX/WPF project.

The project explicitly names the installer `TGit2026.vsix` in both configurations:
`TGit\bin\Debug\TGit2026.vsix` for Debug and `TGit\bin\Release\TGit2026.vsix` for Release.

### Regression checks

The solution includes a .NET Framework 4.8 console test runner with no test framework
dependencies. After building Release, run:

```powershell
.\tests\bin\Release\TGit.RegressionTests.exe
```

Git must be on PATH, or pass the full path to `git.exe` as the first argument:

```powershell
.\tests\bin\Release\TGit.RegressionTests.exe 'C:\Program Files\Git\bin\git.exe'
```

The runner exits with code `0` when all scenarios pass and `1` on failure. The
checks use temporary local repositories and remotes, isolate Git configuration,
and remove their temporary files. They cover argument quoting, concurrent process
output, failure handling, GitFlow configuration, GitHub Flow, release tags, all
cleanup checkbox combinations, and filenames containing spaces and Unicode.

These checks exercise the process runner and Git workflows. They do not replace
the Visual Studio installation and UI checks below.

### Installing and checking in Visual Studio 2026

1. Close Visual Studio, run `TGit2026.vsix`, and select Visual Studio 2026.
2. Start Visual Studio and confirm TGit2026 is enabled in **Extensions > Manage Extensions**.
3. Open a solution in a Git repository. Check the TGit menu and the TGit context
   menu for a file in Solution Explorer.
4. Run **Show Log**, **Show Changes**, and a file **Diff** and confirm they open
   the expected TortoiseGit dialogs for that repository/file.
5. Check **Tools > Options > TGit > General** and verify a changed option is
   retained after restarting Visual Studio.

Git for Windows and TortoiseGit must be installed for these checks.

## Getting started

1. Install [Git for Windows](https://git-scm.com/install/windows).
2. Install [TortoiseGit](https://tortoisegit.org/download/) and configure its
   **Git.exe Path** in **Settings > General** to point to the Git for Windows
   `bin` directory (normally `C:\Program Files\Git\bin`). The extension reads
   this setting to locate `git.exe`.
3. Build and install `TGit2026.vsix` as described above.
4. Open a solution in a Git repository and use the **TGit** menu.

## Options
Open **Tools > Options > TGit > General** to configure these options.

**Default commit message:**

You can use regular text and the following supported macros:

- `$(BranchName)`: Current branch name, including its prefix.
- `$(FeatureName)`: Current branch name with the configured feature, release,
  or hotfix prefix removed, when applicable.
- `$(SolutionDir)`: Directory containing the solution file.
- `$(SolutionPath)`: Full path to the solution file.
- `$(SolutionName)`: Solution name reported by Visual Studio.
- `$(SolutionFileName)`: Solution filename, including its extension.
- `$(SolutionExt)`: Solution file extension.
- `$(DevEnvDir)`, `$(VSInstallDir)`, `$(FxCopDir)`: Values read from legacy
  Visual Studio registry keys; these may be unavailable in modern installations.

Other MSBuild macros are not expanded by the extension.

**Close dialog after operation:**

You can change what happens to the TortoiseGit dialogs after their operations:

0: Close manually

1: Auto-close if no further options are available

2: Auto-close if no errors 

**GitFlow options:**

These options set defaults for GitFlow operations:

- **Delete local branch**, **Delete remote branch**, **Push changes**: Default
  checkbox values in the finishing dialog. Each checkbox controls its own action;
  all three are unchecked by default.
- **Pull changes**: Pull the base branch before starting a feature, release, or
  hotfix, and pull each target branch before merging when finishing a workflow.
  Also applies to GitHub Flow. Enabled by default.
- **Use annotated tag**: Prompt for a message and create an annotated tag when
  finishing a release or hotfix. Disabled by default; otherwise the tag is
  lightweight. An empty or cancelled annotation prompt stops the operation.

**Default bug id:** Uses the same macros as the default commit message. Passed
to the commit dialog when the repository has a `bugtraq.message` setting.

**Save files before operations:** Runs Visual Studio's Save All command before
the main-menu and file-context operations that use this option, including GitFlow
and GitHub Flow workflows. Disabled by default.

## Keyboard shortcuts

These are the default two-step shortcuts: press Ctrl+Shift+Alt+G, release it,
then press the second key. Visual Studio keyboard settings can override them.

* `(G)it (C)ommit` - Ctrl+Shift+Alt+G, C
* `(G)it (F)etch` - Ctrl+Shift+Alt+G, F
* `(G)it (L)og` - Ctrl+Shift+Alt+G, L
* `(G)it (M)erge` - Ctrl+Shift+Alt+G, M
* `(G)it (P)ull` - Ctrl+Shift+Alt+G, P
* `(G)it P(u)sh` - Ctrl+Shift+Alt+G, U
* `(G)it (R)evert` - Ctrl+Shift+Alt+G, R
* `(G)it Stas(h)-Apply` - Ctrl+Shift+Alt+G, H
* `(G)it Stash-Cre(a)te` - Ctrl+Shift+Alt+G, A
* `(G)it (S)witch` - Ctrl+Shift+Alt+G, S
* `(G)it S(y)nc` - Ctrl+Shift+Alt+G, Y
* `(G)it Clea(n)up` - Ctrl+Shift+Alt+G, N
* `(G)it R(e)solve` - Ctrl+Shift+Alt+G, E
* `(G)it Sho(w) Changes` - Ctrl+Shift+Alt+G, W
* `(G)it File (B)lame` - Ctrl+Shift+Alt+G, B
* `(G)it File (D)iff` - Ctrl+Shift+Alt+G, D
* `(G)it (T)ag` - Ctrl+Shift+Alt+G, T
* `(G)it Rebase` - Ctrl+Shift+Alt+G, G

## GitFlow
The [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/) commands
use the branch names and prefixes configured through **TGit > GitFlow > Init/Config**
and stored in the repository's `gitflow.*` settings. The names `develop` and
`master` below refer to the configured development and production branches;
they do not have to use those literal names.

Pull steps run only when **Pull changes** is enabled. Push and remote branch
deletion use the remote named `origin`.

### Init / Config

Use **Init** to choose production and development branches and feature, release,
hotfix, and tag prefixes. Once GitFlow is configured, the command is named
**Config**. Saving replaces the corresponding `gitflow.*` values and refreshes
the extension's cached configuration.

If the local development branch already exists, it is preserved. Otherwise,
the command creates it to track `origin/<develop>` when that remote branch exists,
or creates it from the configured production branch. Init/Config does not push
branches to the remote.

### Start New Feature

1. Switch to the develop branch
2. Pull latest changes on develop, if enabled
3. Create and switch to a branch using the configured feature prefix

### Finish Feature

1. Switch to the develop branch
2. Pull latest changes on develop, if enabled
3. Merge the feature branch into develop with `--no-ff --no-edit`
4. After a successful merge, select the finishing options described below and
   click **OK**. The push option sends develop to `origin`.

### Start New Release

1. Switch to the develop branch
2. Pull latest changes on develop, if enabled
3. Create and switch to a branch using the configured release prefix

### Finish Release

1. Switch to the master branch
2. Pull latest changes on master, if enabled
3. Merge the release branch into master with `--no-ff --no-edit`
4. Tag the release using the configured tag prefix; prompt for a message when
   **Use annotated tag** is enabled
5. Switch to the develop branch
6. Pull latest changes on develop, if enabled
7. Merge the release branch into develop with `--no-ff --no-edit`
8. After success, select the finishing options and click **OK**. The push option
   sends develop, master, and the release tag to `origin`, in that order.

### Start New Hotfix

1. Switch to the master branch
2. Pull latest changes on master, if enabled
3. Create and switch to a branch using the configured hotfix prefix

### Finish Hotfix

Use the same sequence as **Finish Release**, merging the hotfix branch into
master and develop and creating a tag from the tag prefix and hotfix version.
The push option sends develop, master, and the hotfix tag to `origin`.

### Finishing options and failures

After a successful feature, release, or hotfix operation, the output window offers:

| Option | Action |
| --- | --- |
| Push changes | Push the target branches and any release/hotfix tag to `origin` |
| Delete local branch | Delete the source branch with `git branch -d` |
| Delete remote branch | Delete the source branch on `origin`; available when its remote-tracking ref exists |

Click **OK** to run the selected actions in the order shown. If all options are
unchecked, **OK** closes the window without running follow-up commands. Local
deletion is never implied by push or remote deletion. After the follow-up run,
click **OK** again to close the window.

Every sequence stops at the first failed Git command. If the main operation
fails, **OK** closes the output window without pushing or deleting branches.
Completed Git commands are not rolled back automatically; inspect the output
and repository state before continuing after a failure. Closing an output window
does not cancel an operation already running.

## GitHub Flow

The **GitHubFlow** submenu is shown for repositories without GitFlow configuration.
It does not require `gitflow.*` settings or a feature prefix. Its base branch is
the branch referenced by local `origin/HEAD`; if that ref is absent, the extension
uses local `main` when available, otherwise `master`.

- **Start New GitHubFlow Feature** switches to the base branch, pulls if enabled,
  then creates and switches to the branch name entered in the dialog.
- **Finish GitHubFlow Feature** switches to the base branch, pulls if enabled,
  then merges the current feature branch with `--no-ff --no-edit`. Run it from
  the feature branch, rather than from the base branch or a detached HEAD.

After a successful finish, the same finishing checkboxes control pushing the base
branch and deleting the local and remote feature branches. These commands merge
branches directly; they do not create GitHub pull requests.
