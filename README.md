# TGit2026
Control TortoiseGit from within Visual Studio

This project is based on [TGit by sboulema](https://github.com/sboulema/TGit).
Many thanks to the original author, sboulema, for the work put into creating
and developing TGit.

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
TGit2026 has several options: 

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
  checkbox values in the finishing dialog; see the GitFlow limitation below.
- **Pull changes**: Pull before starting a feature/release and before merging
  when finishing it. Enabled by default.
- **Use annotated tag**: Create an annotated tag when finishing a release
  (otherwise, create a lightweight tag).

**Default bug id:** Uses the same macros as the default commit message. Passed
to the commit dialog when the repository has a `bugtraq.message` setting.

**Save files before operations:** Runs Visual Studio's Save All command before
the main-menu and file-context operations that use this option. GitFlow commands
do not use it.

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

## Gitflow
The [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/) commands
use the branch names and prefixes configured through **TGit > GitFlow > Init/Config**
and stored in the repository's `gitflow.*` settings. The names `develop` and
`master` below refer to the configured development and production branches;
they do not have to use those literal names.

Pull steps run only when **Pull changes** is enabled. Push and remote branch
deletion use the remote named `origin`.

##### Start New Feature

1. Switch to the develop branch
2. Pull latest changes on develop, if enabled
3. Create and switch to a branch using the configured feature prefix

Known limitation: the current implementation does not await the SSH setup
helper when building this command. The resulting command contains the task's
string representation, so this workflow needs a code fix before it can be
relied on.

##### Finish Feature

1. Switch to the develop branch
2. Pull latest changes on develop, if enabled
3. Merge the feature branch into develop with `--no-ff`
4. After the initial command finishes, choose the finishing options in the
   output dialog and click **OK**. The follow-up command pushes develop if
   selected, attempts to delete the local feature branch, then deletes the
   remote feature branch if selected.

##### Start New Release

1. Switch to the develop branch
2. Pull latest changes on develop, if enabled
3. Create and switch to a branch using the configured release prefix

##### Finish Release

1. Switch to the master branch
2. Pull latest changes on master, if enabled
3. Merge the release branch into master with `--no-ff`
4. Tag the release using the configured tag prefix; prompt for a message when
   **Use annotated tag** is enabled
5. Switch to the develop branch
6. Pull latest changes on develop, if enabled
7. Merge the release branch into develop with `--no-ff`
8. After the initial command finishes, choose the finishing options in the
   output dialog and click **OK**. If **Push changes** is selected, push develop,
   master, and the release tag, in that order. Then attempt to delete the local
   release branch and delete the remote release branch if selected.

Known limitation for finishing workflows: when any of **Delete local branch**,
**Delete remote branch**, or **Push changes** is checked, the follow-up command
always includes local branch deletion (`git branch -d`), even when **Delete local
branch** is unchecked. If all three are unchecked, no follow-up command runs.
