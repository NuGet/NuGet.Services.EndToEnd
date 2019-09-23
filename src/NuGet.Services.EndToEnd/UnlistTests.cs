// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class UnlistTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public UnlistTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        /// <summary>
        /// A package is by default listed in registration right after initial push.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1Stable, false)]
        [InlineData(PackageType.SemVer2Prerel, true)]
        public async Task PackageInitiallyShowsAsListedInRegistration(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act & Assert
            await _clients.Registration.WaitForListedStateAsync(package.Id, package.FullVersion, semVer2, listed: true, logger: _logger);
        }

        /// <summary>
        /// An unlisted package should have indicate that it is unlisted in the registration blobs.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1StableUnlisted, false)]
        [InlineData(PackageType.SemVer2PrerelUnlisted, true)]
        [InlineData(PackageType.SemVer2StableMetadataUnlisted, true)]
        public async Task UnlistedPackageShowsAsUnlistedInRegistration(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act & Assert
            await _clients.Registration.WaitForListedStateAsync(package.Id, package.FullVersion, semVer2, listed: false, logger: _logger);
        }

        /// <summary>
        /// An unlisted package should not appear in search results.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1StableUnlisted)]
        [InlineData(PackageType.SemVer2PrerelUnlisted)]
        [InlineData(PackageType.SemVer2StableMetadataUnlisted)]
        public async Task UnlistedPackageIsHiddenFromSearch(PackageType packageType)
        {
            // Arrange
            var listed = false;
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            await _clients.V2V3Search.WaitForListedStateAsync(package.Id, package.FullVersion, listed, _logger);

            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            foreach (var searchService in searchServices)
            {
                // Act
                var results = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}&prerelease=true&semVerLevel=2.0.0",
                    _logger);

                // Assert
                var versionCount = results
                    .Data
                    .Sum(x => x.Versions.Count());
                Assert.Equal(0, versionCount);
            }
        }
    }
}
