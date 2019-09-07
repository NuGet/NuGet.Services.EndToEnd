// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class RetryUtilityTests
    {
        private readonly ITestOutputHelper _output;

        public RetryUtilityTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public async Task DoesNotRetryIfNoExceptionIsThrown()
        {
            var attempts = 0;
            Func<Task<int>> executeAsync = () => Task.Run(() =>
            {
                attempts++;
                return Task.FromResult(23);
            });

            var result = await RetryUtility.ExecuteWithRetry(
                executeAsync,
                ex => true,
                _output);

            Assert.Equal(1, attempts);
            Assert.Equal(23, result);
        }

        [Fact]
        public async Task TriesUpToMaxAttemptsTimes()
        {
            var attempts = 0;
            Func<Task<int>> executeAsync = () => Task.Run<int>(async () =>
            {
                attempts++;
                await Task.Yield();
                throw new InvalidOperationException("Bad! " + attempts);
            });

            var actualEx = await Assert.ThrowsAsync<InvalidOperationException>(() => RetryUtility.ExecuteWithRetry(
                executeAsync,
                ex => true,
                maxAttempts: 5,
                sleepDuration: TimeSpan.Zero,
                logger: _output));

            Assert.Equal(5, attempts);
            Assert.Equal("Bad! 5", actualEx.Message);
        }

        [Fact]
        public async Task SleepsSleepDurationBetweenAttempts()
        {
            var minimum = TimeSpan.FromMilliseconds(400);
            Func<Task<int>> executeAsync = () => Task.Run<int>(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Bad!");
            });

            var stopwatch = Stopwatch.StartNew();
            var actualEx = await Assert.ThrowsAsync<InvalidOperationException>(() => RetryUtility.ExecuteWithRetry(
                executeAsync,
                ex => true,
                maxAttempts: 5,
                sleepDuration: TimeSpan.FromMilliseconds(100),
                logger: _output));
            stopwatch.Stop();

            Assert.True(
                stopwatch.Elapsed >= minimum,
                $"Elapsed was {stopwatch.Elapsed}. Should be greater than or equal to {minimum}.");
            Assert.Equal("Bad!", actualEx.Message);
        }

        [Fact]
        public async Task ReturnsResultAfterSomeRetries()
        {
            var attempts = 0;
            Func<Task<int>> executeAsync = () => Task.Run(async () =>
            {
                attempts++;
                if (attempts > 3)
                {
                    return 23;
                }

                await Task.Yield();
                throw new InvalidOperationException("Bad! " + attempts);
            });

            var result = await RetryUtility.ExecuteWithRetry(
                executeAsync,
                ex => true,
                maxAttempts: 5,
                sleepDuration: TimeSpan.Zero,
                logger: _output);

            Assert.Equal(4, attempts);
            Assert.Equal(23, result);
        }

        [Fact]
        public async Task DoesNotRetryIfShouldNotRetry()
        {
            var attempts = 0;
            Func<Exception, bool> shouldRetry = ex => ex is InvalidOperationException;
            Func<Task<int>> executeAsync = () => Task.Run<int>(async () =>
            {
                attempts++;
                await Task.Yield();
                throw new ApplicationException("Bad!");
            });

            var actualEx = await Assert.ThrowsAsync<ApplicationException>(() => RetryUtility.ExecuteWithRetry(
                executeAsync,
                ex => true,
                maxAttempts: 5,
                sleepDuration: TimeSpan.Zero,
                logger: _output));

            Assert.Equal(5, attempts);
            Assert.Equal("Bad!", actualEx.Message);
        }
    }
}
