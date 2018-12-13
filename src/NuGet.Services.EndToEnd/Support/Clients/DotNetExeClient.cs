// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class DotNetExeClient
    {
        public DotNetExeClient()
        { }

        public async Task<CommandRunnerResult> BuildProject(string projectPath, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "build",
                projectPath
            };

            var projectDirectory = Path.GetDirectoryName(projectPath);

            return await RunAsync(projectDirectory, arguments, logger);
        }

        private async Task<CommandRunnerResult> RunAsync(string workingDirectory, List<string> arguments, ITestOutputHelper logger)
        {
            var fileName = $"dotnet.exe";
            var filePath = Path.Combine(EnvironmentSettings.DotnetCliDirectory, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The ${filePath} not found. Make sure the ${fileName} is installed correctly.");
            }

            var joinedArguments = string.Join(" ", arguments);

            return await CommandRunner.RunAsync(
                filePath,
                workingDirectory,
                joinedArguments,
                logger);
        }
    }
}
