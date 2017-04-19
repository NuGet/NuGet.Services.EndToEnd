// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Convenience methods for getting data from HTTP.
    /// </summary>
    public class SimpleHttpClient
    {
        public async Task<T> GetJsonAsync<T>(string url)
        {
            return await GetJsonAsync<T>(url, allowNotFound: false);
        }

        public async Task<T> GetJsonAsync<T>(string url, bool allowNotFound)
        {
            using (var httpClientHander = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(httpClientHander))
            using (var response = await httpClient.GetAsync(url))
            {
                if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default(T);
                }

                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(stream))
                {
                    var json = await streamReader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
        }
    }
}
