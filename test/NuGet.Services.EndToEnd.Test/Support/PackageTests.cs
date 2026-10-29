// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageTests
    {
        [Fact]
        public void SignedPackageCanBeReadAlongsideAnotherReader()
        {
            using (var directory = TestDirectory.Create())
            {
                var expected = Package.Create(PackageType.SemVer1Stable, "SharedReaderTest", "1.0.0");
                var path = Path.Combine(directory.FullPath, "package.nupkg");
                File.WriteAllBytes(path, expected.NupkgBytes.ToArray());
                var originalPath = EnvironmentSettings.SignedPackagePath;

                try
                {
                    EnvironmentSettings.SignedPackagePath = path;
                    using (var reader = File.OpenRead(path))
                    {
                        var actual = Package.SignedPackage();

                        Assert.Equal(expected.Id, actual.Id);
                        Assert.Equal(expected.FullVersion, actual.FullVersion);
                        Assert.Equal(expected.NupkgBytes, actual.NupkgBytes);
                        Assert.Equal(PackageType.Signed, actual.Properties.Type);
                    }
                }
                finally
                {
                    EnvironmentSettings.SignedPackagePath = originalPath;
                }
            }
        }
    }
}
