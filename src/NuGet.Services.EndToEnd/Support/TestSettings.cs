// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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

        /// <summary>
        /// A list of default test settings per environment.
        /// </summary>
        public static readonly Dictionary<Mode, TestSettings> DefaultTestSettings = new Dictionary<Mode, TestSettings>
        {
            {   Mode.Dev,
                new TestSettings(
                             DefaultAggressivePush,
                             "https://dev.nugettest.org",
                             "http://api.dev.nugettest.org/v3-index/index.json",
                             new List<string>(),
                             "fa133685-cfc3-43f3-81c7-0a36d0969987",
                             semVer2Enabled: true)
            },
            {
                Mode.Int,
                new TestSettings(
                            DefaultAggressivePush,
                            "https://int.nugettest.org",
                            "http://api.int.nugettest.org/v3-index/index.json",
                            new List<string>(),
                            "API_KEY",
                            semVer2Enabled: false)
            },
            {
                Mode.Prod,
                new TestSettings(
                            DefaultAggressivePush,
                            "https://www.nuget.org",
                            "https://api.nuget.org/v3/index.json",
                            new List<string>(),
                            "API_KEY",
                            semVer2Enabled: false)
            }
        };

        private static readonly Lazy<TestSettings> _testSettings = new Lazy<TestSettings>(() => CreateInternal(CurrentMode));

        public TestSettings(
            bool aggressivePush,
            string galleryBaseUrl,
            string v3IndexUrl,
            IReadOnlyList<string> trustedHttpsCertificates,
            string apiKey,
            bool semVer2Enabled,
            IAzureManagementAPIWrapperConfiguration azureManagementAPIWrapperConfiguration = null,
            string subscription = "",
            string searchServiceResourceGroup = "",
            string searchServiceName = "")
        {
            GalleryBaseUrl = galleryBaseUrl ?? throw new ArgumentNullException(nameof(galleryBaseUrl));
            V3IndexUrl = v3IndexUrl ?? throw new ArgumentNullException(nameof(v3IndexUrl));
            TrustedHttpsCertificates = trustedHttpsCertificates ?? throw new ArgumentNullException(nameof(trustedHttpsCertificates));
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

            AggressivePush = aggressivePush;
            SemVer2Enabled = semVer2Enabled;
            AzureManagementAPIWrapperConfiguration = azureManagementAPIWrapperConfiguration;
            Subscription = subscription;
            SearchServiceResourceGroup = searchServiceResourceGroup;
            SearchServiceName = searchServiceName;
        }

        public string GalleryBaseUrl { get; }
        public string V3IndexUrl { get; }
        public IReadOnlyList<string> TrustedHttpsCertificates { get; }
        public string ApiKey { get; }
        public bool SemVer2Enabled { get; }
        public bool AggressivePush { get; }
        public IAzureManagementAPIWrapperConfiguration AzureManagementAPIWrapperConfiguration { get; }
        public string Subscription { get; }
        public string SearchServiceResourceGroup { get; }
        public string SearchServiceName { get; }

        public static TestSettings Create()
        {
            return _testSettings.Value;
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
                        return DefaultTestSettings[mode];
                    }
                default:
                    {
                        return new TestSettings(
                            DefaultAggressivePush,
                            EnvironmentSettings.GalleryBaseUrl,
                            EnvironmentSettings.V3IndexUrl,
                            EnvironmentSettings.TrustedHttpsCertificates,
                            EnvironmentSettings.ApiKey,
                            EnvironmentSettings.SemVer2Enabled,
                            GetAzureManagementConfiguration(secretReader),
                            EnvironmentSettings.Subscription,
                            EnvironmentSettings.SearchServiceResourceGroup,
                            EnvironmentSettings.SearchServiceName);
                    }
            }
        }

        private static ISecretReader GetSecretReader()
        {
            string vaultName = EnvironmentSettings.KeyVaultName;

            if (!string.IsNullOrEmpty(vaultName))
            {
                string clientId = EnvironmentSettings.KeyVaultClientId;
                string certificateThumbprint = EnvironmentSettings.KeyVaultCertificateThumbprint;

                var certificate = CertificateUtility.FindCertificateByThumbprint(StoreName.My, StoreLocation.LocalMachine, certificateThumbprint, validationRequired: false);

                var keyVaultConfiguration = new KeyVaultConfiguration(vaultName, clientId, certificate);

                return new CachingSecretReader(new KeyVaultReader(keyVaultConfiguration));
            }

            return new EmptySecretReader();
        }

        private static IAzureManagementAPIWrapperConfiguration GetAzureManagementConfiguration(ISecretReader secretReader)
        {
            AzureManagementAPIWrapperConfiguration azureConfig = null;
            var clientId = secretReader.GetSecretAsync(EnvironmentSettings.AzureManagementAPIClientId).Result;

            if (!string.IsNullOrEmpty(clientId))
            {
                var clientSecret = secretReader.GetSecretAsync(EnvironmentSettings.AzureManagementAPIClientSecret).Result;
                azureConfig = new AzureManagementAPIWrapperConfiguration(clientId, clientSecret);
            }

            return azureConfig;
        }
    }
}
