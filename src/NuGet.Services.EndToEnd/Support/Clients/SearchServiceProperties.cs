// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Represents a cloud search service with multiple instances.
    /// </summary>
    public class SearchServiceProperties
    {
        public SearchServiceProperties(Uri uri, int instanceCount, bool isAzureSearch = false)
        {
            Uri = uri;
            InstanceCount = instanceCount;
            IsAzureSearch = isAzureSearch;
        }

        public Uri Uri { get; }
        public int InstanceCount { get; }
        public bool IsAzureSearch { get; }
    }
}
