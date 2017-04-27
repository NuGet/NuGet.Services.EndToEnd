// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    public class SearchResultTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public SearchResultTests(ITestOutputHelper logger)
        {
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

            foreach (var searchBaseAddress in searchBaseAddresses)
            {
                // Act
                var semVer1Query = await _clients.V3Search.QueryAsync(searchBaseAddress, $"q=");
                var semVer2Query = await _clients.V3Search.QueryAsync(searchBaseAddress, $"q=&semVerLevel=2.0.0");

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
