// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class SignedPackageTestFactAttribute : FactAttribute
    {
        public SignedPackageTestFactAttribute()
        {
            if (string.IsNullOrEmpty(EnvironmentSettings.SignedPackagePath))
            {
                Skip = $"{nameof(EnvironmentSettings.SignedPackagePath)} is null or empty";
            }
        }
    }
}
