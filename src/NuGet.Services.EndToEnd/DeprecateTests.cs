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
    public class DeprecateTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        private const string AlternateId = "BaseTestPackage";
        private const string AlternateVersion = "1.0.0";

        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public DeprecateTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Theory]
        [InlineData(PackageType.DeprecatedSingleReason, true)]
        [InlineData(PackageType.DeprecatedAlternateRegistration, true, true, true, AlternateId)]
        [InlineData(PackageType.DeprecatedAlternateVersion, true, true, true, AlternateId, AlternateVersion)]
        public async Task DeprecateAndUndeprecatePackage(
            PackageType packageType, 
            bool isLegacy = false, 
            bool hasCriticalBugs = false, 
            bool isOther = false, 
            string alternateId = null, 
            string alternateVersion = null)
        {
            // Verify package exists
            var package = await _pushedPackages.PrepareAsync(packageType, _logger);
            await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, deprecation: null, logger: _logger);

            // Deprecate package and verify
            var deprecation = new PackageDeprecationContext
            {
                IsLegacy = isLegacy,
                HasCriticalBugs = hasCriticalBugs,
                IsOther = isOther,
                Message = "This is an end-to-end test!",
                AlternatePackageId = alternateId,
                AlternatePackageVersion = alternateVersion
            };

            await _clients.Gallery.DeprecateAsync(
                package.Id, 
                new[] { package.NormalizedVersion },
                deprecation,
                _logger);

            await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, deprecation, _logger);

            // Undeprecate package and verify
            await _clients.Gallery.DeprecateAsync(
                package.Id,
                new[] { package.NormalizedVersion },
                context: null,
                logger: _logger);

            await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, deprecation: null, logger: _logger);
        }

        [Fact]
        public async Task DeprecateAndUndeprecateMultiplePackages()
        {
            // Verify packages exist
            var packages = await Task.WhenAll(
                new[] { PackageType.DeprecatedMultiple1, PackageType.DeprecatedMultiple2 }
                    .Select(t => _pushedPackages.PrepareAsync(t, _logger)));

            await Task.WhenAll(
                packages
                    .Select(p => _clients.Registration.WaitForDeprecationStateAsync(
                        p.Id, p.FullVersion, deprecation: null, logger: _logger)));

            // Deprecate packages and verify
            var deprecation = new PackageDeprecationContext
            {
                IsLegacy = true,
                HasCriticalBugs = true,
                IsOther = true,
                Message = "This is an end-to-end test!",
                AlternatePackageId = AlternateId,
                AlternatePackageVersion = AlternateVersion
            };

            await _clients.Gallery.DeprecateAsync(
                packages.First().Id,
                packages.Select(p => p.NormalizedVersion).ToList(),
                deprecation,
                _logger);

            await Task.WhenAll(
                packages
                    .Select(p => _clients.Registration.WaitForDeprecationStateAsync(
                        p.Id, p.FullVersion, deprecation, _logger)));

            // Undeprecate packages and verify
            await _clients.Gallery.DeprecateAsync(
                packages.First().Id,
                packages.Select(p => p.NormalizedVersion).ToList(),
                context: null,
                logger: _logger);

            await Task.WhenAll(
                packages
                    .Select(p => _clients.Registration.WaitForDeprecationStateAsync(
                        p.Id, p.FullVersion, deprecation: null, logger: _logger)));
        }
    }
}
