// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Different package variants used for testing. Each package type will produce a single package per test run via
    /// the <see cref="PushedPackagesFixture"/>.
    /// </summary>
    public enum PackageType
    {
        SemVer1Stable,
        SemVer2Prerelease,
        SemVer1Unlisted,
        SemVer2Unlisted,
    }
}
