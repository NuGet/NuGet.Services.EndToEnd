// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Versioning;
using NuGet.Packaging;
using NuGet.Services.EndToEnd.Support.Utilities;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Provides files for <see cref="PackageBuilder"/> from test assembly metadata
    /// </summary>
    internal class AssemblyMetadataPackageFile : IPackageFile
    {
        private readonly string _resourceName;

        public AssemblyMetadataPackageFile(string resourceName)
        {
            _resourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
            LastWriteTime = DateTimeOffset.Now;
        }

        public string Path { get; set; }

        public string EffectivePath { get; set; }

        public FrameworkName TargetFramework { get; set; }

        public DateTimeOffset LastWriteTime { get; set; }

        public Stream GetStream()
        {
            return new MemoryStream(TestDataResourceUtility.GetResourceBytes(_resourceName));
        }
    }
}
