// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class SearchResultTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public SearchResultTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Fact]
        public async Task RegistrationValuesInResultMatchRequestSemVerLevel()
        {
            // Arrange
            var semVer1RegistrationAddresses = await _clients.V3Index.GetRegistrationBaseUrlsAsyncForSearch(_logger);
            var semVer2RegistrationAddresses = await _clients.V3Index.GetSemVer2RegistrationBaseUrlsAsyncForSearch(_logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);
            
            var semVer2Package = await _pushedPackages.PrepareAsync(PackageType.SemVer2Prerel, _logger);
            var semVer1Package = await _pushedPackages.PrepareAsync(PackageType.SemVer1Stable, _logger);

            // wait for all packages to become available to ensure that we have results.
            await _clients.Registration.WaitForPackageAsync(semVer2Package.Id, semVer2Package.FullVersion, semVer2: true, logger: _logger);
            await _clients.V2V3Search.WaitForPackageAsync(semVer2Package.Id, semVer2Package.FullVersion, _logger);
            await _clients.Registration.WaitForPackageAsync(semVer1Package.Id, semVer1Package.FullVersion, semVer2: false, logger: _logger);
            await _clients.V2V3Search.WaitForPackageAsync(semVer1Package.Id, semVer1Package.FullVersion, _logger);

            foreach (var searchService in searchServices)
            {
                // Search service changes the registration URL scheme to match the request URL. Modify what we found in
                // the index.json so match this.
                var semVer1Reg = semVer1RegistrationAddresses.Select(u => MatchSchemeAndPort(searchService.Uri, u));
                var semVer2Reg = semVer2RegistrationAddresses.Select(u => MatchSchemeAndPort(searchService.Uri, u));

                // Act
                var semVer1Query = await _clients.V2V3Search.QueryAsync(searchService, $"q=", _logger);
                var semVer2Query = await _clients.V2V3Search.QueryAsync(searchService, $"q=&semVerLevel=2.0.0", _logger);

                // Assert
                Assert.True(semVer1Query.Data.Count > 0);
                Assert.True(semVer2Query.Data.Count > 0);

                for (var i = 0; i < semVer1Query.Data.Count; i++)
                {
                    Assert.False(
                        semVer2Reg.Any(a => semVer1Query.Data[i].Registration.Contains(a)),
                        $"{semVer1Query.Data[i].Id} has a SemVer 2.0.0 registration base URL.");
                    Assert.True(
                        semVer1Reg.Any(a => semVer1Query.Data[i].Registration.Contains(a)),
                        $"{semVer1Query.Data[i].Id} has should have an expected registration base URL.");
                }

                for (var i = 0; i < semVer2Query.Data.Count; i++)
                {
                    Assert.True(
                        semVer2Reg.Any(a => semVer2Query.Data[i].Registration.Contains(a)),
                        $"{semVer2Query.Data[i].Id} should have an expected SemVer 2.0.0 registration base URL.");
                }
            }
        }

        private static string MatchSchemeAndPort(Uri reference, string toChange)
        {
            var builder = new UriBuilder(toChange);
            builder.Scheme = reference.Scheme;
            builder.Port = reference.Port;
            return builder.Uri.ToString();
        }
    }
}
