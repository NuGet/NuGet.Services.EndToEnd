// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public static class DotNetExeClient
    {
        public static readonly string ExeName = "dotnet.exe";

        public static async Task<CommandRunnerResult> BuildProjectAsync(string projectPath, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "build",
                projectPath,
            };

            var projectDirectory = Path.GetDirectoryName(projectPath);
            return await RunAsync(projectDirectory, arguments, logger);
        }

        public static async Task<CommandRunnerResult> PackProjectAsync(string id, string version, string projectPath, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "pack",
                projectPath,
                $"/p:PackageId={id}",
                $"/p:PackageVersion={version}",
            };

            var projectDirectory = Path.GetDirectoryName(projectPath);
            return await RunAsync(projectDirectory, arguments, logger);
        }

        public static bool TryGetDotNetExe(out string filePath)
        {
            filePath = null;
            if (string.IsNullOrEmpty(EnvironmentSettings.DotnetInstallDirectory))
            {
                return false; 
            }

            filePath = Path.Combine(EnvironmentSettings.DotnetInstallDirectory, ExeName);
            return File.Exists(filePath);
        }

        private static async Task<CommandRunnerResult> RunAsync(string workingDirectory, List<string> arguments, ITestOutputHelper logger)
        {
            string filePath;
            if (!TryGetDotNetExe(out filePath))
            {
                throw new FileNotFoundException($"The {filePath} not found. Make sure the {ExeName} is installed correctly.");
            }

            var joinedArguments = string.Join(" ", arguments);

            // New installation of DOTNET invokes first time user experience which involves caching, decompressing packages etc.
            // This prevents faster package build times, disable this behavior using environment variable
            var environmentVariables = new Dictionary<string, string>
            {
                { "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1" }
            };

            return await CommandRunner.RunAsync(
                filePath,
                workingDirectory,
                joinedArguments,
                logger,
                environmentVariables);
        }
    }
}
