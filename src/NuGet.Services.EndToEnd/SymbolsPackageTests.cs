// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    /// <summary>
    /// Tests that verify the symbols package related flows
    /// </summary>
    [Collection(nameof(PushedPackagesCollection))]
    public class SymbolsPackageTests : IDisposable
    {
        private readonly PushedPackagesFixture _pushedPackages;
        private readonly TestSettings _testSettings;
        private readonly Clients _clients;
        private readonly ITestOutputHelper _logger;
        private readonly TestDirectory _testDirectory;
        private readonly string _outputDirectory;

        public SymbolsPackageTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {
            _pushedPackages = pushedPackages;
            _testSettings = pushedPackages.TestSettings;
            _clients = pushedPackages.Clients;
            _logger = logger;
            _testDirectory = TestDirectory.Create();
            _outputDirectory = Path.Combine(_testDirectory, "output");
            Directory.CreateDirectory(_outputDirectory);
        }

        public void Dispose()
        {
            _testDirectory.Dispose();
        }

        [Fact]
        public async Task VerifySymbolsCanbePublishedAsync()
        {
            // Arrange
            var symbolsPackage = await _pushedPackages.PrepareAsync(PackageType.SymbolsPackage, _logger);

            // Act & assert
            AssertPackageIsSymbolsPackage(symbolsPackage);
            await _clients.SymbolServerClient.VerifySymbolFilesAvailable(
                symbolsPackage.Id,
                symbolsPackage.NormalizedVersion,
                symbolsPackage.Properties.IndexedFiles,
                _logger);
        }

        private void AssertPackageIsSymbolsPackage(Package package)
        {
            // The package should have indexed files if it's a symbols package
            Assert.NotNull(package);
            Assert.Equal(PackageType.SymbolsPackage, package.Properties.Type);
            Assert.NotNull(package.Properties.IndexedFiles);
            Assert.True(package.Properties.IndexedFiles.Count() > 0);
        }
    }
}
