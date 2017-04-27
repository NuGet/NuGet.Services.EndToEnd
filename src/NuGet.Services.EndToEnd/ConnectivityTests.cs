// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    public class ConnectivityTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public ConnectivityTests(ITestOutputHelper logger)
        {
            _clients = Clients.Initialize();
            _logger = logger;
        }

        [Fact]
        public async Task GalleryIsReachable()
        {
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(EnvironmentSettings.GalleryBaseUrl))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task V3IndexIsReachable()
        {
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(EnvironmentSettings.V3IndexUrl))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task SearchIsReachable()
        {
            var searchBaseUrls = await _clients.V3Search.GetSearchBaseUrlsAsync();
            foreach (var searchBaseUrl in searchBaseUrls)
            {
                _logger.WriteLine($"Verifying connectivity to search base URL: {searchBaseUrl}");

                using (var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(searchBaseUrl))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
    }
}
