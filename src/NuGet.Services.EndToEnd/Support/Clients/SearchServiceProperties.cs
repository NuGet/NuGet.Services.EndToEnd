// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Represents a cloud search service with multiple instances. Set the instance count to null to use the 
    /// provided Uri as is, otherwise the port is appended based on the instance counts.
    /// </summary>
    public class SearchServiceProperties
    {
        public SearchServiceProperties(Uri uri, int? instanceCount)
        {
            Uri = uri;
            InstanceCount = instanceCount;
        }

        public Uri Uri { get; }
        public int? InstanceCount { get; }
    }
}
