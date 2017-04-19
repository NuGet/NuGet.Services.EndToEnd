// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NuGet.Services.EndToEnd.Support
{
    public class V3IndexClient
    {
        private readonly TestSettings _testSettings;
        private readonly SimpleHttpClient _httpClient;

        public V3IndexClient(SimpleHttpClient httpClient, TestSettings testSettings)
        {
            _httpClient = httpClient;
            _testSettings = testSettings;
        }

        public async Task<IReadOnlyList<string>> GetV3SearchUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(v3Index, t => t.StartsWith("SearchQueryService/"));
        }

        public async Task<IReadOnlyList<string>> GetFlatContainerBaseUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(v3Index, t => t.Equals("PackageBaseAddress/3.0.0"));
        }

        public async Task<IReadOnlyList<string>> GetRegistrationBaseUrls()
        {
            var v3Index = await GetV3IndexAsync();
            return GetResourceUrls(v3Index, t => t.StartsWith("RegistrationsBaseUrl"));
        }

        private static List<string> GetResourceUrls(V3Index v3Index, Func<string, bool> isMatch)
        {
            return v3Index
                .Resources
                .Where(r => isMatch(r.Type))
                .Select(r => r.Id.TrimEnd('/'))
                .Distinct()
                .OrderBy(u => u)
                .ToList();
        }

        private async Task<V3Index> GetV3IndexAsync()
        {
            return await _httpClient.GetJsonAsync<V3Index>(_testSettings.V3IndexUrl);
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
        }
    }
}
