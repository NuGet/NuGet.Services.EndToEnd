// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;
using static NuGet.Services.EndToEnd.Support.RegistrationClient;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class DeprecateTests
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;

        public DeprecateTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _clients = pushedPackages.Clients;
            _logger = logger;
        }

        [Fact]
        public async Task DeprecatePackage()
        {
            var package = await _pushedPackages.PrepareAsync(PackageType.Deprecated, _logger);

            var deprecation = await _clients.Registration.WaitForDeprecationStateAsync(
                package.Id, package.FullVersion, isDeprecated: true, logger: _logger);

            var defaultDeprecation = PackageDeprecationContext.Default;
            AssertDeprecation(defaultDeprecation, deprecation);
        }

        private void AssertDeprecation(PackageDeprecationContext expectedDeprecation, CatalogDeprecation actualDeprecation)
        {
            Assert.Equal(expectedDeprecation.IsLegacy, HasReason(actualDeprecation, "Legacy"));
            Assert.Equal(expectedDeprecation.HasCriticalBugs, HasReason(actualDeprecation, "CriticalBugs"));
            Assert.Equal(expectedDeprecation.IsOther, HasReason(actualDeprecation, "Other"));

            Assert.Equal(expectedDeprecation.Message, actualDeprecation?.Message);

            Assert.Equal(expectedDeprecation.AlternatePackageId, actualDeprecation?.AlternatePackage?.Id);
            Assert.Equal(GetExpectedAlternatePackageRange(expectedDeprecation), actualDeprecation?.AlternatePackage?.Range);
        }

        private static bool HasReason(CatalogDeprecation deprecation, string reasonName)
        {
            return deprecation?.Reasons.Contains(reasonName) ?? false;
        }

        private static string GetExpectedAlternatePackageRange(PackageDeprecationContext deprecation)
        {
            if (deprecation?.AlternatePackageId == null)
            {
                return null;
            }

            var alternatePackageVersion = deprecation.AlternatePackageVersion;
            return alternatePackageVersion == null
                ? $"*"
                : $"[{alternatePackageVersion}, )";
        }
    }
}
