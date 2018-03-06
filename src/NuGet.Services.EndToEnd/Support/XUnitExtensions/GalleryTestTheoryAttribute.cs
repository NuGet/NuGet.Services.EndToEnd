// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class GalleryTestTheoryAttribute : TheoryAttribute
    {
        public GalleryTestTheoryAttribute()
        {
            var settings = TestSettings.CreateAsync().Result;
            if (!settings.RunGalleryTests)
            {
                Skip = "Not running Gallery tests!";
            }
        }
    }
}
