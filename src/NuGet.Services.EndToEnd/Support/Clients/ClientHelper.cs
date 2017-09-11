// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    public static class ClientHelper
    {
        public static Uri ConvertToHttpsAndClean(Uri uri)
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Scheme = "https";
            var clean = uriBuilder.Uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port,
                       UriFormat.UriEscaped);

            return new Uri(clean.ToString());
        }
    }
}
