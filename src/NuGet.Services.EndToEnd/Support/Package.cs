// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;

namespace NuGet.Services.EndToEnd.Support
{
    public class Package
    {
        private Package(
            string id,
            string version,
            ReadOnlyCollection<byte> nupkgBytes)
        {
            Id = id;
            Version = version;
            NupkgBytes = nupkgBytes;
        }

        public string Id { get; }
        public string Version { get; }
        public ReadOnlyCollection<byte> NupkgBytes { get; }

        public override string ToString()
        {
            return $"{Id} {Version}";
        }

        public static Package Create(string label, string version)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyMMdd.HHmmss.fffffff");
            var id = $"E2E.{label}.{timestamp}";

            ReadOnlyCollection<byte> nupkgBytes;
            using (var nupkgStream = TestData.BuildPackageStream(id, version))
            {
                var bufferStream = new MemoryStream();
                nupkgStream.CopyTo(bufferStream);
                nupkgBytes = Array.AsReadOnly(bufferStream.ToArray());
            }

            return new Package(id, version, nupkgBytes);
        }
    }
}
