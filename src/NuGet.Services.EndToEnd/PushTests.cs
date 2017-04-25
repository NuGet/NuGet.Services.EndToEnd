// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class PushTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public PushTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _logger = logger;
        }

        /// <summary>
        /// Push a SemVer 1.0.0 package to the gallery and wait for it to be available in V3.
        /// </summary>
        [Fact]
        public async Task NewlyPushedSemVer1PackageIsAvailableInV3()
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(PackageType.SemVer1Stable, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.Version, _logger);

            await _clients.Registration.WaitForPackageAsync(package.Id, package.Version, semVer2: false, logger: _logger);

            await _clients.V3Search.WaitForPackageAsync(package.Id, package.Version, _logger);
        }

        /// <summary>
        /// Push a SemVer 2.0.0 package to the gallery and wait for it to be available in V3.
        /// </summary>
        [SemVer2Fact]
        public async Task NewlyPushedSemVer2PackageIsAvailableInV3()
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(PackageType.SemVer2Prerelease, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.Version, _logger);

            await _clients.Registration.WaitForPackageAsync(package.Id, package.Version, semVer2: true, logger: _logger);

            await _clients.V3Search.WaitForPackageAsync(package.Id, package.Version, _logger);
        }
    }
}
