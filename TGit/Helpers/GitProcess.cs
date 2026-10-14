using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGit.Helpers
{
    public sealed class GitProcessResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }
        public bool Succeeded => ExitCode == 0;

        public GitProcessResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }
    }

    public static class GitProcess
    {
        // ProcessStartInfo.ArgumentList is unavailable on .NET Framework 4.8.
        // Quote using the Windows argv rules, without passing arguments through a shell.
        public static string QuoteArgument(string argument)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));
            if (argument.IndexOf('\0') >= 0) throw new ArgumentException("Arguments cannot contain NUL.", nameof(argument));

            var result = new StringBuilder("\"");
            var backslashes = 0;
            foreach (var character in argument)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                result.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
                result.Append(character);
                backslashes = 0;
            }
            result.Append('\\', backslashes * 2);
            return result.Append('"').ToString();
        }

        public static string FormatArguments(IEnumerable<string> arguments)
            => string.Join(" ", arguments.Select(QuoteArgument));

        public static async Task<GitProcessResult> RunAsync(string executable, string workingDirectory,
            IEnumerable<string> arguments, IDictionary<string, string> environment = null, Action<string> output = null)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = FormatArguments(arguments),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
                CreateNoWindow = true
            };
            if (environment != null)
            {
                foreach (var pair in environment) startInfo.EnvironmentVariables[pair.Key] = pair.Value;
            }

            using (var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
            {
                var exit = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.Exited += (sender, e) => exit.TrySetResult(process.ExitCode);
                process.Start();

                // Consume both pipes while the child runs and wait for their EOF as well as its exit.
                var stdout = ReadOutputAsync(process.StandardOutput, output);
                var stderr = ReadOutputAsync(process.StandardError, output);
                await Task.WhenAll(stdout, stderr, exit.Task).ConfigureAwait(false);
                return new GitProcessResult(await exit.Task.ConfigureAwait(false),
                    await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
            }
        }

        private static async Task<string> ReadOutputAsync(StreamReader reader, Action<string> output)
        {
            var text = new StringBuilder();
            string line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                text.AppendLine(line);
                output?.Invoke(line);
            }
            return text.ToString();
        }

        public static async Task<bool> RunSequenceAsync(string executable, string workingDirectory,
            IEnumerable<string[]> commands, IDictionary<string, string> environment = null, Action<string> output = null)
        {
            foreach (var command in commands)
            {
                output?.Invoke("> git " + FormatArguments(command));
                var result = await RunAsync(executable, workingDirectory, command, environment, output).ConfigureAwait(false);
                if (!result.Succeeded) return false;
            }
            return true;
        }
    }
}
