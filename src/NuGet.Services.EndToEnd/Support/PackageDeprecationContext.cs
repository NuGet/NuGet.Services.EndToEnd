// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageDeprecationContext
    {
        public bool IsLegacy { get; set; }
        public bool HasCriticalBugs { get; set; }
        public bool IsOther { get; set; }
        public string AlternatePackageId { get; set; }
        public string AlternatePackageVersion { get; set; }
        public string Message { get; set; }
    }
}
