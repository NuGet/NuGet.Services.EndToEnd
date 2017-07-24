// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// This code duplicates some existing infrastructure in gallery's functional test suite. It should be
    /// consolidated at some point.
    /// https://github.com/NuGet/NuGetGallery/blob/master/tests/NuGetGallery.FunctionalTests.Core/EnvironmentSettings.cs
    /// </summary>
    public static class EnvironmentSettings
    {
        private static EnvironmentVariableTarget[] Targets = new[]
        {
            EnvironmentVariableTarget.Process,
            EnvironmentVariableTarget.User,
            EnvironmentVariableTarget.Machine
        };

        /// <summary>
        /// The base URL of the gallery. Gallery UI and V2 endpoint URLs are derived off of this value.
        /// </summary>
        /// <example>
        /// https://int.nugettest.org
        /// </example>
        public static string GalleryBaseUrl => GetEnvironmentVariable("GalleryBaseUrl", required: true).TrimEnd('/');

        /// <summary>
        /// The URL to the V3 index.json file. This is used to discover URLs of different NuGet services.
        /// </summary>
        /// <example>
        /// http://api.int.nugettest.org/v3-index/index.json
        /// </example>
        public static string V3IndexUrl => GetEnvironmentVariable("V3IndexUrl", required: true);

        /// <summary>
        /// The base URL to search service. This value is optional. If not provided, the search services that will
        /// be tested will be found in the V3 index.json (via <see cref="V3IndexUrl"/>).
        /// </summary>
        /// <example>
        /// http://nuget-int-0-v2v3search.cloudapp.net
        /// </example>
        public static string SearchBaseUrl => GetEnvironmentVariable("SearchBaseUrl", required: false)?.TrimEnd('/');

        /// <summary>
        /// The number of search service instances. This value is required and is used when polling the search service
        /// for package ability to make sure all instances have the requested package.
        /// </summary>
        public static int SearchInstanceCount
        {
            get
            {
                var unparsedInstanceCount = GetEnvironmentVariable("SearchInstanceCount", required: true);
                if (!int.TryParse(unparsedInstanceCount, out int instanceCount))
                {
                    throw new ArgumentException("The environment variable 'SearchInstanceCount' must be parsable as an integer.");
                }

                return instanceCount;
            }
        }

        /// <summary>
        /// An optional comma-seperated list of SHA1 certificate fingerprints to trust. In this case, trust means to
        /// allow SSL failures of type "remote certificate name mismatch". That is, we are attempting to hit and HTTPS
        /// endpoint that via a domain name that does not match the SSL certificate. This is important when hitting the
        /// staging slot Azure cloud services.
        /// </summary>
        /// <example>
        /// 9d984f91f40d8b3a1fb29153179415523c4e64d1
        /// </example>
        public static IReadOnlyList<string> TrustedHttpsCertificates
        {
            get
            {
                var fingerprints = GetEnvironmentVariable("TrustedHttpsCertificates", required: false);
                if (fingerprints == null)
                {
                    return new string[0];
                }

                return fingerprints
                    .Split(',')
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// The API key to use when pushing packages. This package should have the scopes necessary to upload
        /// new package IDs, since the end-to-end tests create new packages.
        /// </summary>
        public static string ApiKey => GetEnvironmentVariable("ApiKey", required: true);

        private static string GetEnvironmentVariable(string key, bool required)
        {
            var output = Targets
                .Select(target => Environment.GetEnvironmentVariable(key, target))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .FirstOrDefault();

            if (required && output == null)
            {
                throw new ArgumentException($"The environment variable '{key}' is not defined.");
            }

            return output;
        }
    }
}
