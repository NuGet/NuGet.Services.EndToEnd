// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    public class AzureCloudServiceDetails
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string Name { get; set; }
        public string Slot { get; set; }
    }
}
