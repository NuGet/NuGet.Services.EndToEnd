// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    public class ServiceDetails
    {
        // The desired service slot: "Production" or "Staging"
        public string Slot { get; set; }

        // The desired service URLs.
        public bool UseConfiguredUrls { get; set; }
        public string ProductionUrl { get; set; }
        public string StagingUrl { get; set; }

        // The following configs are specific to Azure Cloud Services.
        // TODO: Remove. See: https://github.com/NuGet/Engineering/issues/2534
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string Name { get; set; }
    }
}
