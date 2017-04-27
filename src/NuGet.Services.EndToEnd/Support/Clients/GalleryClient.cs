// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A simple interface for interacting with the gallery.
    /// </summary>
    public class GalleryClient : IGalleryClient
    {
        private readonly TestSettings _testSettings;

        public GalleryClient(TestSettings testSettings)
        {
            _testSettings = testSettings;
        }

        public async Task PushAsync(Stream nupkgStream)
        {
            var url = $"{_testSettings.GalleryBaseUrl}/api/v2/package";
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Put, url))
            {
                request.Headers.Add("X-NuGet-ApiKey", _testSettings.ApiKey);
                request.Content = new StreamContent(nupkgStream);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task UnlistAsync(string id, string version)
        {
            var url = $"{_testSettings.GalleryBaseUrl}/api/v2/package/{id}/{version}";
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                request.Headers.Add("X-NuGet-ApiKey", _testSettings.ApiKey);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
