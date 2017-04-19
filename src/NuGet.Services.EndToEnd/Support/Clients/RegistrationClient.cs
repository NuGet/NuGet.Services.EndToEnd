﻿// Copyright (c) .NET Foundation. All rights reserved.
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
    public class RegistrationClient
    {
        private readonly V3IndexClient _v3IndexClient;
        private readonly SimpleHttpClient _httpClient;

        public RegistrationClient(SimpleHttpClient httpClient, V3IndexClient v3IndexClient)
        {
            _httpClient = httpClient;
            _v3IndexClient = v3IndexClient;
        }

        public async Task WaitForPackageAsync(string id, string version, ITestOutputHelper logger)
        {
            var baseUrls = await _v3IndexClient.GetRegistrationBaseUrls();

            Assert.True(baseUrls.Count > 0, "At least one registration base URL must be configured.");

            logger.WriteLine(
                $"Waiting for package {id} {version} to be available on registration base URLs:" + Environment.NewLine +
                string.Join(Environment.NewLine, baseUrls.Select(u => $" - {u}")));

            var tasks = baseUrls
                .Select(u => WaitForPackageAsync(u, id, version, logger))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task WaitForPackageAsync(string baseUrl, string id, string version, ITestOutputHelper logger)
        {
            await Task.Yield();

            var url = $"{baseUrl}/{id.ToLowerInvariant()}/index.json";

            var duration = Stopwatch.StartNew();
            var found = false;
            do
            {
                // Note that this serialization currently does not support reading indexes that are broken up into
                // pages. This is acceptable right now because the test packages never have more than 64 versions, which
                // is the threshold that catalog2registration uses when deciding whether to break the registration index
                // into pages.
                var response = await _httpClient.GetJsonAsync<RegistrationIndexResponse>(url, allowNotFound: true);
                if (response != null)
                {
                    found = response
                        .Items
                        .SelectMany(p => p.Items)
                        .Select(i => i.CatalogEntry)
                        .Where(c => c.Id == id && c.Version == version)
                        .Any();
                }

                if (!found && duration.Elapsed + TestData.V3SleepDuration < TestData.V3WaitDuration)
                {
                    await Task.Delay(TestData.V3SleepDuration);
                }
            }
            while (!found && duration.Elapsed < TestData.V3WaitDuration);

            Assert.True(found, $"Package {id} {version} was not found on {baseUrl} after waiting {TestData.V3WaitDuration}.");
            logger.WriteLine($"Package {id} {version} was found on {baseUrl} after waiting {duration.Elapsed}.");
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
        }
    }
}
