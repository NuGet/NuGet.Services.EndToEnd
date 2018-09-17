// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class HttpResponseMessageExtensionsTests
    {
        public class EnsureSuccessStatusCodeOrLogAsync : BaseFacts
        {
            private readonly ITestOutputHelper _output;

            public EnsureSuccessStatusCodeOrLogAsync(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public async Task DoesNotThrowOnSuccess()
            {
                _response.StatusCode = HttpStatusCode.OK;

                await _response.EnsureSuccessStatusCodeOrLogAsync(_requestUrl, _output);
            }

            [Theory]
            [InlineData(HttpStatusCode.Found)]
            [InlineData(HttpStatusCode.NotFound)]
            [InlineData(HttpStatusCode.InternalServerError)]
            public async Task ThrowsOnFailure(HttpStatusCode statusCode)
            {
                _response.StatusCode = statusCode;

                var ex = await Assert.ThrowsAsync<HttpRequestMessageException>(
                    () => _response.EnsureSuccessStatusCodeOrLogAsync(_requestUrl, _output));

                Assert.Equal(
                    $"Response status code does not indicate success: {(int)statusCode} ({_response.ReasonPhrase}).",
                    ex.Message);
                Assert.Equal(statusCode, ex.StatusCode);
                Assert.Equal(_response.ReasonPhrase, ex.ReasonPhrase);
            }
        }

        public class AsLoggableStringAsync : BaseFacts
        {
            [Fact]
            public async Task IncludesStringResponseBody()
            {
                var output = await _response.AsLoggableStringAsync(_requestUrl);

                Assert.Equal(@"An HTTP request to 'http://localhost/robots.txt' had the following response:
============================================================
HTTP/1.1 200 Just OK
Date: Wed, 01 Aug 2018 14:05:30 GMT
X-NuGet-Warning: Beware!
Content-Type: text/plain; charset=utf-8
Content-Language: en-US

The response body is this.
============================================================
", output);
            }

            [Fact]
            public async Task HidesNonStringBody()
            {
                _response.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("The response body is this."));
                _response.Content.Headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=invalid");

                var output = await _response.AsLoggableStringAsync(_requestUrl);

                Assert.Equal(@"An HTTP request to 'http://localhost/robots.txt' had the following response:
============================================================
HTTP/1.1 200 Just OK
Date: Wed, 01 Aug 2018 14:05:30 GMT
X-NuGet-Warning: Beware!
Content-Type: text/plain; charset=invalid

(the response body could not be read as a string)
============================================================
", output);
            }
        }

        public abstract class BaseFacts
        {
            protected string _requestUrl;
            protected HttpResponseMessage _response;

            public BaseFacts()
            {
                _requestUrl = "http://localhost/robots.txt";
                _response = new HttpResponseMessage
                {
                    RequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("http://localhost/redirected/robots.txt"),
                    },
                    Version = new Version(1, 1),
                    StatusCode = HttpStatusCode.OK,
                    ReasonPhrase = "Just OK",
                    Content = new StringContent("The response body is this.", Encoding.UTF8, "text/plain"),
                };
                _response.Headers.Date = new DateTimeOffset(2018, 8, 1, 14, 5, 30, TimeSpan.Zero);
                _response.Headers.TryAddWithoutValidation("X-NuGet-Warning", "Beware!");
                _response.Content.Headers.ContentLanguage.Add("en-US");
            }
        }
    }
}
