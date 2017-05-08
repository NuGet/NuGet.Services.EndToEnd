// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public static class CommandRunner
    {
        public static async Task<CommandRunnerResult> RunAsync(
            string fileName,
            string workingDirectory,
            string arguments,
            ITestOutputHelper logger,
            IDictionary<string, string> environmentVariables = null,
            int timeOutInMilliseconds = 60000)
        {
            await Task.Yield();

            logger.WriteLine($"Executing command:{Environment.NewLine} - {fileName} {arguments}");

            var psi = new ProcessStartInfo(Path.GetFullPath(fileName), arguments)
            {
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (environmentVariables != null)
            {
                foreach (var pair in environmentVariables)
                {
                    psi.EnvironmentVariables[pair.Key] = pair.Value;
                }
            }

            int exitCode = 1;

            var output = new StringBuilder();
            var errors = new StringBuilder();

            Process process = null;

            try
            {
                process = new Process();

                process.StartInfo = psi;
                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Could not start process with file name: {fileName}", ex);
                }

                var outputTask = ConsumeStreamReaderAsync(process.StandardOutput, output);
                var errorTask = ConsumeStreamReaderAsync(process.StandardError, errors);

                var processExited = process.WaitForExit(timeOutInMilliseconds);
                if (!processExited)
                {
                    process.Kill();

                    var processName = Path.GetFileName(fileName);

                    throw new TimeoutException($"{processName} timed out: " + psi.Arguments);
                }

                if (processExited)
                {
                    await Task.WhenAll(outputTask, errorTask);
                    exitCode = process.ExitCode;
                }
            }
            finally
            {
                process.Dispose();
            }

            var result = new CommandRunnerResult(
                exitCode,
                output.ToString(),
                errors.ToString());

            logger.WriteLine(
                $"Exit code: {result.ExitCode}" +
                Environment.NewLine +
                $"STDOUT ({result.Output.Length} characters):{Environment.NewLine}{result.Output}" +
                Environment.NewLine +
                $"STDERR ({result.Error.Length} characters):{Environment.NewLine}{result.Error}");

            return result;
        }

        private static async Task ConsumeStreamReaderAsync(StreamReader reader, StringBuilder lines)
        {
            await Task.Yield();

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.AppendLine(line);
            }
        }
    }
}
