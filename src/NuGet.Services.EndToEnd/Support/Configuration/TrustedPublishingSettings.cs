// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    public sealed class TrustedPublishingSettings
    {
        public string Environment { get; set; }

        public string ResourceUri { get; set; }

        public string[] AllowedGalleryHosts { get; set; }
    }
}