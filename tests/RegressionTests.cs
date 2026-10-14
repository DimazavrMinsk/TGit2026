using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TGit.Helpers;
using TGit.Models;

internal static class RegressionTests
{
    private static readonly string Executable = Assembly.GetExecutingAssembly().Location;
    private static string _git;

    private static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--argv")
        {
            foreach (var arg in args.Skip(1)) Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(arg)));
            return 0;
        }
        if (args.Length > 0 && args[0] == "--flood")
        {
            for (var i = 0; i < 8000; i++) Console.Error.WriteLine(new string('x', 128));
            Console.WriteLine("stdout finished");
            return 0;
        }
        if (args.Length > 0 && args[0] == "--failure")
        {
            Console.WriteLine("output does not mean success");
            return 17;
        }
        if (args.Length > 0 && args[0] == "--record")
        {
            File.WriteAllText(args[1], "executed");
            return 0;
        }

        _git = args.Length > 0 ? args[0] : "git.exe";
        var tests = new Dictionary<string, Func<Task>>
        {
            { "Windows argument round trip", ArgumentRoundTrip },
            { "Concurrent stdout/stderr drainage", DrainBothPipes },
            { "Exit code is authoritative", ExitCode },
            { "Stop sequence after failure", StopAfterFailure },
            { "Shell characters remain literal", LiteralArguments },
            { "Start and finish feature without GitFlow", GitHubFlow },
            { "All eight cleanup checkbox combinations", CleanupCombinations },
            { "Cleanup stops on unmerged local branch", CleanupFailure },
            { "Hidden cleanup has no commands", EmptyCleanup },
            { "Repeated GitFlow configuration replaces values", Reconfigure },
            { "Configuration parser preserves spaces and semicolons", ConfigValues },
            { "Previous revisions of a spaced Unicode filename", PreviousRevisions },
            { "Annotated release reaches master and develop", FinishRelease }
        };
        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Value();
                Console.WriteLine("PASS " + test.Key);
            }
            catch (Exception e)
            {
                failed++;
                Console.Error.WriteLine("FAIL " + test.Key + ": " + e);
            }
        }
        Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
        return failed == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task<T> Within<T>(Task<T> task)
    {
        if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(20))) != task)
            throw new TimeoutException("Process did not finish within 20 seconds.");
        return await task;
    }

    private static async Task ArgumentRoundTrip()
    {
        var values = new[] { "", "two words", "a\"b", "C:\\folder with spaces\\", "\\\\server\\share\\",
            "&|<>^%PATH%!", "строка;日本語", "one\ntwo", "tab\tvalue", "slash\\\"quote", "-option" };
        var result = await Within(GitProcess.RunAsync(Executable, Path.GetTempPath(), new[] { "--argv" }.Concat(values)));
        // Preserve the first empty line, which represents an empty argv entry.
        var actual = result.Output.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
            .Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line))).ToArray();
        Assert(result.Succeeded && values.SequenceEqual(actual), "An argument changed during process launch.");
    }

    private static async Task DrainBothPipes()
    {
        var result = await Within(GitProcess.RunAsync(Executable, Path.GetTempPath(), new[] { "--flood" }));
        Assert(result.Succeeded && result.Error.Length > 1000000 && result.Output.Contains("stdout finished"),
            "Both streams must be drained completely.");
    }

    private static async Task ExitCode()
    {
        var result = await GitProcess.RunAsync(Executable, Path.GetTempPath(), new[] { "--failure" });
        Assert(!result.Succeeded && result.ExitCode == 17 && result.Output.Length > 0,
            "A failed process with stdout must remain a failure.");
    }

    private static async Task StopAfterFailure()
    {
        using (var repo = await Repository.Create())
        {
            var before = Path.Combine(repo.Root, "before.txt");
            var after = Path.Combine(repo.Root, "after.txt");
            var success = await GitProcess.RunSequenceAsync(Executable, repo.Root,
                new[] { new[] { "--record", before }, new[] { "--failure" }, new[] { "--record", after } });
            Assert(!success && File.Exists(before) && !File.Exists(after), "Commands after failure must not execute.");
        }
    }

    private static async Task LiteralArguments()
    {
        using (var repo = await Repository.Create())
        {
            const string value = "x\" & echo injected > injection.txt & %PATH% !literal! \\";
            await repo.Git("config", "--local", "review.literal", value);
            Assert((await repo.Git("config", "--get", "review.literal")).Output.TrimEnd('\r', '\n') == value,
                "Shell characters were not preserved.");
            Assert(!File.Exists(Path.Combine(repo.Root, "injection.txt")), "An extra command executed.");
            const string branch = "feature/a&b%PATH%!_тест";
            await repo.Git("check-ref-format", "--branch", branch);
            Assert(await repo.Run(GitFlowPlan.Start("main", branch, false)), "Starting a literal branch failed.");
            Assert((await repo.Git("symbolic-ref", "--short", "HEAD")).Output.Trim() == branch,
                "The actual branch name changed.");
        }
    }

    private static async Task GitHubFlow()
    {
        using (var repo = await Repository.Create())
        {
            Assert(!(await repo.Raw("config", "--get", "gitflow.branch.master")).Succeeded, "Fixture must have no GitFlow.");
            Assert(await repo.Run(GitFlowPlan.Start("main", "feature/github", false)), "Start feature failed.");
            await repo.Commit("feature change");
            var feature = (await repo.Git("rev-parse", "HEAD")).Output.Trim();
            Assert(await repo.Run(GitFlowPlan.Finish("main", "feature/github", false)), "Finish feature failed.");
            Assert((await repo.Git("symbolic-ref", "--short", "HEAD")).Output.Trim() == "main", "Did not switch to main.");
            await repo.Git("merge-base", "--is-ancestor", feature, "main");
        }
    }

    private static async Task CleanupCombinations()
    {
        for (var mask = 0; mask < 8; mask++)
        {
            using (var repo = await Repository.Create())
            {
                await repo.AddRemote();
                var originalMain = (await repo.Git("rev-parse", "main")).Output.Trim();
                await repo.Git("checkout", "-b", "feature/cleanup");
                await repo.Commit("cleanup feature");
                await repo.Git("push", "origin", "feature/cleanup");
                Assert(await repo.Run(GitFlowPlan.Finish("main", "feature/cleanup", false)), "Merge failed.");
                var deleteLocal = (mask & 1) != 0;
                var deleteRemote = (mask & 2) != 0;
                var push = (mask & 4) != 0;
                Assert(await repo.Run(GitFlowPlan.Cleanup("feature/cleanup", deleteLocal, deleteRemote, push,
                    new[] { new[] { "push", "origin", "main" } })), "Cleanup failed for mask " + mask);
                var local = (await repo.Raw("show-ref", "--verify", "--quiet", "refs/heads/feature/cleanup")).Succeeded;
                var remote = (await repo.Git("ls-remote", "--heads", "origin", "feature/cleanup")).Output.Length > 0;
                var remoteMain = (await repo.Git("ls-remote", "--heads", "origin", "main")).Output.Split('\t')[0];
                Assert(local == !deleteLocal && remote == !deleteRemote, "Checkboxes ignored for mask " + mask);
                Assert((remoteMain != originalMain) == push, "Push checkbox ignored for mask " + mask);
            }
        }
    }

    private static async Task CleanupFailure()
    {
        using (var repo = await Repository.Create())
        {
            await repo.AddRemote();
            await repo.Git("checkout", "-b", "feature/unmerged");
            await repo.Commit("unmerged change");
            await repo.Git("push", "origin", "feature/unmerged");
            await repo.Git("checkout", "main");
            Assert(!await repo.Run(GitFlowPlan.Cleanup("feature/unmerged", true, true, false, null)),
                "Deleting an unmerged branch must fail.");
            Assert((await repo.Git("ls-remote", "--heads", "origin", "feature/unmerged")).Output.Length > 0,
                "Remote deletion ran after local deletion failed.");
        }
    }

    private static Task EmptyCleanup()
    {
        Assert(GitFlowPlan.Cleanup("", true, true, true, new[] { new[] { "push", "origin", "main" } }).Count == 0,
            "An output window without a branch must never clean up or push.");
        return Task.CompletedTask;
    }

    private static async Task Reconfigure()
    {
        using (var repo = await Repository.Create())
        {
            var config = new GitConfig { MasterBranch = "main", DevelopBranch = "develop", FeaturePrefix = "feature/",
                ReleasePrefix = "release/", HotfixPrefix = "hotfix/", TagPrefix = "" };
            Assert(await repo.Run(GitFlowPlan.Configure(config, false, false)), "Initial configuration failed.");
            await repo.Commit("develop change");
            var develop = (await repo.Git("rev-parse", "develop")).Output.Trim();
            config.FeaturePrefix = "work/";
            config.TagPrefix = "v";
            Assert(await repo.Run(GitFlowPlan.Configure(config, true, false)), "Reconfiguration failed.");
            var refreshed = new GitConfig((await repo.Git("config", "--get-regexp", "gitflow|bugtraq|svn-remote")).Output);
            Assert(refreshed.FeaturePrefix == "work/" && refreshed.TagPrefix == "v", "New configuration was not read.");
            Assert((await repo.Git("config", "--get-all", "gitflow.prefix.feature")).Output.Trim() == "work/",
                "Configuration accumulated duplicate values.");
            Assert((await repo.Git("rev-parse", "develop")).Output.Trim() == develop, "Existing develop branch was reset.");
        }
    }

    private static async Task ConfigValues()
    {
        using (var repo = await Repository.Create())
        {
            const string bugtraq = "Issue %BUGID%; сохранить пробелы";
            const string svn = "https://example.invalid/svn?a=b";
            await repo.Git("config", "bugtraq.message", bugtraq);
            await repo.Git("config", "svn-remote.svn.url", svn);
            var config = new GitConfig((await repo.Git("config", "--get-regexp", "bugtraq|svn-remote")).Output);
            Assert(config.BugTraqMessage == bugtraq && config.SvnUrl == svn, "Configuration values were truncated.");
        }
    }

    private static async Task PreviousRevisions()
    {
        using (var repo = await Repository.Create())
        {
            var name = "-файл с пробелами.txt";
            var path = Path.Combine(repo.Root, name);
            File.WriteAllText(path, "first");
            await repo.Git("add", "--", name);
            await repo.Git("commit", "-m", "first revision");
            File.AppendAllText(path, "second");
            await repo.Git("add", "--", name);
            await repo.Git("commit", "-m", "second revision");
            var result = await repo.Git("log", "-2", "--pretty=format:%h", "--", name);
            Assert(result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length == 2,
                "A filename containing spaces was not passed as one pathspec.");
        }
    }

    private static async Task FinishRelease()
    {
        using (var repo = await Repository.Create())
        {
            await repo.Git("branch", "develop");
            await repo.Git("checkout", "-b", "release/1.0", "develop");
            await repo.Commit("release change");
            var source = (await repo.Git("rev-parse", "HEAD")).Output.Trim();
            const string message = "Release \"1.0\" & literal %PATH%; вторая строка\nnotes";
            Assert(await repo.Run(GitFlowPlan.FinishVersion("main", "develop", "release/1.0", "v1.0", message, false)),
                "Finishing release failed.");
            await repo.Git("merge-base", "--is-ancestor", source, "main");
            await repo.Git("merge-base", "--is-ancestor", source, "develop");
            Assert((await repo.Git("cat-file", "-t", "v1.0")).Output.Trim() == "tag", "Tag is not annotated.");
            Assert((await repo.Git("for-each-ref", "--format=%(contents)", "refs/tags/v1.0")).Output.Replace("\r\n", "\n").TrimEnd('\n') == message,
                "Annotated tag message changed.");
        }
    }

    private sealed class Repository : IDisposable
    {
        public string Root { get; }
        private readonly IDictionary<string, string> _environment;

        private Repository()
        {
            Root = Path.Combine(Path.GetTempPath(), "TGitRegression-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            var globalConfig = Path.Combine(Root, "empty-global-config");
            File.WriteAllText(globalConfig, string.Empty);
            _environment = new Dictionary<string, string>
            {
                { "GIT_CONFIG_NOSYSTEM", "1" }, { "GIT_CONFIG_GLOBAL", globalConfig },
                { "GIT_TERMINAL_PROMPT", "0" }, { "GIT_MERGE_AUTOEDIT", "no" }
            };
        }

        public static async Task<Repository> Create()
        {
            var repo = new Repository();
            try
            {
                await repo.Git("init", "-b", "main");
                await repo.Git("config", "user.name", "TGit regression");
                await repo.Git("config", "user.email", "test@example.invalid");
                await repo.Git("config", "commit.gpgsign", "false");
                await repo.Git("config", "tag.gpgsign", "false");
                await repo.Commit("initial");
                return repo;
            }
            catch
            {
                repo.Dispose();
                throw;
            }
        }

        public Task<GitProcessResult> Raw(params string[] args) => GitProcess.RunAsync(_git, Root, args, _environment);

        public async Task<GitProcessResult> Git(params string[] args)
        {
            var result = await Within(Raw(args));
            Assert(result.Succeeded, string.Join(" ", args) + ": " + result.Error);
            return result;
        }

        public Task<bool> Run(IEnumerable<string[]> commands)
            => Within(GitProcess.RunSequenceAsync(_git, Root, commands, _environment));

        public async Task Commit(string message)
        {
            File.AppendAllText(Path.Combine(Root, "tracked.txt"), message + Environment.NewLine);
            await Git("add", "tracked.txt");
            await Git("commit", "-m", message);
        }

        public async Task AddRemote()
        {
            var remote = Path.Combine(Root, "remote.git");
            await Git("init", "--bare", remote);
            await Git("remote", "add", "origin", remote);
            await Git("push", "-u", "origin", "main");
        }

        public void Dispose()
        {
            // Only remove the temporary directory created by this fixture.
            var absoluteRoot = Path.GetFullPath(Root);
            var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absoluteRoot.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(absoluteRoot).StartsWith("TGitRegression-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to remove an unexpected path: " + absoluteRoot);
            foreach (var file in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(absoluteRoot, true);
        }
    }
}
