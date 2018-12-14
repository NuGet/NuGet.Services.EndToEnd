// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        /// The name of the configuration to be used. The location of all configurations is assumed to be in target directory under Config folder.
        /// The config file name to be used is [ConfigurationName].json
        /// </summary>
        public static string ConfigurationName => GetEnvironmentVariable("ConfigurationName", required: true);

        /// <summary>
        /// The path to the signed package that should be used for tests.
        /// </summary>
        public static string SignedPackagePath = GetEnvironmentVariable("SignedPackagePath", required: false);

        /// <summary>
        /// The path to the signed package that should be used for tests.
        /// </summary>
        public static string DotnetInstallDirectory = GetEnvironmentVariable("DOTNET_INSTALL_DIR", required: false);

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
