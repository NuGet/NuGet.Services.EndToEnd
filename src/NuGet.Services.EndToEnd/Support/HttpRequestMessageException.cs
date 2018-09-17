// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace NuGet.Services.EndToEnd.Support
{
    public class HttpRequestMessageException : Exception
    {
        public HttpRequestMessageException(
            string message,
            HttpStatusCode statusCode,
            string reasonPhrase,
            Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase ?? throw new ArgumentNullException(nameof(reasonPhrase));
        }

        public HttpStatusCode StatusCode { get; }
        public string ReasonPhrase { get; }
    }
}
