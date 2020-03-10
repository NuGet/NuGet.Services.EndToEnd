// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NuGet.Services.Configuration;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettings
    {
        /// <summary>
        /// Change this value to test against one of the pre-configured environments: Dev/Int/Prod.
        /// When there is no setting, the configuration will be extracted from an environment variable 
        /// <see cref="EnvironmentSettings.ConfigurationName"/>
        /// </summary>
        public static string ManualConfigurationOverride = "";

        /// <summary>
        /// Manually override this value to easily enable aggressive pushing. This means each test will push its own
        /// packages. This is helpful when debugging a single test.
        /// </summary>
        public static bool AggressivePush => true;

        private static TestSettings _testSettings = null;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private TestSettings()
        {
        }

        public string V3IndexUrl { get; set; }

        public SearchServiceConfiguration SearchServiceConfiguration { get; set; }

        public GalleryConfiguration GalleryConfiguration { get; set; }

        public string TestAccountOwner { get; set; }

        public string ApiKey { get; set; }

        public string SymbolServerUrlTemplate { get; set; }

        /// <summary>
        /// Whether or not to skip end-to-end tests that hit gallery "read" endpoints, such as V2 or the autocomplete
        /// endpoints. Note that this does not disable the usage of the push or unlist endpoints because these are
        /// required for nearly all tests.
        /// </summary>
        public bool SkipGalleryTests { get; set; }

        public static async Task<TestSettings> CreateAsync()
        {
            if (_testSettings != null)
            {
                return _testSettings;
            }

            try
            {
                await _semaphore.WaitAsync();

                if (_testSettings != null)
                {
                    return _testSettings;
                }

                _testSettings = await CreateInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }

            return _testSettings;
        }

        public static TestSettings Create()
        {
            return CreateAsync().Result;
        }

        public static async Task<TestSettings> CreateLocalTestConfigurationAsync(string configurationName)
        {
            if (configurationName == "Dev" ||
                configurationName == "Int" ||
                configurationName == "Prod")
            {
                return await CreateInternalAsync(configurationName);
            }

            throw new ArgumentException($"{configurationName} is not one of the local test supported configurations!");
        }

        /// <summary>
        /// Override base URLs for regular (Semver 1) registration to be used in Azure Search tests.
        /// </summary>
        /// <remarks>
        /// Azure search service has registration root supplied through its own configuration
        /// that it uses it responses (instead of taking one from the service index file). This makes
        /// switching to (and testing) the alternative registration hives as service index information
        /// and search service configuration will not align at all times. This setting allows providing
        /// overrides for expected registration roots for Search Service tests.
        /// </remarks>
        public List<string> SearchRegistrationBaseUrls { get; set; }

        /// <summary>
        /// Override base URLs for GZipped registration to be used in Azure Search tests.
        /// </summary>
        public List<string> SearchGZipRegistrationBaseUrls { get; set; }

        /// <summary>
        /// Override base URLs for Semver 2 registration to be used in Azure Search tests.
        /// </summary>
        public List<string> SearchSemVer2RegistrationBaseUrls { get; set; }

        private static async Task<TestSettings> CreateInternalAsync()
        {
            string configurationName = ManualConfigurationOverride;

            if (string.IsNullOrEmpty(configurationName))
            {
                configurationName = EnvironmentSettings.ConfigurationName;
            }

            return await CreateInternalAsync(configurationName);
        }

        private static async Task<TestSettings> CreateInternalAsync(string configurationName)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Environment.CurrentDirectory, "config"))
                .AddJsonFile(configurationName + ".json", optional: false);

            var configurationRoot = builder.Build();

            var configuration = new E2ESecretConfigurationReader(configurationRoot, new ConfigurationRootSecretReaderFactory(configurationRoot));
            await configuration.InjectSecrets();

            var testSettings = new TestSettings();
            configurationRoot.GetSection("TestSettings").Bind(testSettings);

            return testSettings;
        }
    }
}
