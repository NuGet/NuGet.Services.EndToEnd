// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Versioning;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class V3IndexClient
    {
        private static readonly NuGetVersion Version430Alpha = NuGetVersion.Parse("4.3.0-alpha");

        private readonly TestSettings _testSettings;
        private readonly SimpleHttpClient _httpClient;

        public V3IndexClient(SimpleHttpClient httpClient, TestSettings testSettings)
        {
            _httpClient = httpClient;
            _testSettings = testSettings;
        }

        public async Task<IReadOnlyList<string>> GetSearchGalleryQueryServiceUrlsAsync(ITestOutputHelper logger)
        {
            var v3Index = await GetV3IndexAsync(logger);
            return GetResourceUrls(v3Index, "SearchGalleryQueryService");
        }

        public async Task<IReadOnlyList<string>> GetSearchQueryServiceUrlsAsync(ITestOutputHelper logger)
        {
            var v3Index = await GetV3IndexAsync(logger);
            return GetResourceUrls(v3Index, "SearchQueryService");
        }

        public async Task<IReadOnlyList<string>> GetSearchAutocompleteServiceUrlsAsync(ITestOutputHelper logger)
        {
            var v3Index = await GetV3IndexAsync(logger);
            return GetResourceUrls(v3Index, "SearchAutocompleteService");
        }

        public async Task<IReadOnlyList<string>> GetFlatContainerBaseUrlsAsync(ITestOutputHelper logger)
        {
            var v3Index = await GetV3IndexAsync(logger);
            return GetResourceUrls(v3Index, "PackageBaseAddress");
        }

        public async Task<IReadOnlyList<string>> GetRegistrationBaseUrlsAsync(ITestOutputHelper logger)
        {
            var v3Index = await GetV3IndexAsync(logger);
            return GetResourceUrls(v3Index, "RegistrationsBaseUrl");
        }

        public async Task<IReadOnlyList<string>> GetRegistrationBaseUrlsAsyncForSearch(ITestOutputHelper logger)
        {
            if (_testSettings.SearchRegistrationBaseUrls != null && _testSettings.SearchRegistrationBaseUrls.Any())
            {
                return _testSettings.SearchRegistrationBaseUrls;
            }

            return await GetRegistrationBaseUrlsAsync(logger);
        }

        public async Task<IReadOnlyList<string>> GetSemVer2RegistrationBaseUrlsAsync(ITestOutputHelper logger)
        {
            var v3Index = await GetV3IndexAsync(logger);
            return GetResourceUrls(
                v3Index,
                t => t.Type == "RegistrationsBaseUrl/Versioned" &&
                     t.ClientVersion != null &&
                     NuGetVersion.Parse(t.ClientVersion) >= Version430Alpha);
        }

        public async Task<IReadOnlyList<string>> GetSemVer2RegistrationBaseUrlsAsyncForSearch(ITestOutputHelper logger)
        {
            if (_testSettings.SearchSemVer2RegistrationBaseUrls != null && _testSettings.SearchSemVer2RegistrationBaseUrls.Any())
            {
                return _testSettings.SearchSemVer2RegistrationBaseUrls;
            }

            return await GetSemVer2RegistrationBaseUrlsAsync(logger);
        }

        private static List<string> GetResourceUrls(V3Index v3Index, string resourceType)
        {
            return GetResourceUrls(
                v3Index,
                r => r.Type == resourceType || r.Type.StartsWith($"{resourceType}/"));
        }

        private static List<string> GetResourceUrls(V3Index v3Index, Func<Resource, bool> isMatch)
        {
            return v3Index
                .Resources
                .Where(isMatch)
                .Select(r => r.Id.TrimEnd('/'))
                .Distinct()
                .OrderBy(u => u)
                .ToList();
        }

        private async Task<V3Index> GetV3IndexAsync(ITestOutputHelper logger)
        {
            return await _httpClient.GetJsonAsync<V3Index>(
                _testSettings.V3IndexUrl,
                allowNotFound: false,
                logResponseBody: false,
                logger: logger);
        }

        private class V3Index
        {
            public List<Resource> Resources { get; set; }
        }

        private class Resource
        {
            [JsonProperty("@id")]
            public string Id { get; set; }

            [JsonProperty("@type")]
            public string Type { get; set; }

            [JsonProperty("clientVersion")]
            public string ClientVersion { get; set; }
        }
    }
}
