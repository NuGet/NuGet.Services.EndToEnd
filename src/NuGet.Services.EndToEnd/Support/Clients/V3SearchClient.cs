// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class V3SearchClient
    {
        private readonly V3IndexClient _v3IndexClient;
        private readonly SimpleHttpClient _httpClient;
        private readonly TestSettings _testSettings;

        public V3SearchClient(SimpleHttpClient httpClient, V3IndexClient v3IndexClient, TestSettings testSettings)
        {
            _httpClient = httpClient;
            _v3IndexClient = v3IndexClient;
            _testSettings = testSettings;
        }

        public async Task WaitForPackageAsync(string id, string version, ITestOutputHelper logger)
        {
            var v3SearchUrls = new List<string>();
            if (_testSettings.SearchBaseUrl != null)
            {
                v3SearchUrls.Add($"{_testSettings.SearchBaseUrl}/query");
            }
            else
            {
                v3SearchUrls.AddRange(await _v3IndexClient.GetV3SearchUrls());
            }

            Assert.True(v3SearchUrls.Count > 0, "At least one search base URL must be configured.");

            logger.WriteLine(
                $"Waiting for package {id} {version} to be available on search endpoints:" + Environment.NewLine +
                string.Join(Environment.NewLine, v3SearchUrls.Select(u => $" - {u}")));

            var tasks = v3SearchUrls
                .Select(u => WaitForPackageAsync(u, id, version, logger))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task WaitForPackageAsync(string v3SearchUrl, string id, string version, ITestOutputHelper logger)
        {
            await Task.Yield();

            var url = QueryHelpers.AddQueryString(v3SearchUrl, "q", $"packageid:{id}");
            url = QueryHelpers.AddQueryString(url, "prerelease", "true");

            var duration = Stopwatch.StartNew();
            var found = false;
            do
            {
                var response = await _httpClient.GetJsonAsync<SearchResponse>(url);
                found = response
                    .Data
                    .Where(d => d.Id == id)
                    .SelectMany(d => d.Versions)
                    .Where(v => v.Version == version)
                    .Any();

                if (!found && duration.Elapsed + TestData.V3SleepDuration < TestData.V3WaitDuration)
                {
                    await Task.Delay(TestData.V3SleepDuration);
                }
            }
            while (!found && duration.Elapsed < TestData.V3WaitDuration);

            Assert.True(found, $"Package {id} {version} was not found on {v3SearchUrl} after waiting {TestData.V3WaitDuration}.");
            logger.WriteLine($"Package {id} {version} was found on {v3SearchUrl} after waiting {duration.Elapsed}.");
        }

        private class SearchResponse
        {
            public List<SearchPackage> Data { get; set; }
        }

        public class SearchPackage
        {
            public string Id { get; set; }
            public List<SearchVersion> Versions { get; set; }
        }

        public class SearchVersion
        {
            public string Version { get; set; }
        }
    }
}
