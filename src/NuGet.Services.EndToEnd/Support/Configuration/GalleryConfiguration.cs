// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// There are 2 modes for Gallery configuration:
    /// 1. ServiceDetails - if not empty read the service URL from Azure and use that.
    /// 2. OverrideServiceUrl - this URL will be used if ServiceDetails are empty
    /// </summary>
    public class GalleryConfiguration
    {
        public string GalleryBaseUrl { get; set; }

        public ServiceDetails ServiceDetails { get; set; }
    }
}
