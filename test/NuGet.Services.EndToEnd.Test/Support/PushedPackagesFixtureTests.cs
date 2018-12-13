// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class PushedPackagesFixtureTests
    {
        private readonly Mock<IGalleryClient> _galleryClient;
        private readonly PushedPackagesFixture _fixture;
        private readonly ITestOutputHelper _logger;

        public PushedPackagesFixtureTests()
        {
            _galleryClient = new Mock<IGalleryClient>();
            _fixture = new PushedPackagesFixture(_galleryClient.Object);
            _logger = new Mock<ITestOutputHelper>().Object;
        }

        [Theory]
        [InlineData(PackageType.SemVer1Stable, "1.0.0", "1.0.0")]
        [InlineData(PackageType.SemVer1StableUnlisted, "1.0.0", "1.0.0")]
        [InlineData(PackageType.SemVer2Prerel, "1.0.0-alpha.1", "1.0.0-alpha.1")]
        [InlineData(PackageType.SemVer2PrerelRelisted, "1.0.0-alpha.1", "1.0.0-alpha.1")]
        [InlineData(PackageType.SemVer2PrerelUnlisted, "1.0.0-alpha.1", "1.0.0-alpha.1")]
        [InlineData(PackageType.SemVer2StableMetadata, "1.0.0", "1.0.0+metadata")]
        [InlineData(PackageType.SemVer2StableMetadataUnlisted, "1.0.0", "1.0.0+metadata")]
        public async Task ProducesExpectedPackage(PackageType packageType, string normalizedVersion, string fullVersion)
        {
            // Arrange
            var idPrefix = $"E2E.{packageType}.";

            // Act
            var package = await _fixture.PrepareAsync(packageType, _logger);

            // Assert
            Assert.StartsWith(idPrefix, package.Id);
            Assert.Equal(normalizedVersion, package.NormalizedVersion);
            Assert.Equal(fullVersion, package.FullVersion);
        }

        [Fact]
        public async Task PushesAllPackagesOnFirstPush()
        {
            // Act
            await _fixture.PrepareAsync(PackageType.SemVer1Stable, _logger);

            // Assert - The signed package will only be pushed if a path was provided.
            var expectedPackageTypes = Enum.GetNames(typeof(PackageType));
            var expectedPushes = expectedPackageTypes.Count();

            if (string.IsNullOrEmpty(EnvironmentSettings.SignedPackagePath))
            {
                expectedPushes -= 1;
            }

            // If Dotnet env not available symbols package push cannot happen
            if (string.IsNullOrEmpty(EnvironmentSettings.DotnetCliDirectory))
            {
                expectedPushes -= 1;
            }
            else if (expectedPackageTypes.Contains("SymbolsPackage"))
            {
                var exists = File.Exists(Path.Combine(EnvironmentSettings.DotnetCliDirectory, "dotnet.exe"));
                var previous = File.Exists(Path.Combine(Path.GetDirectoryName(EnvironmentSettings.DotnetCliDirectory), "dotnet.exe"));
                _logger.WriteLine($"Something is wrong: {EnvironmentSettings.DotnetCliDirectory} contains dotnet.exe: {exists}");
                // The SymbolsPackage type will invoke push twice for nupkg and snupkg each.
                expectedPushes += 1;
            }

            _galleryClient.Verify(
                x => x.PushAsync(It.IsAny<Stream>(), _logger, It.IsAny<bool>()),
                Times.Exactly(expectedPushes));
        }

        [Fact]
        public async Task CachesPushedPackage()
        {
            // Act
            var packageA = await _fixture.PrepareAsync(PackageType.SemVer1Stable, _logger);
            _galleryClient.Reset();
            var packageB = await _fixture.PrepareAsync(PackageType.SemVer1Stable, _logger);

            // Assert
            Assert.Same(packageA, packageB);
            _galleryClient.Verify(
                x => x.PushAsync(It.IsAny<Stream>(), _logger, It.IsAny<bool>()),
                Times.Never); // "never" because we reset the mock
        }

        [Fact]
        public async Task CanPushUnnamedPackageType()
        {
            // Arrange
            await _fixture.PrepareAsync(PackageType.SemVer1Stable, _logger);
            _galleryClient.Reset();

            // Act
            var package = await _fixture.PrepareAsync((PackageType) 999, _logger);

            // Assert
            Assert.NotNull(package);
            Assert.StartsWith("E2E.999.", package.Id);
            Assert.Equal("1.0.0", package.NormalizedVersion);
            Assert.Equal("1.0.0", package.FullVersion);
            _galleryClient.Verify(
                x => x.PushAsync(It.IsAny<Stream>(), _logger, false),
                Times.Once);
        }
    }
}
