﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class PushedPackagesFixtureTest
    {
        private readonly Mock<IGalleryClient> _galleryClient;
        private readonly TestSettings _testSettings;
        private readonly PushedPackagesFixture _fixture;
        private readonly Mock<ITestOutputHelper> _logger;

        public PushedPackagesFixtureTest()
        {
            _galleryClient = new Mock<IGalleryClient>();
            _testSettings = new TestSettings(
                galleryBaseUrl: "https://example-gallery",
                v3IndexUrl: "https://example-v3/index.json",
                trustedHttpsCertificates: new List<string>(),
                apiKey: "API_KEY",
                searchBaseUrl: null,
                semVer2Enabled: true,
                searchInstanceCount: 2);
            _fixture = new PushedPackagesFixture(_galleryClient.Object, _testSettings);
            _logger = new Mock<ITestOutputHelper>();
        }

        [Theory]
        [InlineData(PackageType.SemVer1Stable, "E2E." + nameof(PackageType.SemVer1Stable) + ".", "1.0.0")]
        [InlineData(PackageType.SemVer2Prerelease, "E2E." + nameof(PackageType.SemVer2Prerelease) + ".", "1.0.0-alpha.1")]
        public async Task ProducesExpectedPackage(PackageType packageType, string idPrefix, string version)
        {
            // Act
            var package = await _fixture.PushAsync(packageType, _logger.Object);

            // Assert
            Assert.StartsWith(idPrefix, package.Id);
            Assert.Equal(version, package.Version);
        }

        [Fact]
        public async Task PushesAllPackagesOnFirstPush()
        {
            // Act
            await _fixture.PushAsync(PackageType.SemVer1Stable, _logger.Object);

            // Assert
            _galleryClient.Verify(
                x => x.PushAsync(It.IsAny<Stream>()),
                Times.Exactly(Enum.GetNames(typeof(PackageType)).Count()));
        }

        [Fact]
        public async Task CachesPushedPackage()
        {
            // Act
            var packageA = await _fixture.PushAsync(PackageType.SemVer1Stable, _logger.Object);
            _galleryClient.Reset();
            var packageB = await _fixture.PushAsync(PackageType.SemVer1Stable, _logger.Object);

            // Assert
            Assert.Same(packageA, packageB);
            _galleryClient.Verify(
                x => x.PushAsync(It.IsAny<Stream>()),
                Times.Never); // "never" because we reset the mock
        }

        [Fact]
        public async Task CanPushUnnamedPackageType()
        {
            // Arrange
            await _fixture.PushAsync(PackageType.SemVer1Stable, _logger.Object);
            _galleryClient.Reset();

            // Act
            var package = await _fixture.PushAsync((PackageType) 999, _logger.Object);

            // Assert
            Assert.NotNull(package);
            Assert.StartsWith("E2E.999.", package.Id);
            Assert.Equal("1.0.0", package.Version);
            _galleryClient.Verify(
                x => x.PushAsync(It.IsAny<Stream>()),
                Times.Once);
        }
    }
}
