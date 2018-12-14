// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class SymbolServerClient
    {
        private readonly TestSettings _testSettings;
        private static readonly string SymbolServerHeaderKey = "SymbolChecksum";
        private static readonly string SymbolServerHeaderValue = "1";

        public SymbolServerClient(TestSettings testSettings)
        {
            _testSettings = testSettings;
        }

        public async Task VerifySymbolFilesAvailable(string id, string version, HashSet<string> indexedFiles, ITestOutputHelper logger)
        {
            await Task.Yield();

            await PollAsync(indexedFiles,
                $"Waiting for symbols package for {id} {version} to be available on symbol server",
                $"Symbols package {id} {version} is available on {{0}} after waiting {{1}}",
                $"Symbols package {id} {version} was still not available on {{0}} after waiting {{1}}",
                logger);
        }

        private Task PollAsync(
            HashSet<string> indexedFiles,
            string startingMessage,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            return RetryUtility.ExecuteWithRetry(
                async () =>
                {
                    logger.WriteLine(startingMessage);
                    var tasks = indexedFiles
                        .Select(file => PollAsync(file, successMessageFormat, failureMessageFormat, logger))
                        .ToList();

                    await Task.WhenAll(tasks);
                },
                ex => ex.HasTypeOrInnerType<SocketException>()
                    || ex.HasTypeOrInnerType<TaskCanceledException>(),
                logger: logger);
        }

        private async Task PollAsync(
            string indexedFileName,
            string successMessageFormat,
            string failureMessageFormat,
            ITestOutputHelper logger)
        {
            await Task.Yield();

            var complete = false;
            var symbolFileEndpoint = new Uri(string.Format(_testSettings.SymbolServerUrlTemplate, indexedFileName));

            var duration = Stopwatch.StartNew();
            while (!complete && duration.Elapsed < TestData.SymbolsWaitDuration)
            {
                using (var httpClient = new HttpClient())
                {
                    // Add Checksum headers for fetching the pdbs from symbol server. This header is used
                    // by the symbol server to validate the request(provided by supported clients) for 
                    // portable PDBs.
                    httpClient.DefaultRequestHeaders.Add(SymbolServerHeaderKey, SymbolServerHeaderValue);
                    using (var response = await httpClient.GetAsync(symbolFileEndpoint))
                    {
                        complete = response.StatusCode == HttpStatusCode.OK;

                    }
                }

                if (!complete && duration.Elapsed + TestData.SymbolsSleepDuration < TestData.SymbolsWaitDuration)
                {
                    await Task.Delay(TestData.SymbolsSleepDuration);
                }
            }

            Assert.True(complete, string.Format(failureMessageFormat, symbolFileEndpoint, duration.Elapsed));
            logger.WriteLine(string.Format(successMessageFormat, symbolFileEndpoint, duration.Elapsed));
        }
    }
}
