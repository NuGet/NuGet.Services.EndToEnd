// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public interface IGalleryClient
    {
        Task<Uri> GetGalleryUrlAsync(ITestOutputHelper logger);
        Task PushAsync(Stream nupkgStream, ITestOutputHelper logger, bool isSymbolsPackage = false);
        Task UnlistAsync(string id, string version, ITestOutputHelper logger);
        Task RelistAsync(string id, string version, ITestOutputHelper logger);
        Task<IList<string>> AutocompletePackageIdsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger);
        Task<IList<string>> AutocompletePackageVersionsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger);
    }
}