// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using NuGet.Services.AzureManagement;
using NuGet.Versioning;
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
        private readonly IRetryingAzureManagementAPIWrapper _azureManagementAPIWrapper;

        private Uri _galleryUrl = null;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public GalleryClient(SimpleHttpClient httpClient, TestSettings testSettings, IRetryingAzureManagementAPIWrapper azureManagementAPIWrapper)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _testSettings = testSettings ?? throw new ArgumentNullException(nameof(testSettings));
            _azureManagementAPIWrapper = azureManagementAPIWrapper;
        }

        public async Task<Uri> GetGalleryUrlAsync(ITestOutputHelper logger)
        {
            if (_galleryUrl != null)
            {
                return _galleryUrl;
            }

            try
            {
                await _semaphore.WaitAsync();

                if (_galleryUrl != null)
                {
                    return _galleryUrl;
                }

                if (_azureManagementAPIWrapper == null || _testSettings.GalleryConfiguration.ServiceDetails == null)
                {
                    _galleryUrl = new Uri(_testSettings.GalleryConfiguration.GalleryBaseUrl);
                    logger.WriteLine($"Configured gallery mode: use hardcoded URL {_galleryUrl}");
                }
                else
                {
                    var serviceDetails = _testSettings.GalleryConfiguration.ServiceDetails;

                    logger.WriteLine($"Configured gallery mode: get service properties from Azure. " +
                       $"Subscription: {serviceDetails.Subscription}, " +
                       $"Resource group: {serviceDetails.ResourceGroup}, " +
                       $"Service name: {serviceDetails.Name}");

                    string result = await _azureManagementAPIWrapper.GetCloudServicePropertiesAsync(
                                    serviceDetails.Subscription,
                                    serviceDetails.ResourceGroup,
                                    serviceDetails.Name,
                                    serviceDetails.Slot,
                                    logger,
                                    CancellationToken.None);

                    var cloudService = AzureHelper.ParseCloudServiceProperties(result);

                    _galleryUrl = ClientHelper.ConvertToHttpsAndClean(cloudService.Uri);

                    logger.WriteLine($"Gallery URL to use: {_galleryUrl}");
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return _galleryUrl;
        }

        public Uri GetGalleryBaseUrl(ITestOutputHelper logger)
        {
            return new Uri(_testSettings.GalleryConfiguration.GalleryBaseUrl);
        }

        public async Task<IList<string>> AutocompletePackageIdsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger)
        {
            var galleryEndpoint = await GetGalleryUrlAsync(logger);
            var serviceEndpoint = $"{galleryEndpoint}/api/v2/package-ids";
            var uri = AppendAutocompletePackageIdsQueryString(serviceEndpoint, id, includePrerelease, semVerLevel);

            return await _httpClient.GetJsonAsync<List<string>>(uri, logger);
        }

        public async Task<IList<string>> AutocompletePackageVersionsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger)
        {
            var galleryEndpoint = await GetGalleryUrlAsync(logger);
            var serviceEndpoint = $"{galleryEndpoint}/api/v2/package-versions";
            var uri = AppendAutocompletePackageVersionsQueryString($"{serviceEndpoint}/{id}", includePrerelease, semVerLevel);

            return await _httpClient.GetJsonAsync<List<string>>(uri, logger);
        }

        public async Task PushAsync(Stream nupkgStream, ITestOutputHelper logger, PackageType packageType)
        {
            var galleryEndpoint = await GetGalleryUrlAsync(logger);

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
            var galleryEndpoint = await GetGalleryUrlAsync(logger);
            var url = $"{galleryEndpoint}/api/v2/package/{id}/{version}";
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage(method, url))
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
