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
    public class IconTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public IconTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Theory]
        [InlineData(PackageType.EmbeddedIconJpeg)]
        [InlineData(PackageType.EmbeddedIconPng)]
        public async Task PushedPackageWithIconIsAvailableInRegistration(PackageType packageType)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            var expectedPath = GetExpectedIconPath(package);

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.EndsWith(expectedPath, x.CatalogEntry.IconUrl));
        }

        [Theory]
        [InlineData(PackageType.EmbeddedIconJpeg)]
        [InlineData(PackageType.EmbeddedIconPng)]
        public async Task PushedPackageWithIconIsAvailableInFlatContainer(PackageType packageType)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            var expectedContent = GetIconData(packageType);
            var expectedPath = GetExpectedIconPath(package);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            var iconContents = await _clients.FlatContainer.TryAndGetFileBinaryContent(package.Id, package.NormalizedVersion, FlatContainerContentType.Icon, _logger);
            Assert.All(iconContents, x => Assert.Equal(expectedContent, x));
        }

        [Theory]
        [InlineData(PackageType.EmbeddedIconJpeg)]
        [InlineData(PackageType.EmbeddedIconPng)]
        public async Task PushedPackageWithIconIsAvailableInSearch(PackageType packageType)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            var expectedPath = GetExpectedIconPath(package);

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
                Assert.EndsWith(expectedPath, data.IconUrl);
                Assert.Equal(package.NormalizedVersion, data.Version);
                var version = Assert.Single(data.Versions);
                Assert.Equal(package.NormalizedVersion, version.Version);
            }
        }

        private static string GetExpectedIconPath(Package package)
        {
            return $"/{package.Id.ToLowerInvariant()}/{package.NormalizedVersion.ToLowerInvariant()}/icon";
        }

        private static byte[] GetIconData(PackageType packageType)
        {
            string sourceExtension;
            switch (packageType)
            {
                case PackageType.EmbeddedIconJpeg:
                    sourceExtension = ".jpg";
                    break;

                case PackageType.EmbeddedIconPng:
                    sourceExtension = ".png";
                    break;

                default:
                    throw new ArgumentException($"Unsupported package type {packageType}");
            }
            var sourceFilename = $"icon{sourceExtension}";
            var expectedContent = TestDataResourceUtility.GetResourceBytes($"Icons.{sourceFilename}");
            return expectedContent;
        }
    }
}
