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
        /// <summary>
        /// Get the gallery base URL used for interacting with the system. When deploying a component that is not
        /// NuGetGallery, this will be the well-known public URL. When deploying NuGetGallery, this will be the staging
        /// slot URL.
        /// </summary>
        Uri GetGalleryServiceBaseUrl();

        /// <summary>
        /// Get the well-known public URL of the NuGetGallery. This method should only be used when asserting the value
        /// of a URL that is expected to point to the well known URL of NuGetGallery. Use the value returned by
        /// <see cref="GetGalleryServiceBaseUrl"/> to interact with the gallery endpoints.
        /// </summary>
        Uri GetGalleryBaseUrl();

        Task PushAsync(Stream nupkgStream, ITestOutputHelper logger, PackageType packageType);
        Task UnlistAsync(string id, string version, ITestOutputHelper logger);
        Task RelistAsync(string id, string version, ITestOutputHelper logger);
        Task DeprecateAsync(string id, IReadOnlyCollection<string> versions, PackageDeprecationContext context, ITestOutputHelper logger);
        Task<IList<string>> AutocompletePackageIdsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger);
        Task<IList<string>> AutocompletePackageVersionsAsync(string id, bool includePrerelease, string semVerLevel, ITestOutputHelper logger);
        Task SearchPackageODataV2FromDBAsync(string id, string version, ITestOutputHelper logger);
    }
}