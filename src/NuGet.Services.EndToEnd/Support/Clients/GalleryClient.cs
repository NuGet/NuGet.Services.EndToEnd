// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuGet.Services.EndToEnd.Support
{
    public class GalleryClient
    {
        private readonly TestSettings _testSettings;

        public GalleryClient(TestSettings testSettings)
        {
            _testSettings = testSettings;
        }

        public async Task PushAsync(Stream nupkgStream)
        {
            var pushUrl = $"{_testSettings.GalleryBaseUrl}/api/v2/package";
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Put, pushUrl))
            {
                request.Headers.Add("X-NuGet-ApiKey", _testSettings.ApiKey);
                request.Content = new StreamContent(nupkgStream);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
