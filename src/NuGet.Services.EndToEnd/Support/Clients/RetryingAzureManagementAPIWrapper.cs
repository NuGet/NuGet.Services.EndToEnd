// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Services.AzureManagement;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class RetryingAzureManagementAPIWrapper : IRetryingAzureManagementAPIWrapper
    {
        private readonly IAzureManagementAPIWrapper _inner;
        private readonly TimeSpan _sleepDuration;

        public RetryingAzureManagementAPIWrapper(IAzureManagementAPIWrapper inner, TimeSpan sleepDuration)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _sleepDuration = sleepDuration;
        }

        public Task<string> GetCloudServicePropertiesAsync(
            string subscription,
            string resourceGroup,
            string name,
            string slot,
            ITestOutputHelper logger,
            CancellationToken token)
        {
            return RetryUtility.ExecuteWithRetry(
                () => _inner.GetCloudServicePropertiesAsync(
                    subscription,
                    resourceGroup,
                    name,
                    slot,
                    token),
                ex => ex is AzureManagementException,
                maxAttempts: RetryUtility.DefaultMaxAttempts,
                sleepDuration: _sleepDuration,
                logger: logger);
        }
    }
}
