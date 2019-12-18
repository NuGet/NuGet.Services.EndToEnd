// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class PackageTypeTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public PackageTypeTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Fact]
        public async Task CanSearchByPackageType()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.DotnetTool, _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            // Act & Assert
            foreach (var searchService in searchServices)
            {
                var matchingResults = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}&packageType=DotnetTool",
                    _logger);
                var nonMatchingResults = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}&packageType=Dependency",
                    _logger);

                var result = Assert.Single(matchingResults.Data);
                Assert.Equal("DotnetTool", Assert.Single(result.PackageTypes).Name);
                Assert.Empty(nonMatchingResults.Data);
            }
        }

        [Fact]
        public async Task CanAutocompleteByPackageType()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.DotnetTool, _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            // Act & Assert
            foreach (var searchService in searchServices)
            {
                var matchingResults = await _clients.V2V3Search.AutocompleteAsync(
                    searchService,
                    $"q={package.Id}&packageType=DotnetTool",
                    _logger);
                var nonMatchingResults = await _clients.V2V3Search.AutocompleteAsync(
                    searchService,
                    $"q={package.Id}&packageType=Dependency",
                    _logger);

                var result = Assert.Single(matchingResults.Data);
                Assert.Equal(package.Id, result);
                Assert.Empty(nonMatchingResults.Data);
            }
        }

        [Fact]
        public async Task PackagesWithNoPackageTypeAreDependency()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.SemVer1Stable, _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            // Act & Assert
            foreach (var searchService in searchServices)
            {
                var matchingResults = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}&packageType=Dependency",
                    _logger);
                var nonMatchingResults = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}&packageType=DotnetTool",
                    _logger);

                var result = Assert.Single(matchingResults.Data);
                Assert.Equal("Dependency", Assert.Single(result.PackageTypes).Name);
                Assert.Empty(nonMatchingResults.Data);
            }
        }

        [Fact]
        public async Task PackagesWithPackageTypeAppearWithoutPackageTypeFilter()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.DotnetTool, _logger);
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            // Act & Assert
            foreach (var searchService in searchServices)
            {
                var results = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}",
                    _logger);

                var result = Assert.Single(results.Data);
                Assert.Equal("DotnetTool", Assert.Single(result.PackageTypes).Name);
            }
        }
    }
}
