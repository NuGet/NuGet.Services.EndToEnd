// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.EndToEnd
{
    public class BasicTests
    {
        [Fact]
        public async Task NuGetDotOrgIsReachableOverHttps()
        {
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync("https://www.nuget.org/"))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
