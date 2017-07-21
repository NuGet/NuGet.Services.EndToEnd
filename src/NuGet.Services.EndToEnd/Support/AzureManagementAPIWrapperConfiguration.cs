// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Services.AzureManagement;

namespace NuGet.Services.EndToEnd
{
    public class AzureManagementAPIWrapperConfiguration : IAzureManagementAPIWrapperConfiguration
    {
        public AzureManagementAPIWrapperConfiguration(string clientId, string clientSecret)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            ClientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        }

        public string ClientId { get; }

        public string ClientSecret { get; }
    }
}
