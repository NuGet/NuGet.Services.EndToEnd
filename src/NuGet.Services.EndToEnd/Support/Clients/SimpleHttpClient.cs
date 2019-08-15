// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
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
            var json = await GetFileStringContentAsync(url, allowNotFound, logResponseBody: false, logger: logger);
            if (json == null)
            {
                return default(T);
            }

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

        public async Task<string> GetFileStringContentAsync(string url, bool allowNotFound, bool logResponseBody, ITestOutputHelper logger)
        {
            Action<string> logAction;
            if (logResponseBody)
            {
                logAction = content => logger.WriteLine($" - URL: {url}{Environment.NewLine} - Response: {content}");
            }
            else
            {
                logAction = content => { };
            }

            return await GetAndProcessContent(
                url,
                allowNotFound,
                logger,
                async (stream) => await StringFromStream(stream, logAction));
        }

        public async Task<byte[]> GetFileBinaryContentAsync(string url, bool allowNotFound, bool logResponseBody, ITestOutputHelper logger)
        {
            Action<byte[]> logAction;
            if (logResponseBody)
            {
                logAction = content => logger.WriteLine($" - URL: {url}{Environment.NewLine} - Response: {BytesToHex(content)}");
            }
            else
            {
                logAction = content => { };
            }

            return await GetAndProcessContent(
                url,
                allowNotFound,
                logger,
                async (stream) => await BytesFromStream(stream, logAction));
        }

        private async Task<TResult> GetAndProcessContent<TResult>(
            string url,
            bool allowNotFound,
            ITestOutputHelper logger,
            Func<Stream, Task<TResult>> getResult)
        {
            using (var httpClientHander = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(httpClientHander))
            using (var response = await httpClient.GetAsync(url))
            {
                if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default(TResult);
                }

                await response.EnsureSuccessStatusCodeOrLogAsync(url, logger);
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    return await getResult(stream);
                }
            }
        }

        private async Task<string> StringFromStream(Stream stream, Action<string> logContent)
        {
            using (var streamReader = new StreamReader(stream))
            {
                var fileStringContent = await streamReader.ReadToEndAsync();
                logContent(fileStringContent);
                return fileStringContent;
            }
        }

        private async Task<byte[]> BytesFromStream(Stream stream, Action<byte[]> logContent)
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                var result = ms.ToArray();
                logContent(result);
                return result;
            }
        }

        private static string BytesToHex(byte[] bytes, int? maxLength = null)
        {
            int outputLength = Math.Min(maxLength ?? 1024, bytes.Length * 2);
            var sb = new StringBuilder(outputLength);
            foreach (var @byte in bytes)
            {
                sb.AppendFormat("{0:x2}", @byte);
                outputLength -= 2;
                if (outputLength <= 0)
                {
                    break;
                }
            }
            return sb.ToString();
        }
    }
}
