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
        : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly TestSettings _testSettings;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public AutocompleteResultTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _testSettings = TestSettings.Create();
            _logger = logger;
        }

        [Theory]
        [InlineData(PackageType.SemVer1Stable, true, null)]
        [InlineData(PackageType.SemVer1Stable, false, null)]
        [SemVer2InlineData(PackageType.SemVer2Prerel, true, "2.0.0")]
        [SemVer2InlineData(PackageType.SemVer2StableMetadata, false, "2.0.0")]
        public async Task V2PackageVersionsAutocompleteResultsMatchV3Results(
            PackageType packageType,
            bool includePrerelease,
            string semVerLevel)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act
            var v2Response = await _clients.Gallery.AutocompletePackageVersionsAsync(
                package.Id, 
                includePrerelease, 
                semVerLevel, 
                _logger);

            foreach (var v3Endpoint in await _clients.V2V3Search.GetSearchBaseUrlsAsync())
            {
                var v3Response = await _clients.V2V3Search.AutocompletePackageVersionsAsync(
                    v3Endpoint,
                    package.Id,
                    includePrerelease,
                    semVerLevel,
                    _logger);
                
                // Assert
                Assert.Equal(v2Response.Count, v3Response.Data.Count);
                Assert.Equal(v2Response, v3Response.Data);
            }
        }

        [Theory]
        [InlineData(PackageType.SemVer1Stable, true, null)]
        [InlineData(PackageType.SemVer1Stable, false, null)]
        [SemVer2InlineData(PackageType.SemVer2Prerel, true, "2.0.0")]
        [SemVer2InlineData(PackageType.SemVer2StableMetadata, false, "2.0.0")]
        public async Task V2PackageIdsAutocompleteResultsMatchV3Results(
            PackageType packageType,
            bool includePrerelease,
            string semVerLevel)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act
            var v2Response = await _clients.Gallery.AutocompletePackageIdsAsync(
                package.Id,
                includePrerelease,
                semVerLevel,
                _logger);

            foreach (var v3Endpoint in await _clients.V2V3Search.GetSearchBaseUrlsAsync())
            {
                var v3Response = await _clients.V2V3Search.AutocompletePackageIdsAsync(
                    v3Endpoint,
                    package.Id,
                    includePrerelease,
                    semVerLevel,
                    _logger);

                // Assert
                Assert.Equal(v2Response.Count, v3Response.Data.Count);
                Assert.Equal(v2Response, v3Response.Data);
            }
        }
    }
}
