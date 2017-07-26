// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettings
    {
        /// <summary>
        /// Change this value to test against one of the pre-configured environments: Dev/Int/Prod.
        /// When there is no setting, the configuration will be extracted from an environment variable
        /// </summary>
        public static string ManualConfigurationOverride = "Dev";

        /// <summary>
        /// Manually override this value to easily enable aggressive pushing. This means each test will push its own
        /// packages. This is helpful when debugging a single test.
        /// </summary>
        public static bool AggressivePush => true;

        private TestSettings(string configurationName)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Path.Combine(Environment.CurrentDirectory, "config"))
              .AddJsonFile(configurationName + ".json");

            var configurationRoot = builder.Build();

            configurationRoot.GetSection("TestSettings").Bind(this);
        }

        public string GalleryBaseUrl { get; set; }
        public string V3IndexUrl { get; set; }
        public string SearchBaseUrl { get; set; }
        public IReadOnlyList<string> TrustedHttpsCertificates { get; set; }
        public string ApiKey { get; set; }
        public int SearchInstanceCount { get; set; }

        public static TestSettings Create()
        {
            string configurationName = ManualConfigurationOverride;

            if (string.IsNullOrEmpty(configurationName))
            {
                configurationName = EnvironmentSettings.ConfigurationName;
            }

            return Create(configurationName);
        }

        public static TestSettings Create(string configurationName)
        {
            return new TestSettings(configurationName);
        }
    }
}
