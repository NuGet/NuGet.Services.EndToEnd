// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Reflection;

namespace NuGet.Services.EndToEnd.Support
{
    public static class HttpClientExtensions
    {
        public static HttpClient AddUserAgent(this HttpClient httpClient, string clientName)
        {
            var assembly = Assembly.GetAssembly(typeof(HttpClientExtensions));
            var assemblyName = assembly.GetName().Name;
            var assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"{assemblyName}/{assemblyVersion} ({clientName}; +https://github.com/NuGet/NuGet.Services.EndToEnd)");
            return httpClient;
        }
    }
}
