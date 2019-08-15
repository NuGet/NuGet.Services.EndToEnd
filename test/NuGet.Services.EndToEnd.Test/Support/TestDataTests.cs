// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Compression;
using NuGet.Services.EndToEnd.Support.Utilities;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestDataTests
    {
        [Theory]
        [InlineData("icon.jpg")]
        [InlineData("icon.png")]
        public void InjectsIconFile(string iconFilename)
        {
            string resourceName = "Icons." + iconFilename;
            var iconData = TestDataResourceUtility.GetResourceBytes(resourceName);
            var packageStream = TestData.BuildPackageStream(new PackageCreationContext
            {
                Id = "testP",
                NormalizedVersion = "1.0.0",
                FullVersion = "1.0.0",
                Properties = new PackageProperties(PackageType.EmbeddedIconJpeg)
                {
                    EmbeddedIconFilename = iconFilename
                },
            });

            using (packageStream)
            using (var zipArchive = new ZipArchive(packageStream, ZipArchiveMode.Read))
            {
                var iconEntry = zipArchive.GetEntry(iconFilename);
                Assert.NotNull(iconEntry);
                byte[] actualIconData;

                using (var iconDataStream = iconEntry.Open())
                using (var br = new BinaryReader(iconDataStream))
                {
                    actualIconData = br.ReadBytes(iconData.Length);
                }

                Assert.Equal(iconData, actualIconData);

                var nuspecEntry = zipArchive.GetEntry($"testP.nuspec");
                Assert.NotNull(nuspecEntry);

                using (var nuspecStream = nuspecEntry.Open())
                using (var streamReader = new StreamReader(nuspecStream))
                {
                    var nuspecContent = streamReader.ReadToEnd();
                    Assert.Contains($"<icon>{iconFilename}</icon>", nuspecContent);
                }
            }
        }
    }
}
