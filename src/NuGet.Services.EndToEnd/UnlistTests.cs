// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class UnlistTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public UnlistTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = Clients.Initialize();
            _logger = logger;
        }

        /// <summary>
        /// A package is by default listed in registration right after initial push.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1Stable, false)]
        [SemVer2InlineData(PackageType.SemVer2Prerelease, true)]
        public async Task PackageInitiallyShowsAsListedInRegistration(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(packageType, _logger);

            await _clients.Registration.WaitForPackageAsync(package.Id, package.Version, semVer2, logger: _logger);

            await _clients.Gallery.UnlistAsync(package.Id, package.Version);

            // Act & Assert
            await _clients.Registration.WaitForListedStateAsync(
                package.Id,
                package.Version,
                semVer2,
                listed: true,
                logger: _logger);
        }

        /// <summary>
        /// An unlisted package should have indicate that it is unlisted in the registration blobs.
        /// </summary>
        [Theory]
        [InlineData(PackageType.SemVer1Unlisted, false)]
        [SemVer2InlineData(PackageType.SemVer2Unlisted, true)]
        public async Task UnlistedPackageShowsAsUnlistedInRegistration(PackageType packageType, bool semVer2)
        {
            // Arrange
            var package = await _pushedPackages.PushAsync(packageType, _logger);
            
            await _clients.Registration.WaitForPackageAsync(package.Id, package.Version, semVer2, logger: _logger);

            await _clients.Gallery.UnlistAsync(package.Id, package.Version);

            // Act & Assert
            await _clients.Registration.WaitForListedStateAsync(
                package.Id,
                package.Version,
                semVer2,
                listed: false,
                logger: _logger);
        }
    }
}
