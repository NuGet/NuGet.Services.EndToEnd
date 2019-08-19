// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class DeprecateTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public DeprecateTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Fact]
        public async Task DeprecatePackage()
        {
            var package = await _pushedPackages.PrepareAsync(PackageType.Deprecated, _logger);

            await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, PackageDeprecationContext.Default, _logger);
        }

        [Fact]
        public async Task UndeprecatePackage()
        {
            var package = await _pushedPackages.PrepareAsync(PackageType.Undeprecated, _logger);

            await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, PackageDeprecationContext.Default, _logger);

            await _clients.Gallery.DeprecateAsync(
                package.Id, new[] { package.FullVersion }, context: null, logger: _logger);

            await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, deprecation: null, logger: _logger);
        }
    }
}
