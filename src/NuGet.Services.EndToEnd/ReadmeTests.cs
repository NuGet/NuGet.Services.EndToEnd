// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using NuGet.Services.EndToEnd.Support.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class ReadmeTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public ReadmeTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Fact]
        public async Task PushedPackageWithReadmeIsAvailableInRegistration()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.EmbeddedReadmeFile, _logger);
            var expectedPath = GetExpectedReadmePath(package);

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.EndsWith(expectedPath, x.CatalogEntry.ReadmeUrl));
        }

        [Fact]
        public async Task PushedPackageWithReadmeIsAvailableInFlatContainer()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.EmbeddedReadmeFile, _logger);
            var expectedContent = GetReadmeData();
            var expectedPath = GetExpectedReadmePath(package);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            var readmeContents = await _clients.FlatContainer.TryAndGetFileStringContent(package.Id, package.NormalizedVersion, FlatContainerContentType.Readme, _logger);
            Assert.All(readmeContents, x => Assert.Equal(expectedContent, x));
        }

        [Fact]
        public async Task PushedPackageWithReadmeIsAvailableInSearch()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.EmbeddedReadmeFile, _logger);

            // Act & Assert
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            foreach (var searchService in searchServices)
            {
                var results = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}",
                    _logger);

                var data = Assert.Single(results.Data);
                Assert.Equal(package.NormalizedVersion, data.Version);
                var version = Assert.Single(data.Versions);
                Assert.Equal(package.NormalizedVersion, version.Version);
            }
        }

        private static string GetExpectedReadmePath(Package package)
        {
            return $"/{package.Id}/{package.NormalizedVersion}#show-readme-container";
        }

        private static string GetReadmeData()
        {
            var sourceFilename = $"Readmes.readme.md";
            var expectedContent = TestDataResourceUtility.GetResourceStringContent(sourceFilename);
            return expectedContent;
        }
    }
}