// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Services.EndToEnd.Support.Utilities;
using NuGet.Versioning;
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
        private readonly IDictionary<PackageType, List<Package>> _packages = new Dictionary<PackageType, List<Package>>();
        private readonly object _packageIdsLock = new object();
        private readonly IDictionary<PackageType, string> _packageIds = new Dictionary<PackageType, string>();
        private IGalleryClient _galleryClient;

        private static readonly string SymbolsProjectTemplateFolder = Path.Combine(Environment.CurrentDirectory, "Support", "TestData", "E2E.TestPortableSymbols");

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
                return pushedPackage.Last();
            }

            return (await PreparePackagesAsync(requestedPackageType, logger)).Last();
        }

        private async Task<List<Package>> PreparePackagesAsync(PackageType requestedPackageType, ITestOutputHelper logger)
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
                var pushTasks = new Dictionary<PackageType, Task<List<Package>>>();
                lock (_packagesLock)
                {
                    foreach (var packageType in packageTypes)
                    {
                        if (!_packages.TryGetValue(packageType, out List<Package> package))
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

        private List<Package> GetCachedPackage(PackageType requestedPackageType, ITestOutputHelper logger)
        {
            lock (_packagesLock)
            {
                if (_packages.TryGetValue(requestedPackageType, out List<Package> package))
                {
                    logger.WriteLine($"Package of type {requestedPackageType} has already been pushed. Using {package}.");
                    return package;
                }
            }

            return null;
        }

        private async Task<List<Package>> UncachedPrepareAsync(PackageType requestedPackageType, PackageType packageType, ITestOutputHelper logger)
        {
            try
            {
                var packagesToPrepare = await InitializePackageAsync(packageType, logger);
                foreach (var packageToPrepare in packagesToPrepare)
                {
                    logger.WriteLine($"Package of type {packageType} has not been pushed yet. Pushing {packageToPrepare}.");
                    try
                    {
                        using (var nupkgStream = new MemoryStream(packageToPrepare.Package.NupkgBytes.ToArray()))
                        {
                            await _galleryClient.PushAsync(nupkgStream, logger, isSymbolsPackage: packageToPrepare.Package.Properties.IsSymbolsPackage);
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
                        logger.WriteLine($"The package {packageToPrepare} failed to be pushed.");
                        throw ex;
                    }
                }

                // Maintain the order of the `Package` from the list so that
                // the last pushed package represents the apropriate package for
                // the pushed package type.
                var listOfPackagesToPush = packagesToPrepare
                    .Select(x => x.Package)
                    .ToList();

                lock (_packagesLock)
                {
                    _packages[packageType] = listOfPackagesToPush;
                }

                return listOfPackagesToPush;
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
                        $"The package initialization failed. Since this package was not explicitly requested " +
                        $"by the running test, this failure will be ignored. Exception:{Environment.NewLine}{ex}");
                    return null;
                }
            }
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

            // Only return the signed package type if the environment provided a path to a signed package.
            if (string.IsNullOrEmpty(EnvironmentSettings.SignedPackagePath))
            {
                selectedPackageTypes.RemoveAll(p => p == PackageType.Signed);
            }

            return selectedPackageTypes
                .Distinct()
                .OrderBy(x => x);
        }

        /// <summary>
        /// Return the list in the ordered form for which we want to run the task
        /// synchronously
        /// </summary>
        /// <returns>Ordered list of tasks to run for <see cref="PackageToPrepare"/></returns>
        private async Task<List<PackageToPrepare>> InitializePackageAsync(PackageType packageType, ITestOutputHelper logger)
        {
            var id = GetPackageId(packageType);
            PackageToPrepare packageToPrepare;
            List<PackageToPrepare> packagesToPrepare = new List<PackageToPrepare>();
            switch (packageType)
            {
                case PackageType.SemVer2DueToSemVer2Dep:
                    packageToPrepare = new PackageToPrepare(Package.Create(new PackageCreationContext
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
                    break;

                case PackageType.SemVer2StableMetadataUnlisted:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(id, "1.0.0", "1.0.0+metadata"),
                        unlist: true);
                    break;

                case PackageType.SemVer2StableMetadata:
                    packageToPrepare = new PackageToPrepare(Package.Create(id, "1.0.0", "1.0.0+metadata"));
                    break;

                case PackageType.SemVer2PrerelUnlisted:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(id, "1.0.0-alpha.1"),
                        unlist: true);
                    break;

                case PackageType.SemVer2Prerel:
                    packageToPrepare = new PackageToPrepare(Package.Create(id, SemVer2PrerelVersion));
                    break;

                case PackageType.SemVer2PrerelRelisted:
                    packageToPrepare = new PackageToPrepare(Package.Create(id, "1.0.0-alpha.1"),
                        unlist: true);
                    break;

                case PackageType.SemVer1StableUnlisted:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(id, "1.0.0"),
                        unlist: true);
                    break;

                case PackageType.Signed:
                    packageToPrepare = new PackageToPrepare(Package.SignedPackage());
                    break;

                case PackageType.SymbolsPackage:
                    return await PrepareSymbolsPackageAsync(id, "1.0.0", logger);

                case PackageType.SemVer1Stable:
                case PackageType.FullValidation:
                default:
                    packageToPrepare = new PackageToPrepare(Package.Create(id, "1.0.0"));
                    break;

            }

            packagesToPrepare.Add(packageToPrepare);
            return packagesToPrepare;
        }

        private async Task<List<PackageToPrepare>> PrepareSymbolsPackageAsync(string id, string version, ITestOutputHelper logger)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                // Copy the symbols project from template, also set the ID, Version appropriately in the properties.
                var projectPath = CopySymbolsProjectFromTemplate(id, version, testDirectory.FullPath);

                // Build the symbols project with the DotNet.exe
                var buildCommandResult = await DotNetExeClient.BuildProject(projectPath, logger);
                if (!string.IsNullOrEmpty(buildCommandResult.Error))
                {
                    throw new Exception($"Error building symbols package! {buildCommandResult.Error}");
                }

                // Build nupkg and snupkg from appropriate files
                var buildOutput = Path.Combine(testDirectory.FullPath, "bin", "Debug");
                var dllFiles = Directory.GetFiles(buildOutput, "*.dll");
                var pdbFiles = Directory.GetFiles(buildOutput, "*.pdb");
                if (dllFiles.Count() == 0 || pdbFiles.Count() == 0)
                {
                    throw new Exception($"Symbols project build failed to create DLL or PDBs");
                }

                var nupkgPackage = Package.Create(new PackageCreationContext()
                {
                    Id = id,
                    NormalizedVersion = version,
                    FullVersion = version
                }, dllFiles);

                var pdbIndexes = new HashSet<string>();
                foreach (var file in pdbFiles)
                {
                    pdbIndexes.Add(PortableMetadataReader.GetIndex(file));
                }

                var snupkgPackage = Package.Create(new PackageCreationContext()
                {
                    Id = id,
                    NormalizedVersion = version,
                    FullVersion = version
                }, pdbFiles, new PackageProperties(isSymbolsPackage: true, indexedFiles: pdbIndexes));

                return new List<PackageToPrepare>() { new PackageToPrepare(nupkgPackage), new PackageToPrepare(snupkgPackage) };
            }
        }

        private static string CopySymbolsProjectFromTemplate(string id, string version, string tempProjectFolder)
        {
            var sourceDirectoryFiles = Directory.GetFiles(SymbolsProjectTemplateFolder,"*.*", SearchOption.AllDirectories);
            string projectPath = string.Empty;
            foreach (string sourceFile in sourceDirectoryFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                if (string.Equals(fileName, "AssemblyInfo.cs"))
                {
                    var propertiesContent = File.ReadAllText(sourceFile);
                    var formattedContent = string.Format(propertiesContent, id, version);
                    var destinationFile = Path.Combine(tempProjectFolder, "Properties", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    File.WriteAllText(destinationFile, formattedContent);
                }
                else
                {
                    var destinationFile = Path.Combine(tempProjectFolder, fileName);
                    File.Copy(sourceFile, destinationFile, true);
                    if (Path.GetExtension(fileName) == ".csproj")
                    {
                        projectPath = destinationFile;
                    }
                }
            }

            return projectPath;
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
