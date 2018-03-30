﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(CommonCollection))]
    public class ConnectivityTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;
        private readonly TestSettings _testSettings;

        public ConnectivityTests(CommonFixture commonFixture, ITestOutputHelper logger)
        {
            _testSettings = commonFixture.TestSettings;
            _clients = commonFixture.Clients;
            _logger = logger;
        }

        [Fact]
        public async Task GalleryIsReachable()
        {
            var galleryUrl = await _clients.Gallery.GetGalleryUrlAsync(_logger);
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(galleryUrl))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task V3IndexIsReachable()
        {
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(_testSettings.V3IndexUrl))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task SearchIsReachable()
        {
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);
            foreach (var searchService in searchServices)
            {
                _logger.WriteLine($"Verifying connectivity to search base URL: {searchService.Uri.AbsoluteUri}");

                using (var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(searchService.Uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
    }
}
