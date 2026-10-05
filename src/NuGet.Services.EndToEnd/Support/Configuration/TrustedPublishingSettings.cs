// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    public sealed class TrustedPublishingSettings
    {
        public string Environment { get; set; }

        // Gallery's Entra application client ID..
        public string Audience { get; set; }

        public string[] AllowedGalleryHosts { get; set; }
    }
}