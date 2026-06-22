// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class AutocompleteResultTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public AutocompleteResultTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [GalleryTestTheory]
        [InlineData(PackageType.SemVer1Stable, true, null)]
        [InlineData(PackageType.SemVer1Stable, false, null)]
        [InlineData(PackageType.SemVer2Prerel, true, "2.0.0")]
        [InlineData(PackageType.SemVer2StableMetadata, false, "2.0.0")]
        public async Task V2PackageVersionsAutocompleteResultsMatchV3Results(
            PackageType packageType,
            bool includePrerelease,
            string semVerLevel)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            
            // Wait for package to become available
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

            // Act
            var v2Response = await _clients.Gallery.AutocompletePackageVersionsAsync(
                package.Id, 
                includePrerelease, 
                semVerLevel, 
                _logger);

            // Assert
            Assert.NotEmpty(v2Response);

            foreach (var searchService in await _clients.V2V3Search.GetSearchServicesAsync(_logger))
            {
                var v3Response = await _clients.V2V3Search.AutocompletePackageVersionsAsync(
                    searchService,
                    package.Id,
                    includePrerelease,
                    semVerLevel,
                    _logger);

                Assert.NotEmpty(v3Response.Data);
                Assert.Equal(v2Response.Count, v3Response.Data.Count);
                Assert.Equal(v2Response, v3Response.Data);
            }
        }

        [GalleryTestTheory]
        [InlineData(PackageType.SemVer1Stable, true, null)]
        [InlineData(PackageType.SemVer1Stable, false, null)]
        [InlineData(PackageType.SemVer2Prerel, true, "2.0.0")]
        [InlineData(PackageType.SemVer2StableMetadata, false, "2.0.0")]
        public async Task V2PackageIdsAutocompleteResultsMatchV3Results(
            PackageType packageType,
            bool includePrerelease,
            string semVerLevel)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Wait for package to become available
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

            // Act
            var v2Response = await _clients.Gallery.AutocompletePackageIdsAsync(
                package.Id,
                includePrerelease,
                semVerLevel,
                _logger);

            // Assert
            Assert.NotEmpty(v2Response);

            foreach (var searchService in await _clients.V2V3Search.GetSearchServicesAsync(_logger))
            {
                var v3Response = await _clients.V2V3Search.AutocompletePackageIdsAsync(
                    searchService,
                    package.Id,
                    includePrerelease,
                    semVerLevel,
                    _logger);

                Assert.NotEmpty(v3Response.Data);
                Assert.Equal(v2Response.Count, v3Response.Data.Count);
                Assert.Equal(v2Response, v3Response.Data);
            }
        }
    }
}
