// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Convenience methods for getting data from HTTP.
    /// </summary>
    public class SimpleHttpClient
    {
        private readonly JsonSerializer _serializer;

        internal SimpleHttpClient()
        {
            _serializer = JsonSerializer.Create(new JsonSerializerSettings()
            {
                Converters = new JsonConverter[]
                {
                    new NuGetVersionConverter()
                },
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public async Task<T> GetJsonAsync<T>(string url, ITestOutputHelper logger)
        {
            return await GetJsonAsync<T>(url, allowNotFound: false, logResponseBody: true, logger: logger);
        }

        public async Task<T> GetJsonAsync<T>(string url, bool allowNotFound, ITestOutputHelper logger)
        {
            return await GetJsonAsync<T>(url, allowNotFound, logResponseBody: true, logger: logger);
        }

        public async Task<T> GetJsonAsync<T>(string url, bool allowNotFound, bool logResponseBody, ITestOutputHelper logger)
        {
            using (var httpClientHander = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(httpClientHander))
            using (var response = await httpClient.GetAsync(url))
            {
                if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default(T);
                }

                await response.EnsureSuccessStatusCodeOrLogAsync(url, logger);

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(stream))
                {
                    var json = await streamReader.ReadToEndAsync();
                    if (logResponseBody)
                    {
                        // Make sure the JSON is a single line.
                        var parsedJson = JToken.Parse(json);
                        logger.WriteLine($" - URL: {url}{Environment.NewLine} - Response: {parsedJson.ToString(Formatting.None)}");
                    }

                    using (var stringReader = new StringReader(json))
                    using (var jsonReader = new JsonTextReader(stringReader))
                    {
                        return _serializer.Deserialize<T>(jsonReader);
                    }
                }
            }
        }
    }
}
