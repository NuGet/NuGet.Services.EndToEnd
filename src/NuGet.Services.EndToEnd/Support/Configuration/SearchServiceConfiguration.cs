// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// There are 3 modes for search service configuration:
    /// 1. SingleSearchService - if not null, only the configured search service will be used.
    /// 2. IndexJsonMappedSearchServices - if not null, we will use all search service instances configured in index.json for testing. This property will provide mapping between index.json
    /// entry and Azure service.
    /// 3. OverrideInstanceCount - provide a hard coded instance count and don't attempt to get data from Azure.
    /// </summary>
    public class SearchServiceConfiguration
    {
        public int OverrideInstanceCount { get; set; }

        public Dictionary<string, AzureCloudServiceDetails> IndexJsonMappedSearchServices { get; set; }

        public AzureCloudServiceDetails SingleSearchService { get; set; }
    }
}
