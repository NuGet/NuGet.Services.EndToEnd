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

        /// <summary>
        /// Polls all V2 search URLs until the provided ID and version are available. If <see cref="TestSettings.SearchBaseUrl"/>,
        /// is configured, then only that search service is polled. We poll the V2 search URL because it allows us to
        /// easily ignore all filtering rules and query for a single version.
        /// </summary>
        /// <param name="id">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Returns a task that completes when the package is available or the timeout has occurred.</returns>
        public async Task WaitForPackageAsync(string id, string version, ITestOutputHelper logger)
        {
            var searchBaseUrls = new List<string>();
            if (_testSettings.SearchBaseUrl != null)
            {
                searchBaseUrls.Add(_testSettings.SearchBaseUrl);
            }
            else
            {
                searchBaseUrls.AddRange(await _v3IndexClient.GetSearchBaseUrls());
            }

            var v2SearchUrls = searchBaseUrls
                .Select(u => $"{u}/search/query")
                .ToList();

            Assert.True(v2SearchUrls.Count > 0, "At least one search base URL must be configured.");

            logger.WriteLine(
                $"Waiting for package {id} {version} to be available on search endpoints:" + Environment.NewLine +
                string.Join(Environment.NewLine, v2SearchUrls.Select(u => $" - {u}")));

            var tasks = v2SearchUrls
                .Select(u => WaitForPackageAsync(u, id, version, logger))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task WaitForPackageAsync(string v2SearchUrl, string id, string version, ITestOutputHelper logger)
        {
            await Task.Yield();

            var url = QueryHelpers.AddQueryString(v2SearchUrl, "q", $"packageid:{id} and version:{version}");
            url = QueryHelpers.AddQueryString(url, "ignoreFilter", "true");

            var duration = Stopwatch.StartNew();
            var found = false;
            do
            {
                var response = await _httpClient.GetJsonAsync<SearchResponse>(url);
                found = response
                    .Data
                    .Where(d => d.PackageRegistration?.Id == id && d.Version == version)
                    .Any();

                if (!found && duration.Elapsed + TestData.V3SleepDuration < TestData.V3WaitDuration)
                {
                    await Task.Delay(TestData.V3SleepDuration);
                }
            }
            while (!found && duration.Elapsed < TestData.V3WaitDuration);

            Assert.True(found, $"Package {id} {version} was not found on {v2SearchUrl} after waiting {TestData.V3WaitDuration}.");
            logger.WriteLine($"Package {id} {version} was found on {v2SearchUrl} after waiting {duration.Elapsed}.");
        }

        private class SearchResponse
        {
            public List<SearchPackage> Data { get; set; }
        }

        public class SearchPackage
        {
            public SearchPackageRegistration PackageRegistration { get; set; }
            public string Version { get; set; }
        }

        public class SearchPackageRegistration
        {
            public string Id { get; set; }
        }
    }
}
