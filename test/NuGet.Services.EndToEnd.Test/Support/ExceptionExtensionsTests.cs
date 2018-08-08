// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class ExceptionExtensionsTests
    {
        public class HasTypeOrInnerType
        {
            [Fact]
            public void ReturnsTrueWhenExceptionItselfMatchesType()
            {
                var target = new InvalidOperationException(
                    "bad!",
                    new ApplicationException("something else"));

                Assert.True(target.HasTypeOrInnerType<InvalidOperationException>());
            }

            [Fact]
            public void ReturnsTrueWhenInnerExceptionMatchesType()
            {
                var target = new ApplicationException(
                    "bad!",
                    new InvalidOperationException("something else"));

                Assert.True(target.HasTypeOrInnerType<InvalidOperationException>());
            }

            [Fact]
            public void ReturnsTrueWhenInnerInnerExceptionMatchesType()
            {
                var target = new HttpRequestException(
                    "An error occurred while sending the request.",
                    new WebException(
                        "Unable to connect to the remote server",
                        new SocketException(10060)));

                Assert.True(target.HasTypeOrInnerType<SocketException>());
            }

            [Fact]
            public void ReturnsTrueWhenInnerInnerExceptionInheritsType()
            {
                var target = new HttpRequestException(
                    "An error occurred while sending the request.",
                    new WebException(
                        "Unable to connect to the remote server",
                        new SocketException(10060)));

                Assert.True(target.HasTypeOrInnerType<InvalidOperationException>());
            }

            [Fact]
            public void ReturnsFalseWhenNoExceptionMatchesType()
            {
                var target = new HttpRequestException(
                    "An error occurred while sending the request.",
                    new WebException(
                        "Unable to connect to the remote server",
                        new SocketException(10060)));

                Assert.False(target.HasTypeOrInnerType<ApplicationException>());
            }
        }
    }
}
