// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace NuGet.Services.EndToEnd
{
    public static class EnvironmentSettings
    {
        private static EnvironmentVariableTarget[] Targets = new[]
        {
            EnvironmentVariableTarget.Process,
            EnvironmentVariableTarget.User,
            EnvironmentVariableTarget.Machine
        };

        public static string GalleryBaseUrl => GetEnvironmentVariable("GalleryBaseUrl");

        private static string GetEnvironmentVariable(string key)
        {
            var output = Targets
                .Select(target => Environment.GetEnvironmentVariable(key, target))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (output == null)
            {
                throw new ArgumentException($"The environment variable '{key}' is not defined.");
            }

            return output;
        }
    }
}
