// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using NuGet.Versioning;
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

        [Obsolete]
        public async Task<V2SearchResponse> SearchQueryAsync(string searchBaseUrl, string queryString, ITestOutputHelper logger)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryUrl = new Uri(baseUri, $"search/query?{queryString}");
            return await _httpClient.GetJsonAsync<V2SearchResponse>(queryUrl.AbsoluteUri, logger);
        }

        public async Task<V3SearchResponse> QueryAsync(string searchBaseUrl, string queryString, ITestOutputHelper logger)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryUrl = new Uri(baseUri, $"query?{queryString}");
            return await _httpClient.GetJsonAsync<V3SearchResponse>(queryUrl.AbsoluteUri, logger);
        }

        public async Task<AutocompleteResponse> AutocompletePackageIdsAsync(
            string searchBaseUrl,
            string packageId,
            bool includePrerelease,
            string semVerLevel,
            ITestOutputHelper logger)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryString = BuildAutocompleteQueryString($"take=30&q={packageId}", includePrerelease, semVerLevel);
            var queryUrl = new Uri(baseUri, queryString);
            return await _httpClient.GetJsonAsync<AutocompleteResponse>(queryUrl.AbsoluteUri, logger);
        }

        public async Task<AutocompleteResponse> AutocompletePackageVersionsAsync(
            string searchBaseUrl,
            string packageId,
            bool includePrerelease,
            string semVerLevel,
            ITestOutputHelper logger)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryString = BuildAutocompleteQueryString($"id={packageId}", includePrerelease, semVerLevel);
            var queryUrl = new Uri(baseUri, queryString);
            return await _httpClient.GetJsonAsync<AutocompleteResponse>(queryUrl.AbsoluteUri, logger);
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
            await PollAsync(
                id,
                version,
                response => response
                    .Data
                    .Where(d => d.PackageRegistration?.Id == id && d.Version == version)
                    .Any(),
                $"Waiting for package {id} {version} to be available on search endpoints:",
                $"Package {id} {version} was found on {{0}} after waiting {{1}}.",
                $"Package {id} {version} was not found on {{0}} after waiting {{1}}.",
                logger);
        }

        /// <summary>
        /// Polls all V2 search URLs until the package has the specified listed state.
        /// </summary>
        /// <param name="id">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="listed">The listed state to wait for.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Returns a task that completes when the package is available or the timeout has occurred.</returns>
        public async Task WaitForListedStateAsync(string id, string version, bool listed, ITestOutputHelper logger)
        {
            var successState = listed ? "listed" : "unlisted";
            var failureState = listed ? "unlisted" : "listed";

            await PollAsync(
                id,
                version,
                response => response
                    .Data
                    .Where(d => d.PackageRegistration?.Id == id && d.Version == version && d.Listed == listed)
                    .Any(),
                $"Waiting for package {id} {version} to be {successState} on search endpoints:",
                $"Package {id} {version} became {successState} on {{0}} after waiting {{1}}.",
                $"Package {id} {version} was still {failureState} on {{0}} after waiting {{1}}.",
                logger);
        }

        private IEnumerable<string> GetSearchUrlsForPolling(string originalBaseUrl)
        {
            for (var instanceIndex = 0; instanceIndex < _testSettings.SearchInstanceCount; instanceIndex++)
            {
                var port = 8080 + instanceIndex;
                var uriBuilder = new UriBuilder(originalBaseUrl)
                {
                    Scheme = "http",
                    Port = port,
                    Path = "/search/query"
                };

                yield return uriBuilder.Uri.ToString();
            }
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
                searchBaseUrls.AddRange(await _v3IndexClient.GetSearchBaseUrlsAsync());
            }

            return searchBaseUrls;
        }

        private static string BuildAutocompleteQueryString(
            string query,
            bool? includePrerelease,
            string semVerLevel = null)
        {
            query += $"&prerelease={includePrerelease ?? false}";

            NuGetVersion semVerLevelVersion;
            if (!string.IsNullOrEmpty(semVerLevel) && NuGetVersion.TryParse(semVerLevel, out semVerLevelVersion))
            {
                query += $"&semVerLevel={semVerLevel}";
            }

            if (string.IsNullOrEmpty(query))
            {
                return string.Empty;
            }

            return "autocomplete?" + query.TrimStart('&');
        }

        private async Task PollAsync(
            string id,
            string version,
            Func<V2SearchResponse, bool> isComplete,
            string startingMessage,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            var searchBaseUrls = await GetSearchBaseUrlsAsync();

            var v2SearchUrls = searchBaseUrls
                .SelectMany(u => GetSearchUrlsForPolling(u))
                .ToList();

            Assert.True(v2SearchUrls.Count > 0, "At least one search base URL must be configured.");

            logger.WriteLine(
                startingMessage +
                Environment.NewLine +
                string.Join(Environment.NewLine, v2SearchUrls.Select(u => $" - {u}")));

            var tasks = v2SearchUrls
                .Select(u => PollAsync(
                    u,
                    id,
                    version,
                    isComplete,
                    successMessageFormat,
                    failureMessageFormat,
                    logger))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task PollAsync(
            string v2SearchUrl,
            string id,
            string version,
            Func<V2SearchResponse, bool> isComplete,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            await Task.Yield();

            var url = QueryHelpers.AddQueryString(v2SearchUrl, "q", $"packageid:{id} and version:{version}");
            url = QueryHelpers.AddQueryString(url, "ignoreFilter", "true");
            url = QueryHelpers.AddQueryString(url, "semVerLevel", "2.0.0");

            var duration = Stopwatch.StartNew();
            var complete = false;
            do
            {
                var response = await _httpClient.GetJsonAsync<V2SearchResponse>(url, logger: null);
                complete = isComplete(response);

                if (!complete && duration.Elapsed + TestData.V3SleepDuration < TestData.SearchWaitDuration)
                {
                    await Task.Delay(TestData.V3SleepDuration);
                }
            }
            while (!complete && duration.Elapsed < TestData.SearchWaitDuration);

            Assert.True(complete, string.Format(failureMessageFormat, v2SearchUrl, duration.Elapsed));
            logger.WriteLine(string.Format(successMessageFormat, v2SearchUrl, duration.Elapsed));
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
