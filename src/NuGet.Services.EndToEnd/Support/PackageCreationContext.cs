// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.Packaging;

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageCreationContext
    {
        public string Id { get; set; }
        public string NormalizedVersion { get; set; }
        public string FullVersion { get; set; }
        public IEnumerable<PackageDependencyGroup> DependencyGroups { get; set; }

        public PackageProperties Properties { get; set; }
        public IReadOnlyCollection<IPackageFile> Files { get; set; }
    }
}
