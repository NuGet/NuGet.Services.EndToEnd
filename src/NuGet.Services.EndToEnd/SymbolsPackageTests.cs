// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    /// <summary>
    /// Tests that integrate with nuget.exe, since this exercises our primary client code.
    /// </summary>
    [Collection(nameof(PushedPackagesCollection))]
    public class SymbolsPackageTests : IClassFixture<TrustedHttpsCertificatesFixture>, IDisposable
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
        public void VerifySymbolsCanbePublished()
        {
            // Arrange
            
        }
    }
}
