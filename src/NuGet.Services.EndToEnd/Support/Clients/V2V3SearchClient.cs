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
    public class V2V3SearchClient
    {
        private readonly V3IndexClient _v3IndexClient;
        private readonly SimpleHttpClient _httpClient;
        private readonly TestSettings _testSettings;

        public V2V3SearchClient(SimpleHttpClient httpClient, V3IndexClient v3IndexClient, TestSettings testSettings)
        {
            _httpClient = httpClient;
            _v3IndexClient = v3IndexClient;
            _testSettings = testSettings;
        }

        public async Task<V2SearchResponse> SearchQueryAsync(string searchBaseUrl, string queryString)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryUrl = new Uri(baseUri, $"search/query?{queryString}");
            return await _httpClient.GetJsonAsync<V2SearchResponse>(queryUrl.AbsoluteUri);
        }

        public async Task<V3SearchResponse> QueryAsync(string searchBaseUrl, string queryString)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryUrl = new Uri(baseUri, $"query?{queryString}");
            return await _httpClient.GetJsonAsync<V3SearchResponse>(queryUrl.AbsoluteUri);
        }

        public async Task<AutocompleteResponse> AutocompleteAsync(string searchBaseUrl, string queryString)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryUrl = new Uri(baseUri, $"autocomplete?{queryString}");
            return await _httpClient.GetJsonAsync<AutocompleteResponse>(queryUrl.AbsoluteUri);
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
        
        public async Task<IReadOnlyList<string>> GetSearchBaseUrlsAsync()
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

            return searchBaseUrls;
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
                var response = await _httpClient.GetJsonAsync<V2SearchResponse>(url);
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

        public class V2SearchResponse
        {
            public List<V2SearchPackage> Data { get; set; }
            public string Index { get; set; }
            public long TotalHits { get; set; }
        }

        public class V2SearchPackage
        {
            public V2SearchPackageRegistration PackageRegistration { get; set; }
            public string Version { get; set; }
            public string NormalizedVersion { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Summary { get; set; }
            public string Authors { get; set; }
            public string Copyright { get; set; }
            public string Tags { get; set; }
            public string ReleaseNotes { get; set; }
            public bool IsLatestStable { get; set; }
            public bool IsLatest { get; set; }
            public bool Listed { get; set; }
            public DateTimeOffset Created { get; set; }
            public DateTimeOffset Published { get; set; }
            public DateTimeOffset LastUpdated { get; set; }
            public DateTimeOffset LastEdited { get; set; }
            public long DownloadCount { get; set; }
            public string FlattenedDependencies { get; set; }
            public List<V2Dependency> Dependencies { get; set; }
            public string[] SupportedFrameworks { get; set; }
            public string Hash { get; set; }
            public string HashAlgorithm { get; set; }
            public long PackageFileSize { get; set; }
            public bool RequiresLicenseAcceptance { get; set; }
        }

        public class V2Dependency
        {
            public string Id { get; set; }
            public string VersionSpec { get; set; }
            public string TargetFramework { get; set; }
        }

        public class V2SearchPackageRegistration
        {
            public string Id { get; set; }
            public long DownloadCount { get; set; }
            public string[] Owners { get; set; }
        }
        
        public class V3SearchResponse
        {
            public List<V3SearchPackage> Data { get; set; }
            public string Index { get; set; }
            public long TotalHits { get; set; }
            public DateTimeOffset LastReopen { get; set; }
        }

        public class V3SearchPackage
        {
            public string Registration { get; set; }
            public string Id { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public string Summary { get; set; }
            public string Title { get; set; }
            public string[] Tags { get; set; }
            public string[] Authors { get; set; }
            public long TotalDownloads { get; set; }
            public List<V3VersionEntry> Versions { get; set; }
        }

        public class V3VersionEntry
        {
            public string Version { get; set; }
            public long Downloads { get; set; }
        }

        public class AutocompleteResponse
        {
            public List<string> Data { get; set; }
            public string Index { get; set; }
            public long TotalHits { get; set; }
            public DateTimeOffset LastReopen { get; set; }

        }
    }
}
