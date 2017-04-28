// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Used to skip a test when SemVer 2.0.0 is not enabled. This should only be used on tests that explicitly require
    /// SemVer 2.0.0 packages.
    /// </summary>
    public class SemVer2FactAttribute : FactAttribute
    {
        public SemVer2FactAttribute()
        {
            var testSettings = TestSettings.CreateFromEnvironment();
            if (!testSettings.SemVer2Enabled)
            {
                Skip = "SemVer 2.0.0 is not enabled. Set the SemVer2Enabled environment variable to true to enable this test.";
            }
        }
    }
}
