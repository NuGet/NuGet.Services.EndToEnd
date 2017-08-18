// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Represents a cloud search service with multiple instances.
    /// </summary>
    public class SearchService
    {
        public SearchService(Uri uri, int instanceCount)
        {
            Uri = uri;
            InstanceCount = instanceCount;
        }

        public Uri Uri { get; }
        public int InstanceCount { get; }
    }
}
