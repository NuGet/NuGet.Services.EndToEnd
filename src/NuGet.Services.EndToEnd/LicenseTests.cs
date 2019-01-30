// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
            var expectedPath = $"/packages/{package.Id}/{package.NormalizedVersion}/license";

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.Equal(package.Properties.LicenseMetadata.License, x.CatalogEntry.LicenseExpression));
            Assert.All(packageRegistrationList, x => Assert.Equal(expectedPath, new Uri(x.CatalogEntry.LicenseUrl).AbsolutePath));
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            VerifyLicenseUrlForV2V3Search(package, expectedPath);
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
            var expectedPath = $"/packages/{package.Id}/{package.NormalizedVersion}/license";

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.Equal(expectedPath, new Uri(x.CatalogEntry.LicenseUrl).AbsolutePath));
            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            var licenseFileList = await _clients.FlatContainer.TryAndGetFileStringContent(package.Id, package.NormalizedVersion, FlatContainerStringFileType.License, _logger);
            Assert.All(licenseFileList, x => Assert.Equal(package.Properties.LicenseFileContent, x));
            VerifyLicenseUrlForV2V3Search(package, expectedPath);
        }

        private async void VerifyLicenseUrlForV2V3Search(Package package, string expectedPath)
        {
            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            foreach (var searchService in searchServices)
            {
                var results = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}",
                    _logger);

                Assert.True(results.Data.Count > 0);
                for (var i = 0; i < results.Data.Count; i++)
                {
                    Assert.Equal(expectedPath, new Uri(results.Data[i].LicenseUrl).AbsolutePath);
                }
            }
        }
    }
}
