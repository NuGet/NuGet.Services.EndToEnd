// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace NuGet.Services.EndToEnd.Support
{
    public static class TestData
    {
        public static readonly NuGetFramework TargetFramework = NuGetFramework.Parse("net40");
        public static readonly TimeSpan FlatContainerWaitDuration = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan RegistrationWaitDuration = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan SearchWaitDuration = TimeSpan.FromMinutes(35);
        public static readonly TimeSpan V3SleepDuration = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan SymbolsWaitDuration = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan SymbolsSleepDuration = TimeSpan.FromSeconds(5);

        public static Stream BuildPackageStream(PackageCreationContext context)
        {
            if (context.Files == null)
            {
                context.Files = GetDefaultFiles();
            }

            var packageBuilder = new PackageBuilder();
            packageBuilder.Id = context.Id;
            packageBuilder.Version = NuGetVersion.Parse(context.FullVersion ?? context.NormalizedVersion);
            packageBuilder.Description = $"Description of {context.Id}";

            foreach (PhysicalPackageFile file in context.Files)
            {
                packageBuilder.Files.Add(file);
            }

            if (context.DependencyGroups != null)
            {
                packageBuilder.DependencyGroups.AddRange(context.DependencyGroups);
            }

            if (context.Properties != null && context.Properties.LicenseMetadata != null)
            {
                packageBuilder.LicenseMetadata = context.Properties.LicenseMetadata;
            }

            if (context.Properties != null && context.Properties.LicenseUrl != null)
            {
                packageBuilder.LicenseUrl = context.Properties.LicenseUrl;
            }

            if (context.Properties.Type == PackageType.SymbolsPackage)
            {
                packageBuilder.PackageTypes.Add(new Packaging.Core.PackageType("SymbolsPackage", Packaging.Core.PackageType.EmptyVersion));
            }
            else
            {
                packageBuilder.Authors.Add("EndToEndTests");
            }

            return GetStreamFromBuilder(packageBuilder);
        }

        private static List<PhysicalPackageFile> GetDefaultFiles()
        {
            return new List<PhysicalPackageFile>()
            {
                new PhysicalPackageFile(new MemoryStream())
                {
                    TargetPath = "tools/empty.txt"
                },
                new PhysicalPackageFile(new MemoryStream())
                {
                    TargetPath = $"lib/{TargetFramework.GetShortFolderName()}/_._"
                }
            };
        }

        private static Stream GetStreamFromBuilder(PackageBuilder packageBuilder)
        {
            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
