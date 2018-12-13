// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;

namespace NuGet.Services.EndToEnd.Support.Utilities
{
    public static class PortableMetadataReader
    {
        public static string GetIndex(string pdbFullPath)
        {
            var indexingPath = string.Empty;
            if (pdbFullPath == null)
            {
                throw new ArgumentNullException(nameof(pdbFullPath));
            }

            using (var stream = File.OpenRead(pdbFullPath))
            using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(stream, MetadataStreamOptions.LeaveOpen))
            {
                var pdbReader = pdbReaderProvider.GetMetadataReader();
                var pdbSignature = new BlobContentId(pdbReader.DebugMetadataHeader.Id).Guid;
                var pdbFileName = Path.GetFileName(pdbFullPath).ToLowerInvariant();
                var pdbAge = "FFFFFFFF";
                indexingPath = $"{pdbFileName}/{pdbSignature.ToString("N")}{pdbAge}/{pdbFileName}";
            }

            return indexingPath;
        }
    }
}
