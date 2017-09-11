// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.AzureManagement;

namespace NuGet.Services.EndToEnd
{
    public class AzureManagementAPIWrapperConfiguration : IAzureManagementAPIWrapperConfiguration
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
