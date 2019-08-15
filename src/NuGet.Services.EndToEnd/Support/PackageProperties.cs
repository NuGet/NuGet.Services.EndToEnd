// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Packaging;
using System;
using System.Collections.Generic;

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageProperties
    {
        public PackageType Type { get; }
        public HashSet<string> IndexedFiles { get; }
        public LicenseMetadata LicenseMetadata { get; }
        public string LicenseFileContent { get; }
        public Uri LicenseUrl { get; }
        public string EmbeddedIconFilename { get; set; }

        public PackageProperties(PackageType packageType)
        {
            Type = packageType;
        }

        public PackageProperties(PackageType packageType, HashSet<string> indexedFiles)
        {
            Type = packageType;
            IndexedFiles = indexedFiles;
        }

        public PackageProperties(PackageType packageType, Uri licenseUrl)
        {
            Type = packageType;
            LicenseUrl = licenseUrl;
        }

        public PackageProperties(PackageType packageType, LicenseMetadata licenseMetadata)
            : this(packageType, licenseMetadata, null) { }

        public PackageProperties(PackageType packageType, LicenseMetadata licenseMetadata, string licenseFileContent)
        {
            Type = packageType;
            LicenseMetadata = licenseMetadata;
            LicenseFileContent = licenseFileContent;
        }
    }
}
