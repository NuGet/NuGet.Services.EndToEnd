// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NuGet.Packaging;

namespace NuGet.Services.EndToEnd.Support
{
    public class Package
    {
        private Package(
            string id,
            string normalizedVersion,
            string fullVersion,
            ReadOnlyCollection<byte> nupkgBytes)
            : this (id, normalizedVersion, fullVersion, nupkgBytes, new PackageProperties()) { }

        private Package(
            string id,
            string normalizedVersion,
            string fullVersion,
            ReadOnlyCollection<byte> nupkgBytes,
            PackageProperties properties)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NormalizedVersion = normalizedVersion ?? throw new ArgumentNullException(nameof(normalizedVersion));
            FullVersion = fullVersion ?? throw new ArgumentNullException(nameof(fullVersion));
            NupkgBytes = nupkgBytes ?? throw new ArgumentNullException(nameof(nupkgBytes));
            Properties = properties;
        }

        public string Id { get; }
        public string NormalizedVersion { get; }
        public string FullVersion { get; }
        public ReadOnlyCollection<byte> NupkgBytes { get; }
        public PackageProperties Properties { get; }

        public override string ToString()
        {
            return $"{Id} {FullVersion}";
        }

        public static Package Create(string id, string normalizedVersion, PackageType packageType)
        {
            return Create(id, normalizedVersion, normalizedVersion, packageType);
        }

        public static Package Create(string id, string normalizedVersion, string fullVersion, PackageType packageType)
        {
            return Create(new PackageCreationContext
            {
                Id = id,
                NormalizedVersion = normalizedVersion,
                FullVersion = fullVersion,
                Properties = new PackageProperties(packageType)
            });
        }

        public static Package Create(PackageCreationContext context)
        {
            ReadOnlyCollection<byte> nupkgBytes;
            using (var nupkgStream = TestData.BuildPackageStream(context))
            using (var bufferStream = new MemoryStream())
            {
                nupkgStream.CopyTo(bufferStream);
                nupkgBytes = Array.AsReadOnly(bufferStream.ToArray());
            }

            return new Package(context.Id, context.NormalizedVersion, context.FullVersion, nupkgBytes, context.Properties == null ? new PackageProperties() : context.Properties);
        }

        public static Package SignedPackage()
        {
            if (string.IsNullOrEmpty(EnvironmentSettings.SignedPackagePath) ||
                !File.Exists(EnvironmentSettings.SignedPackagePath))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{EnvironmentSettings.SignedPackagePath}' must point to a valid path");
            }

            string id;
            string normalizedVersion;
            string fullVersion;

            ReadOnlyCollection<byte> nupkgBytes;

            using (var nupkgStream = File.Open(EnvironmentSettings.SignedPackagePath, FileMode.Open))
            {
                // Extract the package's id and version.
                using (var packageReader = new PackageArchiveReader(nupkgStream, leaveStreamOpen: true))
                {
                    var identity = packageReader.GetIdentity();

                    id = identity.Id;
                    normalizedVersion = identity.Version.ToNormalizedString();
                    fullVersion = identity.Version.ToFullString();
                }

                // Copy the package's bytes into memory.
                nupkgStream.Seek(0, SeekOrigin.Begin);

                using (var bufferStream = new MemoryStream())
                {
                    nupkgStream.CopyTo(bufferStream);
                    nupkgBytes = Array.AsReadOnly(bufferStream.ToArray());
                }
            }

            return new Package(id, normalizedVersion, fullVersion, nupkgBytes, new PackageProperties(PackageType.Signed));
        }
    }
}
