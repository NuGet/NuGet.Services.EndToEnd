// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A simple interface for interacting with the gallery.
    /// </summary>
    public class GalleryClient : IGalleryClient
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private const string NuGetProtocolHeader = "X-NuGet-Protocol-Version";
        private const string NuGetProtocolVersion = "4.1.0";

        private readonly SimpleHttpClient _httpClient;
        private readonly TestSettings _testSettings;

        public GalleryClient(SimpleHttpClient httpClient, TestSettings testSettings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _testSettings = testSettings ?? throw new ArgumentNullException(nameof(testSettings));
        }

        public Uri GetGalleryServiceBaseUrl()
        {
            var serviceBaseUrl = _testSettings.GalleryConfiguration.ServiceDetails?.BaseUrl;
            if (serviceBaseUrl != null)
            {
                return new Uri(serviceBaseUrl);
            }

            return GetGalleryBaseUrl();
        }

        public Uri GetGalleryBaseUrl()
        {
            return new Uri(_testSettings.GalleryConfiguration.GalleryBaseUrl);
        }

        public async Task<IList<string>> AutocompletePackageIdsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger)
        {
            var galleryEndpoint = GetGalleryServiceBaseUrl();
            var serviceEndpoint = $"{galleryEndpoint}/api/v2/package-ids";
            var uri = AppendAutocompletePackageIdsQueryString(serviceEndpoint, id, includePrerelease, semVerLevel);

            return await _httpClient.GetJsonAsync<List<string>>(uri, logger);
        }

        public async Task<IList<string>> AutocompletePackageVersionsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger)
        {
            var galleryEndpoint = GetGalleryServiceBaseUrl();
            var serviceEndpoint = $"{galleryEndpoint}/api/v2/package-versions";
            var uri = AppendAutocompletePackageVersionsQueryString($"{serviceEndpoint}/{id}", includePrerelease, semVerLevel);

            return await _httpClient.GetJsonAsync<List<string>>(uri, logger);
        }

        public async Task PushAsync(Stream nupkgStream, ITestOutputHelper logger, PackageType packageType)
        {
            var galleryEndpoint = GetGalleryServiceBaseUrl();

            string url;
            switch (packageType)
            {
                case PackageType.SymbolsPackage:
                    url = $"{galleryEndpoint}/api/v2/symbolpackage";
                    break;
                default:
                    url = $"{galleryEndpoint}/api/v2/package";
                    break;
            }

            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Put, url))
            {
                // Use the default push timeout that the client uses.
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                request.Headers.Add(ApiKeyHeader, _testSettings.ApiKey);
                request.Headers.Add(NuGetProtocolHeader, NuGetProtocolVersion);

                request.Content = new StreamContent(nupkgStream);

                using (var response = await httpClient.SendAsync(request))
                {
                    await response.EnsureSuccessStatusCodeOrLogAsync(url, logger);
                }
            }
        }

        public async Task RelistAsync(string id, string version, ITestOutputHelper logger)
        {
            await SendToPackageAsync(HttpMethod.Post, id, version, logger);
        }

        public async Task UnlistAsync(string id, string version, ITestOutputHelper logger)
        {
            await SendToPackageAsync(HttpMethod.Delete, id, version, logger);
        }

        public async Task DeprecateAsync(string id, IReadOnlyCollection<string> versions, PackageDeprecationContext context, ITestOutputHelper logger)
        {
            var galleryEndpoint = GetGalleryServiceBaseUrl();
            var url = $"{galleryEndpoint}/api/v2/package/{id}/deprecations";

            var body = new
            {
                versions,
                isLegacy = context?.IsLegacy ?? false,
                hasCriticalBugs = context?.HasCriticalBugs ?? false,
                isOther = context?.IsOther ?? false,
                alternatePackageId = context?.AlternatePackageId,
                alternatePackageVersion = context?.AlternatePackageVersion,
                message = context?.Message
            };

            var bodyJson = JsonConvert.SerializeObject(body);
            await SendAsync(
                HttpMethod.Put, 
                url, 
                logger, 
                new StringContent(bodyJson, Encoding.UTF8, "application/json"));
        }

        public async Task SearchPackageODataV2FromDBAsync(string id, string normalizedVersion, ITestOutputHelper logger)
        {
            var galleryEndpoint = GetGalleryServiceBaseUrl();
            // Add 1 eq 1 to block the search hijack and execute the query against the database.
            var urlRootAndPath = $"{galleryEndpoint}/api/v2/Packages()";
            var queryParameters = new Dictionary<string, string>()
            {
                { "$filter", $"Id eq '{id}' and NormalizedVersion eq '{normalizedVersion}' and 1 eq 1" }
            };
            var url = QueryHelpers.AddQueryString(urlRootAndPath, queryParameters);

            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await httpClient.SendAsync(request))
            {
                await response.EnsureSuccessStatusCodeOrLogAsync(url, logger);
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Contains(id, responseContent);
            }
        }

        private static string AppendAutocompletePackageIdsQueryString(string uri, string id, bool? includePrerelease, string semVerLevel = null)
        {
            var queryParameters = new Dictionary<string, string>()
            {
                { "partialId", id },
                { "includePrerelease", includePrerelease?.ToString() ?? bool.FalseString }
            };

            NuGetVersion semVerLevelVersion;
            if (!string.IsNullOrEmpty(semVerLevel) && NuGetVersion.TryParse(semVerLevel, out semVerLevelVersion))
            {
                queryParameters.Add("semVerLevel", semVerLevel);
            }

            var queryStringBuilder = QueryHelpers.AddQueryString(uri, queryParameters);
            return queryStringBuilder.ToString();
        }

        private static string AppendAutocompletePackageVersionsQueryString(string uri, bool? includePrerelease, string semVerLevel = null)
        {
            var queryParameters = new Dictionary<string, string>()
            {
                { "includePrerelease", includePrerelease?.ToString() ?? bool.FalseString }
            };

            NuGetVersion semVerLevelVersion;
            if (!string.IsNullOrEmpty(semVerLevel) && NuGetVersion.TryParse(semVerLevel, out semVerLevelVersion))
            {
                queryParameters.Add("semVerLevel", semVerLevel);
            }

            var queryStringBuilder = QueryHelpers.AddQueryString(uri, queryParameters);
            return queryStringBuilder.ToString();
        }

        private async Task SendToPackageAsync(HttpMethod method, string id, string version, ITestOutputHelper logger)
        {
            var galleryEndpoint = GetGalleryServiceBaseUrl();
            var url = $"{galleryEndpoint}/api/v2/package/{id}/{version}";
            await SendAsync(method, url, logger);
        }

        private async Task SendAsync(HttpMethod method, string url, ITestOutputHelper logger, HttpContent content = null)
        {
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(method, url) { Content = content })
            {
                request.Headers.Add(ApiKeyHeader, _testSettings.ApiKey);

                using (var response = await httpClient.SendAsync(request))
                {
                    await response.EnsureSuccessStatusCodeOrLogAsync(url, logger);
                }
            }
        }
    }
}
