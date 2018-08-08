// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public static class RetryUtility
    {
        public const int DefaultMaxAttempts = 5;
        public static readonly TimeSpan DefaultSleepDuration = TimeSpan.FromSeconds(30);

        public static Task ExecuteWithRetry(
            Func<Task> executeAsync,
            Func<Exception, bool> shouldRetry,
            ITestOutputHelper logger)
        {
            return ExecuteWithRetry(
                executeAsync,
                shouldRetry,
                maxAttempts: DefaultMaxAttempts,
                sleepDuration: DefaultSleepDuration,
                logger: logger);
        }

        public static Task<T> ExecuteWithRetry<T>(
            Func<Task<T>> executeAsync,
            Func<Exception, bool> shouldRetry,
            ITestOutputHelper logger)
        {
            return ExecuteWithRetry(
                executeAsync,
                shouldRetry,
                maxAttempts: DefaultMaxAttempts,
                sleepDuration: DefaultSleepDuration,
                logger: logger);
        }

        public static Task ExecuteWithRetry(
            Func<Task> executeAsync,
            Func<Exception, bool> shouldRetry,
            int maxAttempts,
            TimeSpan sleepDuration,
            ITestOutputHelper logger)
        {
            return ExecuteWithRetry(
                async () =>
                {
                    await executeAsync();
                    return 0;
                },
                shouldRetry,
                maxAttempts,
                sleepDuration,
                logger: logger);
        }

        public static async Task<T> ExecuteWithRetry<T>(
            Func<Task<T>> executeAsync,
            Func<Exception, bool> shouldRetry,
            int maxAttempts,
            TimeSpan sleepDuration,
            ITestOutputHelper logger)
        {
            var attempt = 0;
            while (true)
            {
                if (attempt > 0)
                {
                    logger.WriteLine($"Sleeping for {sleepDuration} before trying again.");
                    await Task.Delay(sleepDuration);
                }

                attempt++;

                try
                {
                    var result = await executeAsync();
                    if (attempt > 1)
                    {
                        logger.WriteLine($"Succeeded after {attempt} attempts.");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts || !shouldRetry(ex))
                    {
                        logger.WriteLine($"Failed after {attempt} attempts.");
                        throw;
                    }

                    logger.WriteLine($"Exception encountered. Will retry. Exception:{Environment.NewLine}{ex}{Environment.NewLine}");
                }
            }
        }
    }
}
