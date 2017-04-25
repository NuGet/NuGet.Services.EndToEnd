// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using NuGet.Packaging;
using NuGet.Versioning;

namespace NuGet.Services.EndToEnd.Support
{
    public static class TestData
    {
        public static readonly TimeSpan V3WaitDuration = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan V3SleepDuration = TimeSpan.FromSeconds(5);

        public static Stream BuildPackageStream(string id, string version)
        {
            var packageBuilder = new PackageBuilder();
            packageBuilder.Id = id;
            packageBuilder.Version = NuGetVersion.Parse(version);
            packageBuilder.Authors.Add("EndToEndTests");
            packageBuilder.Description = id;
            packageBuilder.Files.Add(new PhysicalPackageFile(new MemoryStream())
            {
                TargetPath = "tools/empty.txt"
            });

            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
    }
}
