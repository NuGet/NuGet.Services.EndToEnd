// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.EndToEnd.Support;
using Xunit;

namespace NuGet.Services.EndToEnd
{
    public class AzureSearchOnlyAttribute : FactAttribute
    {
        public AzureSearchOnlyAttribute()
        {
            if (!TestSettings.Create().IsTestingAzureSearchService())
            {
                Skip = "Not running against azure search service!";
            }
        }
    }
}