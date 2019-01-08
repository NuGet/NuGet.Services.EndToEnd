// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Packaging;
using System.Collections.Generic;

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageProperties
    {
        public PackageType Type { get; }

        public HashSet<string> IndexedFiles { get; }

        public LicenseMetadata LicenseMetadata { get; }

        public PackageProperties() { }

        public PackageProperties(PackageType packageType)
        {
            Type = packageType;
        }

        public PackageProperties(PackageType packageType, HashSet<string> indexedFiles)
        {
            Type = packageType;
            IndexedFiles = indexedFiles;
        }

        public PackageProperties(PackageType packageType, LicenseMetadata licenseMetadata)
        {
            Type = packageType;
            LicenseMetadata = licenseMetadata;
        }

        public static PackageProperties Default()
        {
            return new PackageProperties();
        }
    }
}
