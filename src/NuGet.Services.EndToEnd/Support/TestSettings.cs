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
            string v3SearchUrl)
        {
            GalleryBaseUrl = galleryBaseUrl ?? throw new ArgumentNullException(nameof(galleryBaseUrl));
            V3IndexUrl = v3IndexUrl ?? throw new ArgumentNullException(nameof(v3IndexUrl));
            TrustedHttpsCertificates = trustedHttpsCertificates ?? throw new ArgumentNullException(nameof(trustedHttpsCertificates));
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

            V3SearchUrl = v3SearchUrl;
        }

        public string GalleryBaseUrl { get; }
        public string V3IndexUrl { get; }
        public string V3SearchUrl { get; }
        public IReadOnlyList<string> TrustedHttpsCertificates { get; }
        public string ApiKey { get; }

        public static TestSettings CreateFromEnvironment()
        {
            return new TestSettings(
                EnvironmentSettings.GalleryBaseUrl,
                EnvironmentSettings.V3IndexUrl,
                EnvironmentSettings.TrustedHttpsCertificates,
                EnvironmentSettings.ApiKey,
                EnvironmentSettings.V3SearchUrl);
        }
    }
}
