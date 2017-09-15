// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class CommonFixture : IAsyncLifetime
    {
        public TestSettings TestSettings { get; private set; }
        public Clients Clients { get; private set; }

        public virtual async Task InitializeAsync()
        {
            TestSettings = await TestSettings.CreateAsync();
            Clients = Clients.Initialize(TestSettings);
        }

        public virtual Task DisposeAsync()
        {
            return Task.FromResult(true);
        }
    }
}
