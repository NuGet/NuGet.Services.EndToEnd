// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    internal sealed class PublishingKey
    {
        public string TokenType { get; set; }
        public string ApiKey { get; set; }
        public DateTimeOffset? Expires { get; set; }
    }
}