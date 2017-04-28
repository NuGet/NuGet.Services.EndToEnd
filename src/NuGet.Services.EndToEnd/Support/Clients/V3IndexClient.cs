// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Versioning;

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

        public async Task<IReadOnlyList<string>> GetSearchBaseUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(v3Index, t => t.Type.StartsWith("SearchGalleryQueryService/"));
        }

        public async Task<IReadOnlyList<string>> GetFlatContainerBaseUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(v3Index, t => t.Type.StartsWith("PackageBaseAddress/"));
        }

        public async Task<IReadOnlyList<string>> GetRegistrationBaseUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(v3Index, t => t.Type.StartsWith("RegistrationsBaseUrl/"));
        }

        public async Task<IReadOnlyList<string>> GetSemVer2RegistrationBaseUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(
                v3Index,
                t => t.Type == "RegistrationsBaseUrl/Versioned" &&
                     t.ClientVersion != null &&
                     NuGetVersion.Parse(t.ClientVersion) >= Version430Alpha);
        }

        private static List<string> GetResourceUrls(V3Index v3Index, Func<Resource, bool> isMatch)
        {
            return v3Index
                .Resources
                .Where(r => isMatch(r))
                .Select(r => r.Id.TrimEnd('/'))
                .Distinct()
                .OrderBy(u => u)
                .ToList();
        }

        private async Task<V3Index> GetV3IndexAsync()
        {
            return await _httpClient.GetJsonAsync<V3Index>(_testSettings.V3IndexUrl, logger: null);
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
