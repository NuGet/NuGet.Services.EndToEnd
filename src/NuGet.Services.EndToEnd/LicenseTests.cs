// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class LicenseTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly ITestOutputHelper _logger;
        private readonly Clients _clients;

        public LicenseTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        /// <summary>
        /// Push a package with license expression to the gallery and wait for it to be available in V3.
        /// </summary>
        [Fact]
        public async Task PushedPackageWithLicenseExpressionIsAvailableInV3()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.LicenseExpression, _logger);

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.True(packageRegistrationList.All(x => x.CatalogEntry.LicenseExpression == package.Properties.LicenseMetadata.License));
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
        }
        
        /// <summary>
        /// Push a package with license file to the gallery and wait for it to be available in V3.
        /// </summary>
        [Fact]
        public async Task PushedPackageWithLicenseFileIsAvailableInV3()
        {
            // Arrange
            var package = await _pushedPackages.PrepareAsync(PackageType.LicenseFile, _logger);

            // Act & Assert
            await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            var licenseFileList = await _clients.FlatContainer.TryAndGetFileContent(package.Id, package.NormalizedVersion, "license", _logger);
            Assert.True(licenseFileList.All(x => x == package.Properties.LicenseFileContent));
        }
    }
}
