// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        /// <returns>Returns a task that completes when the package has the specied listed state or the timeout has occurred.</returns>
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
                response => response
                    .Items
                    .SelectMany(p => p.Items)
                    .Select(i => i.CatalogEntry)
                    .Where(c => c.Id == id && c.Version == version && c.Listed == listed)
                    .Any(),
                startingMessage: $"Waiting for package {id} {version} to be {successState} on registration base URLs:",
                successMessageFormat: $"Package {id} {version} became {successState} on {{0}} after waiting {{1}}.",
                failureMessageFormat: $"Package {id} {version} was still {failureState} on {{0}} after waiting {{1}}.",
                logger: logger);
        }

        /// <summary>
        /// Polls all registration base URLs until the provided ID and version are available.
        /// </summary>
        /// <param name="id">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="semVer2">Whether or not the provided package is SemVer 2.0.0.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Returns a task that completes when the package is available or the timeout has occurred.</returns>
        public async Task WaitForPackageAsync(
            string id,
            string version,
            bool semVer2,
            ITestOutputHelper logger)
        {
            await PollAsync(
                id,
                version,
                semVer2,
                response => response
                    .Items
                    .SelectMany(p => p.Items)
                    .Select(i => i.CatalogEntry)
                    .Where(c => c.Id == id && c.Version == version)
                    .Any(),
                startingMessage: $"Waiting for package {id} {version} to be available on registration base URLs:",
                successMessageFormat: $"Package {id} {version} was found on {{0}} after waiting {{1}}.",
                failureMessageFormat: $"Package {id} {version} was not found on {{0}} after waiting {{1}}.",
                logger: logger);
        }

        private async Task PollAsync(
            string id,
            string version,
            bool semVer2,
            Func<RegistrationIndexResponse, bool> isComplete,
            string startingMessage,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            var baseUrls = await (semVer2 ? _v3IndexClient.GetSemVer2RegistrationBaseUrlsAsync() : _v3IndexClient.GetRegistrationBaseUrlsAsync());

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

            await Task.WhenAll(tasks);
        }

        private async Task PollAsync(
            string baseUrl,
            string id,
            string version,
            Func<RegistrationIndexResponse, bool> isComplete,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            await Task.Yield();

            var url = $"{baseUrl}/{id.ToLowerInvariant()}/index.json";

            var duration = Stopwatch.StartNew();
            var complete = false;
            do
            {
                // Note that this serialization currently does not support reading indexes that are broken up into
                // pages. This is acceptable right now because the test packages never have more than 64 versions, which
                // is the threshold that catalog2registration uses when deciding whether to break the registration index
                // into pages.
                var response = await _httpClient.GetJsonAsync<RegistrationIndexResponse>(url, allowNotFound: true, logger: null);
                if (response != null)
                {
                    complete = isComplete(response);
                }

                if (!complete && duration.Elapsed + TestData.V3SleepDuration < TestData.RegistrationWaitDuration)
                {
                    await Task.Delay(TestData.V3SleepDuration);
                }
            }
            while (!complete && duration.Elapsed < TestData.RegistrationWaitDuration);

            Assert.True(complete, string.Format(failureMessageFormat, url, duration.Elapsed));
            logger.WriteLine(string.Format(successMessageFormat, url, duration.Elapsed));
        }

        private class RegistrationIndexResponse
        {
            public List<RegistrationPage> Items { get; set; }
        }

        private class RegistrationPage
        {
            public List<RegistrationPackage> Items { get; set; }
        }

        private class RegistrationPackage
        {
            public CatalogEntry CatalogEntry { get; set; }
        }

        private class CatalogEntry
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public bool Listed { get; set; }
        }
    }
}
