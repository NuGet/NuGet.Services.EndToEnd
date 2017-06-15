// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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
        /// Manually override this value to easily test end-to-end tests against on of the deployed environments. The
        /// checked in value should always be <see cref="Mode.EnvironmentVariables"/>.
        /// </summary>
        public static Mode CurrentMode => Mode.EnvironmentVariables;

        /// <summary>
        /// Manually override this value to easily enable aggressive pushing. This means each test will push its own
        /// packages. This is helpful when debugging a single test.
        /// </summary>
        public static bool DefaultAggressivePush => true;

        public TestSettings(
            bool aggressivePush,
            string galleryBaseUrl,
            string v3IndexUrl,
            IReadOnlyList<string> trustedHttpsCertificates,
            string apiKey,
            string searchBaseUrl,
            bool semVer2Enabled,
            int searchInstanceCount)
        {
            GalleryBaseUrl = galleryBaseUrl ?? throw new ArgumentNullException(nameof(galleryBaseUrl));
            V3IndexUrl = v3IndexUrl ?? throw new ArgumentNullException(nameof(v3IndexUrl));
            TrustedHttpsCertificates = trustedHttpsCertificates ?? throw new ArgumentNullException(nameof(trustedHttpsCertificates));
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

            AggressivePush = aggressivePush;
            SearchBaseUrl = searchBaseUrl;
            SemVer2Enabled = semVer2Enabled;
            SearchInstanceCount = searchInstanceCount;
        }

        public string GalleryBaseUrl { get; }
        public string V3IndexUrl { get; }
        public string SearchBaseUrl { get; }
        public IReadOnlyList<string> TrustedHttpsCertificates { get; }
        public string ApiKey { get; }
        public bool SemVer2Enabled { get; }
        public int SearchInstanceCount { get; }
        public bool AggressivePush { get; }

        public static TestSettings Create()
        {
            return Create(CurrentMode);
        }

        public static TestSettings Create(Mode mode)
        {
            switch (mode)
            {
                case Mode.Dev:
                    return new TestSettings(
                        DefaultAggressivePush,
                        "https://dev.nugettest.org",
                        "http://api.dev.nugettest.org/v3-index/index.json",
                        new List<string>(),
                        "API_KEY",
                        searchBaseUrl: null,
                        semVer2Enabled: true,
                        searchInstanceCount: 2);
                case Mode.Int:
                    return new TestSettings(
                        DefaultAggressivePush,
                        "https://int.nugettest.org",
                        "http://api.int.nugettest.org/v3-index/index.json",
                        new List<string>(),
                        "API_KEY",
                        searchBaseUrl: null,
                        semVer2Enabled: false,
                        searchInstanceCount: 2);
                case Mode.Prod:
                    return new TestSettings(
                        DefaultAggressivePush,
                        "https://www.nuget.org",
                        "https://api.nuget.org/v3/index.json",
                        new List<string>(),
                        "API_KEY",
                        searchBaseUrl: null,
                        semVer2Enabled: false,
                        searchInstanceCount: 5);
                default:
                    return new TestSettings(
                        DefaultAggressivePush,
                        EnvironmentSettings.GalleryBaseUrl,
                        EnvironmentSettings.V3IndexUrl,
                        EnvironmentSettings.TrustedHttpsCertificates,
                        EnvironmentSettings.ApiKey,
                        EnvironmentSettings.SearchBaseUrl,
                        EnvironmentSettings.SemVer2Enabled,
                        EnvironmentSettings.SearchInstanceCount);
            }
        }
    }
}
