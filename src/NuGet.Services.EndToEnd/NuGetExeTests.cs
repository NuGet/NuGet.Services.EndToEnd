// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    /// <summary>
    /// Tests that integrate with nuget.exe, since this exercises our primary client code.
    /// </summary>
    [Collection(nameof(PushedPackagesCollection))]
    public class NuGetExeTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public NuGetExeTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _logger = logger;
        }

        [SemVer2Theory]
        [MemberData(nameof(PackageTypeAndSourceTypes))]
        public async Task LatestNuGetExeCanInstallPackage(PackageType packageType, bool semVer2, SourceType sourceType)
        {
            // Arrange
            using (var testDirectory = TestDirectory.Create())
            {
                var outputDirectory = Path.Combine(testDirectory, "output");
                Directory.CreateDirectory(outputDirectory);
                var package = await _pushedPackages.PrepareAsync(packageType, _logger);
                var nuGetExe = PrepareNuGetExe(testDirectory)
                    .WithSourceType(sourceType);

                await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
                await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2, _logger);
                await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

                // Act
                var result = await nuGetExe.InstallAsync(
                    package.Id,
                    package.NormalizedVersion,
                    outputDirectory,
                    _logger);

                // Assert
                var expectedPath = Path.Combine(
                    outputDirectory,
                    $"{package.Id}.{package.NormalizedVersion}",
                    $"{package.Id}.{package.NormalizedVersion}.nupkg");
                Assert.True(
                    File.Exists(expectedPath),
                    $"The installed package was expected to be at path {expectedPath} but was not.");

                var bytes = File.ReadAllBytes(expectedPath);
                Assert.Equal(package.NupkgBytes.Count, bytes.Length);
                Assert.Equal(package.NupkgBytes, bytes);
                Assert.Equal(0, result.ExitCode);
            }
        }

        [SemVer2Theory]
        [MemberData(nameof(SemVer2PackageTypes))]
        public async Task Pre430NuGetExeCannotInstallSemVer2Packages(PackageType packageType, SourceType sourceType)
        {
            // Arrange
            using (var testDirectory = TestDirectory.Create())
            {
                var outputDirectory = Path.Combine(testDirectory, "output");
                Directory.CreateDirectory(outputDirectory);
                var package = await _pushedPackages.PrepareAsync(packageType, _logger);
                var nuGetExe = PrepareNuGetExe(testDirectory)
                    .WithVersion("4.1.0")
                    .WithSourceType(sourceType);

                await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
                await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: true, logger: _logger);
                await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

                // Act
                var result = await nuGetExe.InstallAsync(
                    package.Id,
                    package.NormalizedVersion,
                    outputDirectory,
                    _logger);

                // Assert
                var expectedPath = Path.Combine(
                    outputDirectory,
                    $"{package.Id}.{package.NormalizedVersion}",
                    $"{package.Id}.{package.NormalizedVersion}.nupkg");
                Assert.False(
                    File.Exists(expectedPath),
                    $"The package installation should not have occurred but a package was found at path {expectedPath}.");
                
                Assert.NotEqual(0, result.ExitCode);
            }
        }

        private NuGetExeClient PrepareNuGetExe(TestDirectory testDirectory)
        {
            return _clients
                .NuGetExe
                .WithGlobalPackagesPath(Path.Combine(testDirectory, "global-packages"))
                .WithHttpCachePath(Path.Combine(testDirectory, "http-cache"));
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
