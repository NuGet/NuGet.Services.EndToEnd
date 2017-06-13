// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private const string ApiKeyHeader = "X-NuGet-ApiKey";

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
                // Use the default push timeout that the client uses.
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                request.Headers.Add(ApiKeyHeader, _testSettings.ApiKey);
                request.Content = new StreamContent(nupkgStream);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task RelistAsync(string id, string version)
        {
            await SendToPackageAsync(HttpMethod.Post, id, version);
        }

        public async Task UnlistAsync(string id, string version)
        {
            await SendToPackageAsync(HttpMethod.Delete, id, version);
        }

        private async Task SendToPackageAsync(HttpMethod method, string id, string version)
        {
            var url = $"{_testSettings.GalleryBaseUrl}/api/v2/package/{id}/{version}";
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(method, url))
            {
                request.Headers.Add(ApiKeyHeader, _testSettings.ApiKey);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
