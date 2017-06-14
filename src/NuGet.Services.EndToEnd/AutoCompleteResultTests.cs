// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Services.EndToEnd.Support;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    public class AutoCompleteResultTests
        : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly TestSettings _testSettings;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public AutoCompleteResultTests(ITestOutputHelper logger)
        {
            _clients = Clients.Initialize();
            _testSettings = TestSettings.Create();
            _logger = logger;
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, null)]
        [InlineData(true, "2.0.0")]
        [InlineData(false, "2.0.0")]
        public async Task V2PackageVersionsAutocompleteResultsMatchV3Results(
            bool includePrerelease,
            string semVerLevel)
        {
            // Arrange
            const string id = "EntityFramework";

            var v2AutoCompletePackageVersionsEndpoint = $"{_testSettings.GalleryBaseUrl}/api/v2/package-versions";
            var v2AutoCompletePackageVersionsQueryString = BuildV2QueryString(id, includePrerelease, semVerLevel);
            var v2Url = v2AutoCompletePackageVersionsEndpoint + v2AutoCompletePackageVersionsQueryString;

            var v3AutoCompletePackageVersionsEndpoints = await _clients.V2V3Search.GetAutoCompleteUrlsAsync();
            var v3AutoCompletePackageVersionsQueryString = BuildV3QueryString($"id={id}", includePrerelease, semVerLevel);

            // Act
            var httpClient = new SimpleHttpClient();
            var v2Response = await httpClient.GetJsonAsync<List<string>>(v2Url, _logger);

            foreach (var v3AutoCompletePackageVersionsEndpoint in v3AutoCompletePackageVersionsEndpoints)
            {
                var v3Url = v3AutoCompletePackageVersionsEndpoint + v3AutoCompletePackageVersionsQueryString;
                var v3Response = await httpClient.GetJsonAsync<List<string>>(v2Url, _logger);

                // Assert
                Assert.Equal(v2Response.Count, v3Response.Count);

                for (int i = 0; i < v2Response.Count; i++)
                {
                    Assert.Equal(v2Response[i], v3Response[i]);
                }
            }
        }

        private static string BuildV2QueryString(string id, bool? includePrerelease, string semVerLevel = null)
        {
            var queryString = $"includePrerelease={includePrerelease ?? false}";

            NuGetVersion semVerLevelVersion;
            if (!string.IsNullOrEmpty(semVerLevel) && NuGetVersion.TryParse(semVerLevel, out semVerLevelVersion))
            {
                queryString += $"&semVerLevel={semVerLevel}";
            }

            return $"/{id}?" + queryString;
        }

        private static string BuildV3QueryString(string queryString, bool? includePrerelease, string semVerLevel = null)
        {
            queryString += $"&prerelease={includePrerelease ?? false}";

            NuGetVersion semVerLevelVersion;
            if (!string.IsNullOrEmpty(semVerLevel) && NuGetVersion.TryParse(semVerLevel, out semVerLevelVersion))
            {
                queryString += $"&semVerLevel={semVerLevel}";
            }

            if (string.IsNullOrEmpty(queryString))
            {
                return string.Empty;
            }

            return "?" + queryString.TrimStart('&');
        }
    }
}
