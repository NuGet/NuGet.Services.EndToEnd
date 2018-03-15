// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A test fixture for sharing pushed packages between tests. Since packages take quite a while (minutes) to make
    /// their way through the V3 pipeline, packages should be pushed all at once and shared between tests, if possible.
    /// </summary>
    public class PushedPackagesFixture : CommonFixture
    {
        private const string SemVer2PrerelVersion = "1.0.0-alpha.1";

        private static readonly HashSet<PackageType> SemVer2PackageTypes = new HashSet<PackageType>
        {
            PackageType.SemVer2Prerel,
            PackageType.SemVer2PrerelRelisted,
            PackageType.SemVer2PrerelUnlisted,
            PackageType.SemVer2StableMetadata,
            PackageType.SemVer2StableMetadataUnlisted,
            PackageType.SemVer2DueToSemVer2Dep,
        };

        private readonly SemaphoreSlim _pushLock = new SemaphoreSlim(initialCount: 1);
        private readonly object _packagesLock = new object();
        private readonly IDictionary<PackageType, Package> _packages = new Dictionary<PackageType, Package>();
        private readonly object _packageIdsLock = new object();
        private readonly IDictionary<PackageType, string> _packageIds = new Dictionary<PackageType, string>();
        private IGalleryClient _galleryClient;

        public PushedPackagesFixture()
        {
        }

        public PushedPackagesFixture(IGalleryClient galleryClient)
        {
            _galleryClient = galleryClient;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (_galleryClient == null)
            {
                _galleryClient = Clients.Gallery;
            }
        }

        public override Task DisposeAsync()
        {
            return base.DisposeAsync();
        }

        public async Task<Package> PrepareAsync(PackageType requestedPackageType, ITestOutputHelper logger)
        {
            var pushedPackage = GetCachedPackage(requestedPackageType, logger);
            if (pushedPackage != null)
            {
                return pushedPackage;
            }

            return await PreparePackagesAsync(requestedPackageType, logger);
        }

        private async Task<Package> PreparePackagesAsync(PackageType requestedPackageType, ITestOutputHelper logger)
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
                var pushedPackage = GetCachedPackage(requestedPackageType, logger);
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
                            pushTasks.Add(packageType, UncachedPrepareAsync(requestedPackageType, packageType, logger));
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

        private Package GetCachedPackage(PackageType requestedPackageType, ITestOutputHelper logger)
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

        private async Task<Package> UncachedPrepareAsync(PackageType requestedPackageType, PackageType packageType, ITestOutputHelper logger)
        {
            await Task.Yield();

            var packageToPrepare = InitializePackage(packageType);
            logger.WriteLine($"Package of type {packageType} has not been pushed yet. Pushing {packageToPrepare}.");
            try
            {
                using (var nupkgStream = new MemoryStream(packageToPrepare.Package.NupkgBytes.ToArray()))
                {
                    await _galleryClient.PushAsync(nupkgStream, logger);
                }
                logger.WriteLine($"Package {packageToPrepare} has been pushed.");

                if (packageToPrepare.Unlist)
                {
                    logger.WriteLine($"Package of type {packageType} need to be unlisted. Unlisting {packageToPrepare}.");
                    await _galleryClient.UnlistAsync(packageToPrepare.Package.Id, packageToPrepare.Package.NormalizedVersion, logger);
                    logger.WriteLine($"Package {packageToPrepare} has been unlisted.");
                }
            }
            catch (Exception ex)
            {
                if (packageType == requestedPackageType || ex is TaskCanceledException)
                {
                    throw;
                }
                else
                {
                    logger.WriteLine(
                        $"The package {packageToPrepare} failed to be pushed. Since this package was not explicitly requested " +
                        $"by the running test, this failure will be ignored. Exception:{Environment.NewLine}{ex}");
                    return null;
                }
            }
            
            lock (_packagesLock)
            {
                _packages[packageType] = packageToPrepare.Package;
            }

            return packageToPrepare.Package;
        }

        public void Dispose()
        {
        }

        private IEnumerable<PackageType> GetPackageTypes(PackageType requestedPackageType)
        {
            var selectedPackageTypes = new List<PackageType>();

            // Add all package types, supporting aggressive push.
            if (TestSettings.AggressivePush)
            {
                selectedPackageTypes.AddRange(Enum
                    .GetValues(typeof(PackageType))
                    .Cast<PackageType>());
            }

            // Add the requested package type.
            selectedPackageTypes.Add(requestedPackageType);

            return selectedPackageTypes
                .Distinct()
                .OrderBy(x => x);
        }

        private PackageToPrepare InitializePackage(PackageType packageType)
        {
            var id = GetPackageId(packageType);

            switch (packageType)
            {
                case PackageType.SemVer2DueToSemVer2Dep:
                    return new PackageToPrepare(Package.Create(new PackageCreationContext
                    {
                        Id = id,
                        NormalizedVersion = "1.0.0-beta",
                        FullVersion = "1.0.0-beta",
                        DependencyGroups = new[]
                        {
                            new PackageDependencyGroup(
                                TestData.TargetFramework,
                                new[]
                                {
                                    new PackageDependency(
                                        GetPackageId(PackageType.SemVer2Prerel),
                                        VersionRange.Parse(SemVer2PrerelVersion))
                                })
                        }
                    }));

                case PackageType.SemVer2StableMetadataUnlisted:
                    return new PackageToPrepare(
                        Package.Create(id, "1.0.0", "1.0.0+metadata"),
                        unlist: true);

                case PackageType.SemVer2StableMetadata:
                    return new PackageToPrepare(Package.Create(id, "1.0.0", "1.0.0+metadata"));

                case PackageType.SemVer2PrerelUnlisted:
                    return new PackageToPrepare(
                        Package.Create(id, "1.0.0-alpha.1"),
                        unlist: true);

                case PackageType.SemVer2Prerel:
                    return new PackageToPrepare(Package.Create(id, SemVer2PrerelVersion));

                case PackageType.SemVer2PrerelRelisted:
                    return new PackageToPrepare(Package.Create(id, "1.0.0-alpha.1"),
                        unlist: true);

                case PackageType.SemVer1StableUnlisted:
                    return new PackageToPrepare(
                        Package.Create(id, "1.0.0"),
                        unlist: true);

                case PackageType.SemVer1Stable:
                case PackageType.FullValidation:
                default:
                    return new PackageToPrepare(Package.Create(id, "1.0.0"));
            }
        }

        private string GetPackageId(PackageType packageType)
        {
            lock (_packageIdsLock)
            {
                string id;
                if (!_packageIds.TryGetValue(packageType, out id))
                {
                    var timestamp = DateTimeOffset.UtcNow.ToString("yyMMdd.HHmmss.fffffff");
                    id = $"E2E.{packageType.ToString()}.{timestamp}";
                    _packageIds[packageType] = id;
                }

                return id;
            }
        }

        private static string GenerateUniqueId(PackageType packageType)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyMMdd.HHmmss.fffffff");
            var id = $"E2E.{packageType.ToString()}.{timestamp}";
            return id;
        }

        private class PackageToPrepare
        {
            public PackageToPrepare(Package package) : this(package, unlist: false)
            {
            }

            public PackageToPrepare(Package package, bool unlist)
            {
                Package = package;
                Unlist = unlist;
            }
            
            public Package Package { get; }
            public bool Unlist { get; }

            public override string ToString()
            {
                return Package.ToString();
            }
        }
    }
}
