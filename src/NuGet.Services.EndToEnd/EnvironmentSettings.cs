// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace NuGet.Services.EndToEnd
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

        public static string GalleryBaseUrl => GetEnvironmentVariable("GalleryBaseUrl", required: true);

        public static string[] TrustedHttpsCertificates
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
                    .ToArray();
            }
        }

        private static string GetEnvironmentVariable(string key, bool required)
        {
            var output = Targets
                .Select(target => Environment.GetEnvironmentVariable(key, target))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (required && output == null)
            {
                throw new ArgumentException($"The environment variable '{key}' is not defined.");
            }

            return output;
        }
    }
}
