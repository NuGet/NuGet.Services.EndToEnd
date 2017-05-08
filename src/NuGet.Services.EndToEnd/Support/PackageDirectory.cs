// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageDirectory
    {
        private readonly string _path;

        public PackageDirectory(string path)
        {
            _path = path;
        }

        public string AddPackage(Package package)
        {
            var path = Path.Combine(
                _path,
                $"{package.Id}.{package.NormalizedVersion}.nupkg");

            File.WriteAllBytes(path, package.NupkgBytes.ToArray());

            return path;
        }

        public static implicit operator string(PackageDirectory directory)
        {
            return directory._path;
        }

        public override string ToString()
        {
            return _path;
        }
    }
}
