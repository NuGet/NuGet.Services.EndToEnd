// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class PushTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;
        private readonly TestSettings _testSettings;

        public PushTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _testSettings = pushedPackages.TestSettings;
            _logger = logger;
        }

        /// <summary>
        /// Push a package to the gallery and wait for it to be available in V3.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1Stable, false)]
        [InlineData(PackageType.SemVer2Prerel, true)]
        [InlineData(PackageType.SemVer2StableMetadata, true)]
        [InlineData(PackageType.FullValidation, false)]
        public async Task NewlyPushedIsAvailableInV3(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2, logger: _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
        }

        [SignedPackageTestFact]
        public async Task NewlyPushedSignedPackageIsAvailableInV3()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.Signed, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
        }

        [Theory]
        [InlineData(PackageType.SemVer2Prerel)]
        [InlineData(PackageType.SemVer2StableMetadata)]
        public async Task NewSemVer2PackageIsFiltered(PackageType packageType)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            // Wait for package to become available
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

            // Act
            foreach (var searchService in searchServices)
            {
                var shouldBeEmptyV3 = await _clients.V2V3Search.QueryAsync(searchService, $"q=packageid:{package.Id}&prerelease=true", _logger);
                var shouldBeEmptyAutocomplete = await _clients.V2V3Search.AutocompletePackageIdsAsync(
                    searchService,
                    package.Id,
                    includePrerelease: true,
                    semVerLevel: null,
                    logger: _logger);

                var shouldNotBeEmptyV3 = await _clients.V2V3Search.QueryAsync(searchService, $"q=packageid:{package.Id}&semVerLevel=2.0.0&prerelease=true", _logger);
                var shouldNotBeEmptyAutocomplete = await _clients.V2V3Search.AutocompletePackageIdsAsync(
                    searchService,
                    package.Id,
                    includePrerelease: true,
                    semVerLevel: "2.0.0",
                    logger: _logger);

                // Assert
                Assert.Empty(shouldBeEmptyV3.Data);
                Assert.Empty(shouldBeEmptyAutocomplete.Data);
                Assert.Single(shouldNotBeEmptyV3.Data);
                Assert.Single(shouldNotBeEmptyAutocomplete.Data);
            }
        }

        /// <summary>
        /// Push a package to the gallery and search in OData with non-hijacked query.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1Stable)]
        public async Task NewlyPushedIsODataSearchableInDB(PackageType packageType)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            await _clients.Gallery.SearchPackageODataV2FromDBAsync(package.Id, package.NormalizedVersion, _logger);
        }

        /// <summary>
        /// Push a package to the gallery and verify the owner information is propagated.
        /// </summary>
        [Fact]
        public async Task NewlyPushedPackageHasOwnerInformation()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.SemVer1Stable, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            // wait for package to become available
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

            // Act
            foreach (var searchService in searchServices)
            {
                var result = await _clients.V2V3Search.QueryAsync(searchService, $"q=packageid:{package.Id} owner:{_testSettings.TestAccountOwner}", _logger);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Data);
                Assert.Single(result.Data);
                Assert.Equal(package.Id, result.Data[0].Id);
            }
        }
    }
}
