// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class PushTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public PushTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _logger = logger;
        }

        /// <summary>
        /// Push a package to the gallery and wait for it to be available in V3.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1Stable, false)]
        [InlineData(PackageType.SemVer2Prerel, true)]
        [InlineData(PackageType.SemVer2StableMetadata, true)]
        public async Task NewlyPushedIsAvailableInV3(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2, logger: _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
        }

        [Theory]
        [InlineData(PackageType.SemVer2Prerel)]
        [InlineData(PackageType.SemVer2StableMetadata)]
        public async Task NewSemVer2PackageIsFiltered(PackageType packageType)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            var searchBaseAddresses = await _clients.V2V3Search.GetSearchBaseUrlsAsync();

            // Wait for package to become available
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);

            // Act
            foreach (var searchBaseAddress in searchBaseAddresses)
            {
                var shouldBeEmptyV3 = await _clients.V2V3Search.QueryAsync(searchBaseAddress, $"q=packageid:{package.Id}&prerelease=true", _logger);
                var shouldBeEmptyAutocomplete = await _clients.V2V3Search.AutocompletePackageIdsAsync(
                    searchBaseAddress,
                    package.Id,
                    includePrerelease: true,
                    semVerLevel: null,
                    logger: _logger);

                var shouldNotBeEmptyV3 = await _clients.V2V3Search.QueryAsync(searchBaseAddress, $"q=packageid:{package.Id}&semVerLevel=2.0.0&prerelease=true", _logger);
                var shouldNotBeEmptyAutocomplete = await _clients.V2V3Search.AutocompletePackageIdsAsync(
                    searchBaseAddress,
                    package.Id,
                    includePrerelease: true,
                    semVerLevel: "2.0.0",
                    logger: _logger);

                // Assert
                Assert.Equal(0, shouldBeEmptyV3.Data.Count);
                Assert.Equal(0, shouldBeEmptyAutocomplete.Data.Count);
                Assert.Equal(1, shouldNotBeEmptyV3.Data.Count);
                Assert.Equal(1, shouldNotBeEmptyAutocomplete.Data.Count);
            }
        }
    }
}
