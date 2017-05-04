// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    /// <summary>
    /// Tests that integrate with nuget.exe, since this exercises our primary client code.
    /// </summary>
    [Collection(nameof(PushedPackagesCollection))]
    public class NuGetExeTests : IClassFixture<TrustedHttpsCertificatesFixture>, IDisposable
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;
        private readonly TestDirectory _testDirectory;
        private readonly string _outputDirectory;

        public NuGetExeTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _logger = logger;
            _testDirectory = TestDirectory.Create();
            _outputDirectory = Path.Combine(_testDirectory, "output");
            Directory.CreateDirectory(_outputDirectory);
        }

        public void Dispose()
        {
            _testDirectory.Dispose();
        }

        [SemVer2Theory]
        [MemberData(nameof(PackageTypeAndSourceTypes))]
        public async Task LatestNuGetExeCanRestorePackage(PackageType packageType, bool semVer2, SourceType sourceType)
        {
            // Arrange
            var nuGetExe = PrepareNuGetExe(sourceType);
            var package = await PreparePackageAsync(packageType, semVer2);
            var projectPath = WriteProjectWithDependency(package.Id, package.NormalizedVersion);
            var projectDirectory = Path.GetDirectoryName(projectPath);

            // Act
            var result = await nuGetExe.RestoreAsync(projectPath, _logger);

            // Assert
            VerifyRestored(projectDirectory, package.Id, package.NormalizedVersion);
            Assert.Equal(0, result.ExitCode);
        }

        [SemVer2Theory]
        [MemberData(nameof(PackageTypeAndSourceTypes))]
        public async Task LatestNuGetExeCanInstallPackage(PackageType packageType, bool semVer2, SourceType sourceType)
        {
            // Arrange
            var nuGetExe = PrepareNuGetExe(sourceType);
            var package = await PreparePackageAsync(packageType, semVer2);

            // Act
            var result = await nuGetExe.InstallAsync(
                package.Id,
                package.NormalizedVersion,
                _outputDirectory,
                _logger);

            // Assert
            VerifyInstalled(package);
            Assert.Equal(0, result.ExitCode);
        }

        [SemVer2Theory]
        [MemberData(nameof(PackageTypeAndSourceTypes))]
        public async Task LatestNuGetExeCanInstallLatestPackage(PackageType packageType, bool semVer2, SourceType sourceType)
        {
            // Arrange
            var nuGetExe = PrepareNuGetExe(sourceType);
            var prerelease = true;
            var package = await PreparePackageAsync(packageType, semVer2);

            // Act
            var result = await nuGetExe.InstallLatestAsync(
                package.Id,
                prerelease,
                _outputDirectory,
                _logger);

            // Assert
            VerifyInstalled(package);
            Assert.Equal(0, result.ExitCode);
        }

        [SemVer2Theory]
        [MemberData(nameof(SemVer2PackageTypes))]
        public async Task Pre430NuGetExeCannotInstallLatestSemVer2Packages(PackageType packageType, SourceType sourceType)
        {
            // Arrange
            var nuGetExe = PrepareNuGetExe(sourceType, "4.1.0");
            var prerelease = true;
            var package = await PreparePackageAsync(packageType, semVer2: true);

            // Act
            var result = await nuGetExe.InstallLatestAsync(
                package.Id,
                prerelease,
                _outputDirectory,
                _logger);

            // Assert
            VerifyNotInstalled(package);
            Assert.NotEqual(0, result.ExitCode);
        }

        private string WriteProjectWithDependency(string id, string version)
        {
            var path = Path.Combine(
                _testDirectory,
                "project",
                "TestProject.csproj");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllText(
                path,
                $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net463</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""{id}"" Version=""{version}"" />
  </ItemGroup>
</Project>
");

            return path;
        }

        private async Task<Package> PreparePackageAsync(PackageType packageType, bool semVer2)
        {
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2, _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

            return package;
        }

        private void VerifyNotInstalled(Package package)
        {
            var expectedPath = Path.Combine(
                _outputDirectory,
                $"{package.Id}.{package.NormalizedVersion}",
                $"{package.Id}.{package.NormalizedVersion}.nupkg");
            Assert.False(
                File.Exists(expectedPath),
                $"The package installation should not have occurred but a package was found at path {expectedPath}.");
        }

        private void VerifyInstalled(Package package)
        {
            var expectedPath = Path.Combine(
                _outputDirectory,
                $"{package.Id}.{package.NormalizedVersion}",
                $"{package.Id}.{package.NormalizedVersion}.nupkg");
            Assert.True(
                File.Exists(expectedPath),
                $"The installed package was expected to be at path {expectedPath} but was not.");

            var bytes = File.ReadAllBytes(expectedPath);
            Assert.Equal(package.NupkgBytes.Count, bytes.Length);
            Assert.Equal(package.NupkgBytes, bytes);
        }

        private void VerifyRestored(string projectDirectory, string id, string version)
        {
            var assetsFilePath = Path.Combine(
                projectDirectory,
                "obj",
                "project.assets.json");
            Assert.True(
                File.Exists(assetsFilePath),
                $"There should be an assets file (restore output) at path {assetsFilePath}.");

            var assetsJson = JObject.Parse(File.ReadAllText(assetsFilePath));
            var libraries = assetsJson["libraries"].Value<JObject>();
            var libraryIdentities = libraries
                .Properties()
                .Select(p => p.Name)
                .ToList();
            Assert.Contains($"{id}/{version}", libraryIdentities, StringComparer.OrdinalIgnoreCase);
        }

        private NuGetExeClient PrepareNuGetExe(SourceType sourceType)
        {
            return PrepareNuGetExe(sourceType, version: null);
        }

        private NuGetExeClient PrepareNuGetExe(SourceType sourceType, string version)
        {
            return _clients
                .NuGetExe
                .WithVersion(version)
                .WithSourceType(sourceType)
                .WithGlobalPackagesPath(Path.Combine(_testDirectory, "global-packages"))
                .WithHttpCachePath(Path.Combine(_testDirectory, "http-cache"));
        }

        public static IEnumerable<object[]> SemVer2PackageTypes
        {
            get
            {
                return GetTestDataRows()
                    .Where(x => x.SemVer2)
                    .Select(x => new object[] { x.PackageType, x.SourceType });
            }
        }

        public static IEnumerable<object[]> PackageTypeAndSourceTypes
        {
            get
            {
                return GetTestDataRows()
                    .Select(x => new object[] { x.PackageType, x.SemVer2, x.SourceType });
            }
        }

        private static IEnumerable<PackageTypeAndSourceType> GetTestDataRows()
        {
            var packageTypes = new PackageTypeAndSourceType[]
            {
                    new PackageTypeAndSourceType { PackageType = PackageType.SemVer1Stable, SemVer2 = false },
                    new PackageTypeAndSourceType { PackageType = PackageType.SemVer2Prerel, SemVer2 = true },
                    new PackageTypeAndSourceType { PackageType = PackageType.SemVer2StableMetadata, SemVer2 = true },
            };

            var sourceTypes = new[]
            {
                    SourceType.V2,
                    SourceType.V3,
                };

            var rows =
                from pt in packageTypes
                from st in sourceTypes
                select new PackageTypeAndSourceType
                {
                    PackageType = pt.PackageType,
                    SemVer2 = pt.SemVer2,
                    SourceType = st,
                };
            return rows;
        }

        public class PackageTypeAndSourceType
        {
            public PackageType PackageType { get; set; }
            public bool SemVer2 { get; set; }
            public SourceType SourceType { get; set; }
        }
    }
}
