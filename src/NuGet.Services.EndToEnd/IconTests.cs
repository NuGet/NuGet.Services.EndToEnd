// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using NuGet.Services.EndToEnd.Support.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class IconTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public IconTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Theory]
        [InlineData(PackageType.EmbeddedIconJpeg)]
        public async Task PushedPackageWithIconIsAvailableInV3(PackageType packageType)
        {
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            var galleryUrl = _clients.Gallery.GetGalleryBaseUrl(_logger);
            var expectedPath = new Uri(galleryUrl, $"packages/{package.Id}/{package.NormalizedVersion}/icon");
            string expectedExtension;
            switch (packageType)
            {
                case PackageType.EmbeddedIconJpeg:
                    expectedExtension = ".jpg";
                    break;

                default:
                    throw new ArgumentException($"Unsupprted package type {packageType}");
            }
            var expectedFilename = $"icon{expectedExtension}";
            var expectedContent = TestDataResourceUtility.GetResourceBytes(expectedFilename);

            // Act & Assert
            var packageRegistrationList = await _clients.Registration.WaitForPackageAsync(package.Id, package.FullVersion, semVer2: false, logger: _logger);
            Assert.All(packageRegistrationList, x => Assert.Equal(expectedPath.AbsoluteUri, x.CatalogEntry.IconUrl));

            await _clients.FlatContainer.WaitForPackageAsync(package.Id, package.NormalizedVersion, _logger);
            var iconContents = await _clients.FlatContainer.TryAndGetFileBinaryContent(package.Id, package.NormalizedVersion, FlatContainerContentType.Icon, _logger);
            Assert.All(iconContents, x => Assert.Equal(expectedContent, x));

            await _clients.V2V3Search.WaitForPackageAsync(package.Id, package.FullVersion, _logger);
            var searchServices = await _clients.V2V3Search.GetSearchServicesAsync(_logger);

            foreach (var searchService in searchServices)
            {
                var results = await _clients.V2V3Search.QueryAsync(
                    searchService,
                    $"q=packageid:{package.Id}",
                    _logger);

                var data = Assert.Single(results.Data);
                Assert.Equal(expectedPath.AbsoluteUri, data.IconUrl);
                Assert.Equal(package.NormalizedVersion, data.Version);
                var version = Assert.Single(data.Versions);
                Assert.Equal(package.NormalizedVersion, version.Version);
            }
        }
    }
}
