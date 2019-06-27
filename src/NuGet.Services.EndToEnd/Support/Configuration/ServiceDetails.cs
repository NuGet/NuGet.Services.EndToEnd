// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    // TODO: Get rid of the old configs used for cloud service https://github.com/NuGet/Engineering/issues/2534 
    public class ServiceDetails
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string Name { get; set; }
        public string Slot { get; set; }
        public bool UseConfiguredUrls { get; set; }
        public string ProductionUrl { get; set; }
        public string StagingUrl { get; set; }
    }
}
