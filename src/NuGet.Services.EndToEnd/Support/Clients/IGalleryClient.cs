// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;

namespace NuGet.Services.EndToEnd.Support
{
    public interface IGalleryClient
    {
        Task PushAsync(Stream nupkgStream);
    }
}