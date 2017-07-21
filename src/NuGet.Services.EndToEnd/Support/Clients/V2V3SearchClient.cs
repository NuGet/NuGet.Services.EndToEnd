﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using NuGet.Services.AzureManagement;
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
        private readonly IAzureManagementAPIWrapper _azureManagementAPIWrapper; 

        public V2V3SearchClient(SimpleHttpClient httpClient, V3IndexClient v3IndexClient, TestSettings testSettings, IAzureManagementAPIWrapper azureManagementAPIWrapper)
        {
            _httpClient = httpClient;
            _v3IndexClient = v3IndexClient;
            _testSettings = testSettings;
            _azureManagementAPIWrapper = azureManagementAPIWrapper;
        }

        [Obsolete]
        public async Task<V2SearchResponse> SearchQueryAsync(string searchBaseUrl, string queryString, ITestOutputHelper logger)
        {
            var baseUri = new Uri(searchBaseUrl);
            var queryUrl = new Uri(baseUri, $"search/query?{queryString}");
            return await _httpClient.GetJsonAsync<V2SearchResponse>(queryUrl.AbsoluteUri, logger);
        }

        public async Task<V3SearchResponse> QueryAsync(SearchService searchService, string queryString, ITestOutputHelper logger)
        {
            var queryUrl = new Uri(searchService.Uri, $"query?{queryString}");
            return await _httpClient.GetJsonAsync<V3SearchResponse>(queryUrl.AbsoluteUri, logger);
        }

        public async Task<AutocompleteResponse> AutocompletePackageIdsAsync(
            SearchService searchService,
            string packageId,
            bool includePrerelease,
            string semVerLevel,
            ITestOutputHelper logger)
        {
            var queryString = BuildAutocompleteQueryString($"take=30&q={packageId}", includePrerelease, semVerLevel);
            var queryUrl = new Uri(searchService.Uri, queryString);
            return await _httpClient.GetJsonAsync<AutocompleteResponse>(queryUrl.AbsoluteUri, logger);
        }

        public async Task<AutocompleteResponse> AutocompletePackageVersionsAsync(
            SearchService searchService,
            string packageId,
            bool includePrerelease,
            string semVerLevel,
            ITestOutputHelper logger)
        {
            var queryString = BuildAutocompleteQueryString($"id={packageId}", includePrerelease, semVerLevel);
            var queryUrl = new Uri(searchService.Uri, queryString);
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

        private IEnumerable<string> GetSearchUrlsForPolling(SearchService searchServices)
        {
            for (var instanceIndex = 0; instanceIndex < searchServices.InstanceCount; instanceIndex++)
            {
                var port = 8080 + instanceIndex;
                var uriBuilder = new UriBuilder(searchServices.Uri)
                {
                    Scheme = "http",
                    Port = port,
                    Path = "/search/query"
                };

                yield return uriBuilder.Uri.ToString();
            }
        }

        public async Task<IReadOnlyList<SearchService>> GetSearchServicesAsync(ITestOutputHelper logger)
        {
            if (_azureManagementAPIWrapper != null)
            {
                logger.WriteLine($"Extracting search service properties from Azure. " +
                    $"Subscription: {_testSettings.Subscription}," +
                    $" Resource group: {_testSettings.SearchServiceResourceGroup}," +
                    $" Service name: {_testSettings.SearchServiceName}");

                string result = await _azureManagementAPIWrapper.GetCloudServicePropertiesAsync(
                                    _testSettings.Subscription,
                                    _testSettings.SearchServiceResourceGroup,
                                    _testSettings.SearchServiceName,
                                    _testSettings.SearchServiceSlot,
                                    CancellationToken.None);

                var cloudService = AzureHelper.ParseCloudServiceProperties(result);

                return new List<SearchService>() { new SearchService { Uri = cloudService.Uri, InstanceCount = cloudService.InstanceCount } };
            }
            else
            {
                logger.WriteLine($"Extracting search services URLs from index.json: {_testSettings.V3IndexUrl}");

                var urls = await _v3IndexClient.GetSearchBaseUrlsAsync();
                return urls.Select(url => new SearchService { Uri = new Uri(url), InstanceCount = _testSettings.SearchInstanceCount }).ToList();
            }
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
            var searchServices = await GetSearchServicesAsync(logger);

            var v2SearchUrls = searchServices
                .SelectMany(service => GetSearchUrlsForPolling(service))
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

        public class SearchService
        {
            public Uri Uri { get; set; }
            public int InstanceCount { get; set; }
        }
    }
}
