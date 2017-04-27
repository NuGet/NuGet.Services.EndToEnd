// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A test fixture for sharing pushed packages between tests. Since packages take quite a while (minutes) to make
    /// their way through the V3 pipeline, packages should be pushed all at once and shared between tests, if possible.
    /// </summary>
    public class PushedPackagesFixture : IDisposable
    {
        private static readonly HashSet<PackageType> SemVer2PackageTypes = new HashSet<PackageType>
        {
            PackageType.SemVer2Prerelease,
        };

        private readonly SemaphoreSlim _pushLock;
        private readonly object _packagesLock = new object();
        private readonly IDictionary<PackageType, Package> _packages;
        private readonly IGalleryClient _galleryClient;
        private readonly TestSettings _testSettings;

        public PushedPackagesFixture() : this(Clients.Initialize().Gallery, TestSettings.Create())
        {
        }

        public PushedPackagesFixture(IGalleryClient galleryClient, TestSettings testSettings)
        {
            _galleryClient = galleryClient;
            _testSettings = testSettings;
            _pushLock = new SemaphoreSlim(initialCount: 1);
            _packages = new Dictionary<PackageType, Package>();
        }

        public async Task<Package> PushAsync(PackageType requestedPackageType, ITestOutputHelper logger)
        {
            var pushedPackage = GetPushedPackage(requestedPackageType, logger);
            if (pushedPackage != null)
            {
                return pushedPackage;
            }

            return await PushUnpushedPackagesAsync(requestedPackageType, logger);
        }

        private async Task<Package> PushUnpushedPackagesAsync(PackageType requestedPackageType, ITestOutputHelper logger)
        {
            var acquired = await _pushLock.WaitAsync(0);
            try
            {
                if (!acquired)
                {
                    logger.WriteLine("Another test is already pushing the packages. Waiting.");
                    await _pushLock.WaitAsync();
                    acquired = true;
                }

                // Has the package already been pushed?
                var pushedPackage = GetPushedPackage(requestedPackageType, logger);
                if (pushedPackage != null)
                {
                    return pushedPackage;
                }

                var packageTypes = GetPackageTypes(requestedPackageType);

                // Push all of the package types that have not been pushed yet.
                var pushTasks = new Dictionary<PackageType, Task<Package>>();
                lock (_packagesLock)
                {
                    foreach (var packageType in packageTypes)
                    {
                        if (!_packages.TryGetValue(packageType, out Package package))
                        {
                            pushTasks.Add(packageType, UncachedPushAsync(requestedPackageType, packageType, logger));
                        }
                    }
                }

                await Task.WhenAll(pushTasks.Values);
                
                // Use the package that we just pushed.
                return _packages[requestedPackageType];
            }
            finally
            {
                if (acquired)
                {
                    _pushLock.Release();
                }
            }
        }

        private Package GetPushedPackage(PackageType requestedPackageType, ITestOutputHelper logger)
        {
            lock (_packagesLock)
            {
                if (_packages.TryGetValue(requestedPackageType, out Package package))
                {
                    logger.WriteLine($"Package of type {requestedPackageType} has already been pushed. Using {package}.");
                    return package;
                }
            }

            return null;
        }

        private async Task<Package> UncachedPushAsync(PackageType requestedPackageType, PackageType packageType, ITestOutputHelper logger)
        {
            await Task.Yield();

            Package package = CreatePackage(packageType);
            logger.WriteLine($"Package of type {packageType} has not been pushed yet. Pushing {package}.");
            try
            {
                using (var nupkgStream = new MemoryStream(package.NupkgBytes.ToArray()))
                {
                    await _galleryClient.PushAsync(nupkgStream);
                }
                logger.WriteLine($"Package {package} has been pushed.");
            }
            catch (Exception ex)
            {
                if (packageType == requestedPackageType)
                {
                    throw;
                }
                else
                {
                    logger.WriteLine(
                        $"The package {package} failed to be pushed. Since this package was not explicitly requested " +
                        $"by the running test, this failure will be ignored. Exception:{Environment.NewLine}{ex}");
                    return null;
                }
            }
            
            lock (_packagesLock)
            {
                _packages[packageType] = package;
            }

            return package;
        }

        public void Dispose()
        {
        }

        private IEnumerable<PackageType> GetPackageTypes(PackageType requestedPackageType)
        {
            var selectedPackageTypes = Enum
                .GetValues(typeof(PackageType))
                .Cast<PackageType>();

            // If SemVer 2.0.0 is not enabled, don't automatically push SemVer 2.0.0 packages.
            if (!_testSettings.SemVer2Enabled)
            {
                selectedPackageTypes = selectedPackageTypes
                    .Except(SemVer2PackageTypes);
            }

            selectedPackageTypes = selectedPackageTypes
                .Concat(new[] { requestedPackageType })
                .Distinct()
                .OrderBy(x => x);

            return selectedPackageTypes;
        }

        private Package CreatePackage(PackageType packageType)
        {
            switch (packageType)
            {
                case PackageType.SemVer2Prerelease:
                    return Package.Create(packageType.ToString(), "1.0.0-alpha.1");
                case PackageType.SemVer1Stable:
                default:
                    return Package.Create(packageType.ToString(), "1.0.0");
            }
        }
    }
}
