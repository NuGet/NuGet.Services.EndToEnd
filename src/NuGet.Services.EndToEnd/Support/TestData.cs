// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace NuGet.Services.EndToEnd.Support
{
    public static class TestData
    {
        public static readonly NuGetFramework PackageFramework = NuGetFramework.Parse("net40");
        public static readonly TimeSpan V3WaitDuration = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan V3SleepDuration = TimeSpan.FromSeconds(5);

        public static Stream BuildPackageStream(PackageCreationContext context)
        {
            var packageBuilder = new PackageBuilder();
            packageBuilder.Id = context.Id;
            packageBuilder.Version = NuGetVersion.Parse(context.FullVersion ?? context.NormalizedVersion);
            packageBuilder.Authors.Add("EndToEndTests");
            packageBuilder.Description = $"Description of {context.Id}";
            packageBuilder.Files.Add(new PhysicalPackageFile(new MemoryStream())
            {
                TargetPath = "tools/empty.txt"
            });
            packageBuilder.Files.Add(new PhysicalPackageFile(new MemoryStream())
            {
                TargetPath = $"lib/{PackageFramework.GetShortFolderName()}/_._"
            });

            if (context.DependencyGroups != null)
            {
                packageBuilder.DependencyGroups.AddRange(context.DependencyGroups);
            }

            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
    }
}
