// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public interface IRetryingAzureManagementAPIWrapper
    {
        Task<string> GetCloudServicePropertiesAsync(string subscription, string resourceGroup, string name, string slot, ITestOutputHelper logger, CancellationToken token);
    }
}