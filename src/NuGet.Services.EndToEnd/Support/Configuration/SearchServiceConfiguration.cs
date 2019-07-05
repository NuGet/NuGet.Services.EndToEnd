// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        public Dictionary<string, ServiceDetails> IndexJsonMappedSearchServices { get; set; }

        public bool UseOfficialDns { get; set; }

        public ServiceDetails SingleSearchService { get; set; }

        public AzureManagementAPIWrapperConfiguration AzureManagementAPIWrapperConfiguration { get; set; }

        /// <summary>
        /// Additional time to poll after a package was found in the search index. This is necessary for Azure Search
        /// Service since a package can appear in one Azure Search replica before another. We could go deep into the
        /// likelihood of hitting N replicas after M queries (a la coupon collector problem) but it is simpler to have
        /// a duration setting that we can turn up and down for a desired balance between test run time and flakiness.
        /// Another complexity is that we poll the hijack index but some tests follow up with queries to the search
        /// index. These two indexes are updated independently (although very close in time) so there are two sources
        /// of variability in when packages show up in results.
        /// </summary>
        public TimeSpan AdditionalPollingDuration { get; set; }
    }
}
