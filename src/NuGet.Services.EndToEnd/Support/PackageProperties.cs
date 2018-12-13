// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.Services.EndToEnd.Support
{
    public class PackageProperties
    {
        public bool IsSymbolsPackage { get; }

        public HashSet<string> IndexedFiles { get; }

        public PackageProperties() { }

        public PackageProperties(bool isSymbolsPackage, HashSet<string> indexedFiles)
        {
            IsSymbolsPackage = isSymbolsPackage;
            IndexedFiles = indexedFiles;
        }

        public static PackageProperties Default()
        {
            return new PackageProperties();
        }
    }
}
