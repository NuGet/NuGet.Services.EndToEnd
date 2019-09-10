// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A simple interface for interacting with the registration hives.
    /// </summary>
    public class RegistrationClient
    {
        private readonly V3IndexClient _v3IndexClient;
        private readonly SimpleHttpClient _httpClient;

        public RegistrationClient(SimpleHttpClient httpClient, V3IndexClient v3IndexClient)
        {
            _httpClient = httpClient;
            _v3IndexClient = v3IndexClient;
        }

        /// <summary>
        /// Polls all registration base URLs until the provided ID and version has the specified listed state.
        /// </summary>
        /// <param name="id">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="listed">The listed state to poll for.</param>
        /// <param name="semVer2">Whether or not the provided package is SemVer 2.0.0.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Returns a task that completes when the package has the specified listed state or the timeout has occurred.</returns>
        public async Task WaitForListedStateAsync(
            string id,
            string version,
            bool semVer2,
            bool listed,
            ITestOutputHelper logger)
        {
            var successState = listed ? "listed" : "unlisted";
            var failureState = listed ? "unlisted" : "listed";

            await PollAsync(
                id,
                version,
                semVer2,
                catalogEntry => catalogEntry.Id == id && catalogEntry.Version == version && catalogEntry.Listed == listed,
                startingMessage: $"Waiting for package {id} {version} to be {successState} on registration base URLs:",
                successMessageFormat: $"Package {id} {version} became {successState} on {{0}} after waiting {{1}}.",
                failureMessageFormat: $"Package {id} {version} was still {failureState} on {{0}} after waiting {{1}}.",
                logger: logger);
        }

        /// <summary>
        /// Polls all registration base URLs until the provided ID and version has the specified deprecation state.
        /// </summary>
        /// <param name="id">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="isDeprecated">The deprecation state to poll for.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Returns a task that completes when the package has the specified deprecation state or the timeout has occurred.</returns>
        public async Task<CatalogDeprecation> WaitForDeprecationStateAsync(
            string id,
            string version,
            bool isDeprecated,
            ITestOutputHelper logger)
        {
            var successState = isDeprecated ? "deprecated" : "undeprecated";
            var failureState = isDeprecated ? "undeprecated" : "deprecated";

            var package = await PollAsync(
                id,
                version,
                semVer2: true,
                isComplete: catalogEntry => 
                    catalogEntry.Id == id 
                    && catalogEntry.Version == version
                    && (catalogEntry.Deprecation != null) == isDeprecated,
                startingMessage: $"Waiting for package {id} {version} to be {successState} on registration base URLs:",
                successMessageFormat: $"Package {id} {version} became {successState} on {{0}} after waiting {{1}}.",
                failureMessageFormat: $"Package {id} {version} was still {failureState} on {{0}} after waiting {{1}}.",
                logger: logger);

            return package.Last().CatalogEntry.Deprecation;
        }

        /// <summary>
        /// Polls all registration base URLs until the provided ID and version are available.
        /// </summary>
        /// <param name="id">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="semVer2">Whether or not the provided package is SemVer 2.0.0.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Returns a task that completes when the package is available or the timeout has occurred.</returns>
        public async Task<IReadOnlyList<RegistrationPackage>> WaitForPackageAsync(
            string id,
            string version,
            bool semVer2,
            ITestOutputHelper logger)
        {
            return await PollAsync(
                id,
                version,
                semVer2,
                catalogEntry => catalogEntry.Id == id && catalogEntry.Version == version,
                startingMessage: $"Waiting for package {id} {version} to be available on registration base URLs:",
                successMessageFormat: $"Package {id} {version} was found on {{0}} after waiting {{1}}.",
                failureMessageFormat: $"Package {id} {version} was not found on {{0}} after waiting {{1}}.",
                logger: logger);
        }

        private Task<IReadOnlyList<RegistrationPackage>> PollAsync(
            string id,
            string version,
            bool semVer2,
            Func<CatalogEntry, bool> isComplete,
            string startingMessage,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            // Retry for connection problem, timeout, or HTTP 5XX. We are okay with retrying on 5XX in this case because
            // the origin server is blob storage. If they are having some internal server error, there's not much we can
            // do.
            return RetryUtility.ExecuteWithRetry<IReadOnlyList<RegistrationPackage>>(
                async () =>
                {
                    var baseUrls = await (semVer2 ? _v3IndexClient.GetSemVer2RegistrationBaseUrlsAsync(logger) : _v3IndexClient.GetRegistrationBaseUrlsAsync(logger));

                    Assert.True(baseUrls.Count > 0, "At least one registration base URL must be configured.");

                    logger.WriteLine(
                        startingMessage +
                        Environment.NewLine +
                        string.Join(Environment.NewLine, baseUrls.Select(u => $" - {u}")));

                    var tasks = baseUrls
                        .Select(baseUrl => PollAsync(
                            baseUrl,
                            id,
                            version,
                            isComplete,
                            successMessageFormat,
                            failureMessageFormat,
                            logger))
                        .ToList();

                    return await Task.WhenAll(tasks);
                },
                ex => ex.HasTypeOrInnerType<SocketException>()
                   || ex.HasTypeOrInnerType<TaskCanceledException>()
                   || (ex.HasTypeOrInnerType<HttpRequestMessageException>(out var hre)
                       && (hre.StatusCode == HttpStatusCode.InternalServerError
                           || hre.StatusCode == HttpStatusCode.BadGateway
                           || hre.StatusCode == HttpStatusCode.ServiceUnavailable
                           || hre.StatusCode == HttpStatusCode.GatewayTimeout)),
                logger: logger);
        }

        private async Task<RegistrationPackage> PollAsync(
            string baseUrl,
            string id,
            string version,
            Func<CatalogEntry, bool> isComplete,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            await Task.Yield();

            var url = $"{baseUrl}/{id.ToLowerInvariant()}/index.json";

            var nugetVersion = NuGetVersion.Parse(version);
            var duration = Stopwatch.StartNew();
            var complete = false;

            RegistrationPackage package = null;
            do
            {
                var root = await _httpClient.GetJsonAsync<RegistrationRoot>(
                    url,
                    allowNotFound: true,
                    logResponseBody: false,
                    logger: logger);

                if (root != null)
                {
                    var page = root.Items
                        .SingleOrDefault(item => item.Lower <= nugetVersion && nugetVersion <= item.Upper);

                    if (page != null)
                    {
                        if (!page.Items.Any())
                        {
                            page = await _httpClient.GetJsonAsync<RegistrationPage>(
                                page.Id.AbsoluteUri,
                                allowNotFound: true,
                                logResponseBody: false,
                                logger: logger);
                        }

                        if (page != null)
                        {
                            package = page.Items
                                .SingleOrDefault(item => item.CatalogEntry.Id == id && item.CatalogEntry.Version == version);

                            if (package != null)
                            {
                                complete = isComplete(package.CatalogEntry);
                            }
                        }
                    }
                }

                if (!complete && duration.Elapsed + TestData.V3SleepDuration < TestData.RegistrationWaitDuration)
                {
                    await Task.Delay(TestData.V3SleepDuration);
                }
            }
            while (!complete && duration.Elapsed < TestData.RegistrationWaitDuration);

            Assert.True(complete, string.Format(failureMessageFormat, url, duration.Elapsed));
            logger.WriteLine(string.Format(successMessageFormat, url, duration.Elapsed));

            return package;
        }

        private class RegistrationRoot
        {
            public List<RegistrationPage> Items { get; set; }

            public RegistrationRoot()
            {
                Items = new List<RegistrationPage>();
            }
        }

        private class RegistrationPage
        {
            [JsonProperty("@id")]
            public Uri Id { get; set; }
            public List<RegistrationPackage> Items { get; set; }
            public NuGetVersion Lower { get; set; }
            public NuGetVersion Upper { get; set; }

            public RegistrationPage()
            {
                Items = new List<RegistrationPackage>();
            }
        }

        public class RegistrationPackage
        {
            public CatalogEntry CatalogEntry { get; set; }
        }

        public class CatalogEntry
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public bool Listed { get; set; }
            public string LicenseExpression { get; set; }
            public string LicenseUrl { get; set; }
            public string IconUrl { get; set; }
            public CatalogDeprecation Deprecation { get; set; }
        }

        public class CatalogDeprecation
        {
            public string Message { get; set; }
            public IReadOnlyCollection<string> Reasons { get; set; }
            public CatalogAlternatePackage AlternatePackage { get; set; }
        }

        public class CatalogAlternatePackage
        {
            public string Id { get; set; }
            public string Range { get; set; }
        }
    }
}
