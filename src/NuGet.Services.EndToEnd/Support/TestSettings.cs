// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NuGet.Services.AzureManagement;
using NuGet.Services.KeyVault;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettings
    {
        public enum Mode
        {
            EnvironmentVariables,
            Dev,
            Int,
            Prod,
        };

        /// <summary>
        /// Manually override this value to easily test end-to-end tests against one of the deployed environments. The
        /// checked in value should always be <see cref="Mode.EnvironmentVariables"/>.
        /// </summary>
        public static Mode CurrentMode => Mode.EnvironmentVariables;

        /// <summary>
        /// Manually override this value to easily enable aggressive pushing. This means each test will push its own
        /// packages. This is helpful when debugging a single test.
        /// </summary>
        public static bool DefaultAggressivePush => true;
       
        private static readonly Lazy<TestSettings> _testSettings = new Lazy<TestSettings>(() => CreateInternal(CurrentMode));

        public TestSettings(
            bool aggressivePush,
            string galleryBaseUrl,
            string v3IndexUrl,
            int searchServiceInstanceCount,
            IReadOnlyList<string> trustedHttpsCertificates,
            string apiKey,
            bool semVer2Enabled,
            IAzureManagementAPIWrapperConfiguration azureManagementAPIWrapperConfiguration = null,
            string subscription = "",
            string searchServiceResourceGroup = "",
            string searchServiceName = "",
            string searchServiceSlot = "")
        {
            GalleryBaseUrl = galleryBaseUrl ?? throw new ArgumentNullException(nameof(galleryBaseUrl));
            V3IndexUrl = v3IndexUrl ?? throw new ArgumentNullException(nameof(v3IndexUrl));
            TrustedHttpsCertificates = trustedHttpsCertificates ?? throw new ArgumentNullException(nameof(trustedHttpsCertificates));
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

            SearchInstanceCount = searchServiceInstanceCount;
            AggressivePush = aggressivePush;
            SemVer2Enabled = semVer2Enabled;
            AzureManagementAPIWrapperConfiguration = azureManagementAPIWrapperConfiguration;
            Subscription = subscription;
            SearchServiceResourceGroup = searchServiceResourceGroup;
            SearchServiceName = searchServiceName;
            SearchServiceSlot = searchServiceSlot;
        }

        public string GalleryBaseUrl { get; }
        public string V3IndexUrl { get; }

        /// <summary>
        /// Will be used as fallback in case Azure API is not configured
        /// </summary>
        public int SearchInstanceCount { get; }
        public IReadOnlyList<string> TrustedHttpsCertificates { get; }
        public string ApiKey { get; }
        public bool SemVer2Enabled { get; }
        public bool AggressivePush { get; }
        public IAzureManagementAPIWrapperConfiguration AzureManagementAPIWrapperConfiguration { get; }
        public string Subscription { get; }
        public string SearchServiceResourceGroup { get; }
        public string SearchServiceName { get; }
        public string SearchServiceSlot { get; }

        public static TestSettings Create()
        {
            return _testSettings.Value;
        }

        /// <summary>
        /// Default test settings per environment.
        /// </summary>
        public static TestSettings GetDefaultConfiguration(Mode mode)
        {
            switch (mode)
            {
                case Mode.Dev:
                    return new TestSettings(
                             DefaultAggressivePush,
                             "https://dev.nugettest.org",
                             "https://api.dev.nugettest.org/v3-index/index.json",
                             2,
                             new List<string>(),
                             "API_KEY",
                             semVer2Enabled: true);
                case Mode.Int:
                    return new TestSettings(
                            DefaultAggressivePush,
                            "https://int.nugettest.org",
                            "https://api.int.nugettest.org/v3-index/index.json",
                            2,
                            new List<string>(),
                            "API_KEY",
                            semVer2Enabled: false);
                case Mode.Prod:
                    return new TestSettings(
                            DefaultAggressivePush,
                            "https://www.nuget.org",
                            "https://api.nuget.org/v3/index.json",
                            5,
                            new List<string>(),
                            "API_KEY",
                            semVer2Enabled: false);
            }

            throw new ArgumentException($"Mode {mode} has no default configuration!");
        }

        private static TestSettings CreateInternal(Mode mode)
        {
            var secretReader = GetSecretReader();

            switch (mode)
            {
                case Mode.Dev:
                case Mode.Int:
                case Mode.Prod:
                    {
                        return GetDefaultConfiguration(mode);
                    }
                default:
                    {
                        return new TestSettings(
                            DefaultAggressivePush,
                            EnvironmentSettings.GalleryBaseUrl,
                            EnvironmentSettings.V3IndexUrl,
                            EnvironmentSettings.SearchInstanceCount,
                            EnvironmentSettings.TrustedHttpsCertificates,
                            EnvironmentSettings.ApiKey,
                            EnvironmentSettings.SemVer2Enabled,
                            GetAzureManagementConfigurationAsync(secretReader).Result,
                            EnvironmentSettings.Subscription,
                            EnvironmentSettings.SearchServiceResourceGroup,
                            EnvironmentSettings.SearchServiceName);
                    }
            }
        }

        private static ISecretReader GetSecretReader()
        {
            var vaultName = EnvironmentSettings.KeyVaultName;

            if (!string.IsNullOrEmpty(vaultName))
            {
                var clientId = EnvironmentSettings.KeyVaultClientId;
                var certificateThumbprint = EnvironmentSettings.KeyVaultCertificateThumbprint;

                var certificate = CertificateUtility.FindCertificateByThumbprint(
                    StoreName.My,
                    StoreLocation.LocalMachine,
                    certificateThumbprint,
                    validationRequired: false);

                var keyVaultConfiguration = new KeyVaultConfiguration(vaultName, clientId, certificate);

                return new CachingSecretReader(new KeyVaultReader(keyVaultConfiguration));
            }

            return new EmptySecretReader();
        }

        private static async Task<IAzureManagementAPIWrapperConfiguration> GetAzureManagementConfigurationAsync(ISecretReader secretReader)
        {
            AzureManagementAPIWrapperConfiguration azureConfig = null;
            var clientId = secretReader.GetSecretAsync(EnvironmentSettings.AzureManagementAPIClientId).Result;

            if (!string.IsNullOrEmpty(clientId))
            {
                var clientSecret = await secretReader.GetSecretAsync(EnvironmentSettings.AzureManagementAPIClientSecret);
                azureConfig = new AzureManagementAPIWrapperConfiguration(clientId, clientSecret);
            }

            return azureConfig;
        }
    }
}
