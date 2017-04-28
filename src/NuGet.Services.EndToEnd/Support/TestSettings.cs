// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettings
    {
        public TestSettings(
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

        public static TestSettings CreateFromEnvironment()
        {
            return new TestSettings(
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
