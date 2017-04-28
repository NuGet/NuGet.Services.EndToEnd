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
    public class SearchResultTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public SearchResultTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _logger = logger;
        }

        [SemVer2Fact]
        public async Task RegistrationValuesInResultMatchRequestSemVerLevel()
        {
            // Arrange
            var allRegistrationAddresses = await _clients.V3Index.GetRegistrationBaseUrls();
            var semVer2RegistrationAddresses = await _clients.V3Index.GetSemVer2RegistrationBaseUrls();
            var searchBaseAddresses = await _clients.V3Index.GetSearchBaseUrls();
            
            var semVer2Package = await _pushedPackages.PushAsync(PackageType.SemVer2Prerelease, _logger);
            var semVer1Package = await _pushedPackages.PushAsync(PackageType.SemVer1Stable, _logger);

            // wait for all packages to become available to ensure that we have results.
            await _clients.Registration.WaitForPackageAsync(semVer2Package.Id, semVer2Package.Version, semVer2: true, logger: _logger);
            await _clients.V3Search.WaitForPackageAsync(semVer2Package.Id, semVer2Package.Version, _logger);
            await _clients.Registration.WaitForPackageAsync(semVer1Package.Id, semVer1Package.Version, semVer2: false, logger: _logger);
            await _clients.V3Search.WaitForPackageAsync(semVer1Package.Id, semVer1Package.Version, _logger);

            foreach (var searchBaseAddress in searchBaseAddresses)
            {
                // Act
                var semVer1Query = await _clients.V3Search.QueryAsync(searchBaseAddress, $"q=", _logger);
                var semVer2Query = await _clients.V3Search.QueryAsync(searchBaseAddress, $"q=&semVerLevel=2.0.0", _logger);

                // Assert
                Assert.True(semVer1Query.Data.Count > 0);
                Assert.True(semVer2Query.Data.Count > 0);

                for (var i = 0; i < semVer1Query.Data.Count; i++)
                {
                    Assert.False(semVer2RegistrationAddresses.Any(a => semVer1Query.Data[i].Registration.Contains(a)));
                    Assert.True(allRegistrationAddresses.Any(a => semVer1Query.Data[i].Registration.Contains(a)));
                }

                for (var i = 0; i < semVer2Query.Data.Count; i++)
                {
                    Assert.True(semVer2RegistrationAddresses.Any(a => semVer2Query.Data[i].Registration.Contains(a)));
                }
            }
        }
    }
}
