// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;

namespace NuGet.Services.EndToEnd
{
    public class ConnectivityTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        [Fact]
        public async Task NuGetDotOrgIsReachable()
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
    }
}
