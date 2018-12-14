// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;

namespace NuGet.Services.EndToEnd.Support.Utilities
{
    public static class PortableMetadataReader
    {
        /// <summary>
        /// This gets format for the VSTS symbol server indexed files. This format is 
        /// based off of the PDB name and the signature present in the PDB 
        /// </summary>
        /// <param name="pdbFullPath">Path to the PDB file</param>
        /// <returns>The indexed format of PDB file for the VSTS symbol server</returns>
        public static string GetIndex(string pdbFullPath)
        {
            if (pdbFullPath == null)
            {
                throw new ArgumentNullException(nameof(pdbFullPath));
            }

            var indexingPath = string.Empty;
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
