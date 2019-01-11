// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            var galleryUrl = await _clients.Gallery.GetGalleryUrlAsync(_logger);
            var expectedLicenseUrl = new Uri(galleryUrl, $"packages/{package.Id}/{package.NormalizedVersion}/license");

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.Equal(package.Properties.LicenseMetadata.License, x.CatalogEntry.LicenseExpression));
            Assert.All(packageRegistrationList, x => Assert.Equal(expectedLicenseUrl.AbsoluteUri, x.CatalogEntry.LicenseUrl));
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
            var galleryUrl = await _clients.Gallery.GetGalleryUrlAsync(_logger);
            var expectedLicenseUrl = new Uri(galleryUrl, $"packages/{package.Id}/{package.NormalizedVersion}/license");

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.Equal(expectedLicenseUrl.AbsoluteUri, x.CatalogEntry.LicenseUrl));
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            var licenseFileList = await _clients.FlatContainer.TryAndGetFileStringContent(package.Id, package.NormalizedVersion, FlatContainerStringFileType.License, _logger);
            Assert.All(licenseFileList, x => Assert.Equal(package.Properties.LicenseFileContent, x));
        }
    }
}
