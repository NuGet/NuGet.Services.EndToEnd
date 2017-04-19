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
        /// Push a package to the gallery and wait for it to be available in search.
        /// </summary>
        [Fact]
        public async Task NewlyPushedPackageIsAvailableInV3Search()
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(PackageType.SemVer1, _logger);

            // Act & Assert
            await _clients.V3Search.WaitForPackageAsync(package.Id, package.Version, _logger);
        }

        /// <summary>
        /// Push a package to the gallery and wait for it to be available in the flat container.
        /// </summary>
        [Fact]
        public async Task NewlyPushedPackageIsAvailableInFlatContainer()
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(PackageType.SemVer1, _logger);

            // Act & Assert
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.Version, _logger);
        }

        /// <summary>
        /// Push a package to the gallery and wait for it to be available in the flat container.
        /// </summary>
        [Fact]
        public async Task NewlyPushedPackageIsAvailableInRegistration()
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(PackageType.SemVer1, _logger);

            // Act & Assert
            await _clients.Registration.WaitForPackageAsync(package.Id, package.Version, _logger);
        }
    }
}
