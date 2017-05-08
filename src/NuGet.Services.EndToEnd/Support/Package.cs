// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace NuGet.Services.EndToEnd.Support
{
    public class Package
    {
        private Package(
            string id,
            string normalizedVersion,
            string fullVersion,
            ReadOnlyCollection<byte> nupkgBytes)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NormalizedVersion = normalizedVersion ?? throw new ArgumentNullException(nameof(normalizedVersion));
            FullVersion = fullVersion ?? throw new ArgumentNullException(nameof(fullVersion));
            NupkgBytes = nupkgBytes ?? throw new ArgumentNullException(nameof(nupkgBytes));
        }

        public string Id { get; }
        public string NormalizedVersion { get; }
        public string FullVersion { get; }
        public ReadOnlyCollection<byte> NupkgBytes { get; }

        public override string ToString()
        {
            return $"{Id} {FullVersion}";
        }

        public static Package Create(string id, string normalizedVersion)
        {
            return Create(id, normalizedVersion, normalizedVersion);
        }

        public static Package Create(string id, string normalizedVersion, string fullVersion)
        {
            return Create(new PackageCreationContext
            {
                Id = id,
                NormalizedVersion = normalizedVersion,
                FullVersion = fullVersion,
            });
        }

        public static Package Create(PackageCreationContext context)
        {
            ReadOnlyCollection<byte> nupkgBytes;
            using (var nupkgStream = TestData.BuildPackageStream(context))
            {
                var bufferStream = new MemoryStream();
                nupkgStream.CopyTo(bufferStream);
                nupkgBytes = Array.AsReadOnly(bufferStream.ToArray());
            }

            return new Package(context.Id, context.NormalizedVersion, context.FullVersion, nupkgBytes);
        }
    }
}
