// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public static class HttpResponseMessageExtensions
    {
        private const int LogBoundaryLength = 60;

        public static async Task EnsureSuccessStatusCodeOrLogAsync(
            this HttpResponseMessage response,
            string requestUrl,
            ITestOutputHelper logger)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (requestUrl == null)
            {
                throw new ArgumentNullException(nameof(requestUrl));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!response.IsSuccessStatusCode)
            {
                var responseString = await response.AsLoggableStringAsync(requestUrl);
                logger.WriteLine(responseString);

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex)
                {
                    // Wrap the exception so that caller has access to the status code and reason phrase.
                    throw new HttpRequestMessageException(
                        ex.Message,
                        response.StatusCode,
                        response.ReasonPhrase,
                        ex);
                }
            }
        }

        public static async Task<string> AsLoggableStringAsync(this HttpResponseMessage response, string requestUrl)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (requestUrl == null)
            {
                throw new ArgumentNullException(nameof(requestUrl));
            }

            var output = new StringBuilder();
            output.AppendLine($"An HTTP request to '{requestUrl}' had the following response:");
            output.AppendLine(new string('=', LogBoundaryLength));
            output.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

            var headers = response.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
            if (response.Content?.Headers != null)
            {
                headers = headers.Concat(response.Content.Headers);
            }

            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    output.AppendLine($"{header.Key}: {value}");
                }
            }

            output.AppendLine();

            if (response.Content != null)
            {
                try
                {
                    var bodyAsString = await response.Content.ReadAsStringAsync();
                    output.Append(bodyAsString);
                }
                catch
                {
                    output.Append("(the response body could not be read as a string)");
                }
            }

            output.AppendLine();
            output.AppendLine(new string('=', LogBoundaryLength));

            return output.ToString();
        }
    }
}
