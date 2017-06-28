// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Convenience methods for getting data from HTTP.
    /// </summary>
    public class SimpleHttpClient
    {
        private const int MaxRetries = 3;

        public Task<T> GetJsonAsync<T>(string url, ITestOutputHelper logger)
        {
            return GetJsonAsync<T>(url, allowNotFound: false, logger: logger);
        }

        public async Task<T> GetJsonAsync<T>(string url, bool allowNotFound, ITestOutputHelper logger)
        {
            int retryCount = 0;
            do
            {
                try
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
                            if (logger != null)
                            {
                                // Make sure the JSON is a single line.
                                var parsedJson = JToken.Parse(json);
                                logger.WriteLine($" - URL: {url}{Environment.NewLine} - Response: {parsedJson.ToString(Formatting.None)}");
                            }

                            return JsonConvert.DeserializeObject<T>(json);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.WriteLine($"Request for {url} failed with exception: {e}");
                    retryCount++;
                    if (retryCount == MaxRetries)
                    {
                        throw;
                    }
                }
            }
            while (true);
        }
    }
}
