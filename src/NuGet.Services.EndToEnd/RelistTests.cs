// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class RelistTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public RelistTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Theory]
        [InlineData(PackageType.SemVer2PrerelRelisted, true)]
        public async Task RelistedPackageReappearsInRegistrationAndSearch(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);

            var listed = false;
            await _clients.Registration.WaitForListedStateAsync(package.Id, package.FullVersion, semVer2, listed, _logger);
            await _clients.V2V3Search.WaitForListedStateAsync(package.Id, package.FullVersion, listed, _logger);

            // Act
            await _clients.Gallery.RelistAsync(package.Id, package.NormalizedVersion, _logger);

            // Assert
            listed = true;
            await _clients.Registration.WaitForListedStateAsync(package.Id, package.FullVersion, semVer2, listed, _logger);
            await _clients.V2V3Search.WaitForListedStateAsync(package.Id, package.FullVersion, listed, _logger);
        }
    }
}
