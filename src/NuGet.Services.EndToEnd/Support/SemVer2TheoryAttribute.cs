﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class SemVer2TheoryAttribute : TheoryAttribute
    {
        public SemVer2TheoryAttribute()
        {
            var testSettings = TestSettings.Create();
            if (!testSettings.SemVer2Enabled)
            {
                Skip = "SemVer 2.0.0 is not enabled. Set the SemVer2Enabled environment variable to true to enable this test.";
            }
        }
    }
}
