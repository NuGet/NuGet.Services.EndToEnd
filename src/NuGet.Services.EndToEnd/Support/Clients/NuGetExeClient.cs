// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class NuGetExeClient
    {
        private const string LatestVersion = "4.9.1";

        private readonly TestSettings _testSettings;
        private readonly string _version;
        private readonly SourceType _sourceType;
        private readonly string _httpCachePath;
        private readonly string _globalPackagesPath;
        private readonly GalleryClient _galleryClient;

        public NuGetExeClient(TestSettings testSettings, GalleryClient galleryClient) : this(
            testSettings,
            galleryClient,
            LatestVersion,
            SourceType.V3,
            httpCachePath: null,
            globalPackagesPath: null)
        {
        }

        private NuGetExeClient(
            TestSettings testSettings,
            GalleryClient galleryClient,
            string version,
            SourceType sourceType,
            string httpCachePath,
            string globalPackagesPath)
        {
            _version = version;
            _testSettings = testSettings;
            _sourceType = sourceType;
            _httpCachePath = httpCachePath;
            _globalPackagesPath = globalPackagesPath;
            _galleryClient = galleryClient;
        }

        public NuGetExeClient WithHttpCachePath(string httpCachePath)
        {
            return new NuGetExeClient(
                _testSettings,
                _galleryClient,
                _version,
                _sourceType,
                httpCachePath,
                _globalPackagesPath);
        }

        public NuGetExeClient WithGlobalPackagesPath(string globalPackagesPath)
        {
            return new NuGetExeClient(
                _testSettings,
                _galleryClient,
                _version,
                _sourceType,
                _httpCachePath,
                globalPackagesPath);
        }

        /// <summary>
        /// Return a <see cref="NuGetExeClient"/> that uses the provided client version. If <c>null</c> is provided,
        /// use the latest version.
        /// </summary>
        /// <param name="version">The version of nuget.exe to use.</param>
        /// <returns>The <see cref="NuGetExeClient"/> with the provided version selected.</returns>
        public NuGetExeClient WithVersion(string version)
        {
            return new NuGetExeClient(
                _testSettings,
                _galleryClient,
                version ?? LatestVersion,
                _sourceType,
                _httpCachePath,
                _globalPackagesPath);
        }

        public NuGetExeClient WithSourceType(SourceType sourceType)
        {
            return new NuGetExeClient(
                _testSettings,
                _galleryClient,
                _version,
                sourceType,
                _httpCachePath,
                _globalPackagesPath);
        }

        private string GetSource()
        {
            if (_sourceType == SourceType.V2)
            {
                return $"{_galleryClient.GetGalleryServiceBaseUrl()}/api/v2";
            }

            return _testSettings.V3IndexUrl;
        }

        public async Task<CommandRunnerResult> RestoreAsync(string projectPath, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "restore",
                projectPath,
                "-Source",
                GetSource(),
            };

            var projectDirectory = Path.GetDirectoryName(projectPath);

            return await RunAsync(projectDirectory, arguments, logger);
        }

        public async Task<CommandRunnerResult> InstallLatestAsync(string id, bool prerelease, string outputDirectory, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "install",
                id,
                "-Source",
                GetSource(),
                "-OutputDirectory",
                outputDirectory,
            };

            if (prerelease)
            {
                arguments.Add("-Prerelease");
            }

            return await RunAsync(outputDirectory, arguments, logger);
        }

        public async Task<CommandRunnerResult> InstallAsync(string id, string version, string outputDirectory, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "install",
                id,
                "-Version",
                version,
                "-Source",
                GetSource(),
                "-OutputDirectory",
                outputDirectory,
            };

            return await RunAsync(outputDirectory, arguments, logger);
        }

        public Task<CommandRunnerResult> VerifyAsync(string workingDirectory, string packagePath, ITestOutputHelper logger)
        {
            var arguments = new List<string>
            {
                "verify",
                "-All",
                "-Verbosity",
                "detailed",
                packagePath,
            };

            return RunAsync(workingDirectory, arguments, logger);
        }

        private async Task<CommandRunnerResult> RunAsync(string workingDirectory, List<string> arguments, ITestOutputHelper logger)
        {
            var fileName = $"nuget.{_version}.exe";
            var filePath = Path.Combine("Support", "NuGetExe", fileName);

            var joinedArguments = string.Join(" ", arguments);

            var environmentVariables = new Dictionary<string, string>
            {
                { "NUGET_PACKAGES", _globalPackagesPath },
                { "NUGET_HTTP_CACHE_PATH", _httpCachePath },
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
