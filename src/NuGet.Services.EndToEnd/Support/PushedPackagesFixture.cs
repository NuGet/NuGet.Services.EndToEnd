// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Services.EndToEnd.Support.Utilities;
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

        private readonly SemaphoreSlim _pushLock = new SemaphoreSlim(initialCount: 1);
        private readonly object _packagesLock = new object();

        // Mapping of the PackageType to the list of applicable packages that are pushed to gallery.
        private readonly IDictionary<PackageType, IReadOnlyList<Package>> _packages = new Dictionary<PackageType, IReadOnlyList<Package>>();

        private readonly object _packageIdsLock = new object();
        private readonly IDictionary<PackageType, string> _packageIds = new Dictionary<PackageType, string>();
        private IGalleryClient _galleryClient;

        private static readonly string SymbolsProjectTemplateFolder = Path.Combine(Environment.CurrentDirectory, "Support", "TestData", "E2E.TestPortableSymbols");
        private static readonly string DotnetToolProjectTemplateFolder = Path.Combine(Environment.CurrentDirectory, "Support", "TestData", "E2E.DotnetTool");

        public PushedPackagesFixture()
        {
        }

        private PushedPackagesFixture(IGalleryClient galleryClient)
        {
            _galleryClient = galleryClient;
        }

        public static PushedPackagesFixture Create(IGalleryClient galleryClient)
        {
            return new PushedPackagesFixture(galleryClient);
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
            var pushedPackages = GetCachedPackages(requestedPackageType, logger);
            if (pushedPackages != null)
            {
                return pushedPackages.Last();
            }

            return (await PreparePackagesAsync(requestedPackageType, logger)).Last();
        }

        /// <summary>
        /// This method will generate and cache the list of packages, either for all <see cref="PackageType"/>s
        /// or for the requested package type.
        /// </summary>
        /// <param name="requestedPackageType">The package type being requested to be pushed.</param>
        /// <param name="logger">Test logger</param>
        /// <returns><see cref="Task"/> of lists of <see cref="Package"/> that are generated or cached which were pushed to gallery.</returns>
        private async Task<IReadOnlyList<Package>> PreparePackagesAsync(PackageType requestedPackageType, ITestOutputHelper logger)
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
                var pushedPackage = GetCachedPackages(requestedPackageType, logger);
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
                        if (!_packages.TryGetValue(packageType, out IReadOnlyList<Package> package))
                        {
                            pushTasks.Add(packageType, UncachedPrepareAsync(requestedPackageType, packageType, logger));
                        }
                    }
                }

                await Task.WhenAll(pushTasks.Values);

                // Use the packages that we just pushed.
                lock (_packagesLock)
                {
                    return _packages[requestedPackageType];
                }
            }
            finally
            {
                if (acquired)
                {
                    _pushLock.Release();
                }
            }
        }

        /// <summary>
        /// Gets the list of cached packages for a given <see cref="PackageType"/>
        /// </summary>
        private IReadOnlyList<Package> GetCachedPackages(PackageType requestedPackageType, ITestOutputHelper logger)
        {
            lock (_packagesLock)
            {
                if (_packages.TryGetValue(requestedPackageType, out IReadOnlyList<Package> package))
                {
                    logger.WriteLine($"Package of type {requestedPackageType} has already been pushed. Using {string.Join(", ", package)}.");
                    return package;
                }
            }

            return null;
        }

        /// <summary>
        /// This method will generate the list of applicable packages for a given <see cref="PackageType"/> and
        /// push them squentially to the NuGet gallery. Note this method will maintain the order of the packages
        /// in the list that were pushed to the gallery.
        /// </summary>
        /// <param name="requestedPackageType">The type being currently requested to be pushed to gallery.</param>
        /// <param name="packageType">The type that is going to be pushed to gallery.</param>
        /// <param name="logger">Test logger</param>
        /// <returns><see cref="Task"/> of lists of <see cref="Package"/> that are generated and pushed to gallery.</returns>
        private async Task<List<Package>> UncachedPrepareAsync(PackageType requestedPackageType, PackageType packageType, ITestOutputHelper logger)
        {
            try
            {
                var packagesToPrepare = await InitializePackagesAsync(packageType, logger);
                foreach (var packageToPrepare in packagesToPrepare)
                {
                    logger.WriteLine($"Package of type {packageType} has not been pushed yet. Pushing {packageToPrepare}.");
                    try
                    {
                        using (var nupkgStream = new MemoryStream(packageToPrepare.Package.NupkgBytes.ToArray()))
                        {
                            await _galleryClient.PushAsync(nupkgStream, logger, packageToPrepare.Package.Properties.Type);
                        }

                        logger.WriteLine($"Package {packageToPrepare} has been pushed.");

                        if (packageToPrepare.Unlist)
                        {
                            logger.WriteLine($"Package of type {packageType} needs to be unlisted. Unlisting {packageToPrepare}.");
                            await _galleryClient.UnlistAsync(packageToPrepare.Package.Id, packageToPrepare.Package.NormalizedVersion, logger);
                            logger.WriteLine($"Package {packageToPrepare} has been unlisted.");
                        }

                        if (packageToPrepare.Deprecation != null)
                        {
                            logger.WriteLine($"Package of type {packageType} needs to deprecated. Deprecating {packageToPrepare}.");
                            await _galleryClient.DeprecateAsync(
                                packageToPrepare.Package.Id, 
                                new[] { packageToPrepare.Package.NormalizedVersion }, 
                                packageToPrepare.Deprecation, 
                                logger);

                            logger.WriteLine($"Package {packageToPrepare} has been deprecated.");
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
        /// Return the list of <see cref="PackageToPrepare"/> in the ordered form
        /// for which we want to run all the tasks sequentially
        /// </summary>
        /// <returns>Ordered list of tasks to run for <see cref="PackageToPrepare"/></returns>
        private async Task<List<PackageToPrepare>> InitializePackagesAsync(PackageType packageType, ITestOutputHelper logger)
        {
            var id = GetPackageId(packageType);
            LicenseMetadata licenseMetadata;
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
                        },
                        Properties = new PackageProperties(packageType)
                    }));
                    break;

                case PackageType.SemVer2StableMetadataUnlisted:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(packageType, id, "1.0.0", "1.0.0+metadata"),
                        unlist: true);
                    break;

                case PackageType.SemVer2StableMetadata:
                    packageToPrepare = new PackageToPrepare(Package.Create(packageType, id, "1.0.0", "1.0.0+metadata"));
                    break;

                case PackageType.SemVer2PrerelUnlisted:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(packageType, id, "1.0.0-alpha.1"),
                        unlist: true);
                    break;

                case PackageType.SemVer2Prerel:
                    packageToPrepare = new PackageToPrepare(Package.Create(packageType, id, SemVer2PrerelVersion));
                    break;

                case PackageType.SemVer2PrerelRelisted:
                    packageToPrepare = new PackageToPrepare(Package.Create(packageType, id, "1.0.0-alpha.1"),
                        unlist: true); 
                    break;

                case PackageType.SemVer1StableUnlisted:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(packageType, id, "1.0.0"),
                        unlist: true);
                    break;

                case PackageType.Signed:
                    packageToPrepare = new PackageToPrepare(Package.SignedPackage());
                    break;

                case PackageType.SymbolsPackage:
                    return await PrepareSymbolsPackageAsync(id, "1.0.0", logger);

                case PackageType.LicenseExpression:
                    licenseMetadata = new LicenseMetadata(LicenseType.Expression, "MIT", null, null, LicenseMetadata.EmptyVersion);
                    packageToPrepare = new PackageToPrepare(Package.Create(new PackageCreationContext
                    {
                        Id = id,
                        NormalizedVersion = "1.0.0",
                        FullVersion = "1.0.0",
                        Properties = new PackageProperties(packageType, licenseMetadata)
                    }));
                    break;

                case PackageType.LicenseFile:
                    var licenseFilePath = "license.txt";
                    var licenseFileContent = "It's a license";
                    licenseMetadata = new LicenseMetadata(LicenseType.File, licenseFilePath, null, null, LicenseMetadata.EmptyVersion);
                    packageToPrepare = new PackageToPrepare(Package.Create(new PackageCreationContext
                    {
                        Id = id,
                        NormalizedVersion = "1.0.0",
                        FullVersion = "1.0.0",
                        Properties = new PackageProperties(packageType, licenseMetadata, licenseFileContent),
                        Files = new List<PhysicalPackageFile>{
                            new PhysicalPackageFile(new MemoryStream(Encoding.UTF8.GetBytes(licenseFileContent)))
                            {
                                TargetPath = licenseFilePath
                            } }
                    }));
                    break;

                case PackageType.LicenseUrl:
                    var licenseUrl = new Uri("https://testLicenseUrl");
                    packageToPrepare = new PackageToPrepare(Package.Create(new PackageCreationContext
                    {
                        Id = id,
                        NormalizedVersion = "1.0.0",
                        FullVersion = "1.0.0",
                        Properties = new PackageProperties(packageType, licenseUrl: licenseUrl)
                    }));
                    break;

                case PackageType.EmbeddedIconJpeg:
                    packageToPrepare = new PackageToPrepare(Package.Create(new PackageCreationContext
                    {
                        Id = id,
                        NormalizedVersion = "1.0.0",
                        FullVersion = "1.0.0",
                        Properties = new PackageProperties(packageType)
                        {
                            EmbeddedIconFilename = "icon.jpg"
                        },
                    }));
                    break;

                case PackageType.EmbeddedIconPng:
                    packageToPrepare = new PackageToPrepare(Package.Create(new PackageCreationContext
                    {
                        Id = id,
                        NormalizedVersion = "1.0.0",
                        FullVersion = "1.0.0",
                        Properties = new PackageProperties(packageType)
                        {
                            EmbeddedIconFilename = "icon.png"
                        },
                    }));
                    break;

                case PackageType.Deprecated:
                    packageToPrepare = new PackageToPrepare(
                        Package.Create(packageType, id, "1.0.0"),
                        PackageDeprecationContext.Default);
                    break;

                case PackageType.DotnetTool:
                    return await PrepareDotnetToolPackageAsync(id, "1.0.0", logger);

                case PackageType.SemVer1Stable:
                case PackageType.FullValidation:
                default:
                    packageToPrepare = new PackageToPrepare(Package.Create(packageType, id, "1.0.0"));
                    break;
            }

            packagesToPrepare.Add(packageToPrepare);
            return packagesToPrepare;
        }

        private async Task<List<PackageToPrepare>> PrepareDotnetToolPackageAsync(string id, string version, ITestOutputHelper logger)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                var projectPath = CopyDotnetToolProjectFromTemplate(id, version, testDirectory.FullPath);

                var packCommandResult = await DotNetExeClient.PackProjectAsync(id, version, projectPath, logger);
                if (!string.IsNullOrEmpty(packCommandResult.Error))
                {
                    throw new ExternalException($"Error packing the .NET tool package! Error: {packCommandResult.Error} Output: {packCommandResult.Output}");
                }

                var buildOutput = Path.Combine(testDirectory.FullPath, "bin", "Debug");
                var nupkgFiles = Directory.GetFiles(buildOutput, "*.nupkg");
                if (nupkgFiles.Count() == 0)
                {
                    throw new InvalidOperationException($".NET tool project pack failed to create a package");
                }

                return new List<PackageToPrepare>
                {
                    new PackageToPrepare(Package.Create(nupkgFiles[0], new PackageProperties(PackageType.DotnetTool))),
                };
            }
        }

        private async Task<List<PackageToPrepare>> PrepareSymbolsPackageAsync(string id, string version, ITestOutputHelper logger)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                // Copy the symbols project from template, also set the ID, Version appropriately in the properties.
                var projectPath = CopySymbolsProjectFromTemplate(id, version, testDirectory.FullPath);

                // Build the symbols project with the DotNet
                var buildCommandResult = await DotNetExeClient.BuildProjectAsync(projectPath, logger);
                if (!string.IsNullOrEmpty(buildCommandResult.Error))
                {
                    throw new ExternalException($"Error building symbols package! Error: {buildCommandResult.Error} Output: {buildCommandResult.Output}");
                }

                // Build nupkg and snupkg from appropriate files
                var buildOutput = Path.Combine(testDirectory.FullPath, "bin", "Debug");
                var dllFiles = Directory.GetFiles(buildOutput, "*.dll");
                var pdbFiles = Directory.GetFiles(buildOutput, "*.pdb");
                if (dllFiles.Count() == 0 || pdbFiles.Count() == 0)
                {
                    throw new InvalidOperationException($"Symbols project build failed to create DLL or PDBs");
                }

                var dllPhysicalPackageFiles = dllFiles
                    .Select(x => new { FileName = Path.GetFileName(x), Stream = GetMemoryStreamForFile(x) })
                    .Select(x => new PhysicalPackageFile(x.Stream)
                    {
                        TargetPath = $"lib/{x.FileName}"
                    }).ToList();

                var nupkgPackage = Package.Create(new PackageCreationContext()
                {
                    Id = id,
                    NormalizedVersion = version,
                    FullVersion = version,
                    Files = dllPhysicalPackageFiles,
                    Properties = new PackageProperties(PackageType.SemVer1Stable)
                });

                var pdbIndexes = new HashSet<string>();
                foreach (var file in pdbFiles)
                {
                    pdbIndexes.Add(PortableMetadataReader.GetIndex(file));
                }

                var pdbPhysicalPackageFiles = pdbFiles
                    .Select(x => new { FileName = Path.GetFileName(x), Stream = GetMemoryStreamForFile(x) })
                    .Select(x => new PhysicalPackageFile(x.Stream)
                    {
                        TargetPath = $"lib/{x.FileName}"
                    }).ToList();

                var snupkgPackage = Package.Create(new PackageCreationContext()
                {
                    Id = id,
                    NormalizedVersion = version,
                    FullVersion = version,
                    Files = pdbPhysicalPackageFiles,
                    Properties = new PackageProperties(PackageType.SymbolsPackage, indexedFiles: pdbIndexes)
                });

                return new List<PackageToPrepare>() { new PackageToPrepare(nupkgPackage), new PackageToPrepare(snupkgPackage) };
            }
        }

        private static string CopySymbolsProjectFromTemplate(string id, string version, string tempProjectFolder)
        {
            return CopyProjectFromTemplate(SymbolsProjectTemplateFolder, id, version, tempProjectFolder);
        }

        private static string CopyDotnetToolProjectFromTemplate(string id, string version, string tempProjectFolder)
        {
            return CopyProjectFromTemplate(DotnetToolProjectTemplateFolder, id, version, tempProjectFolder);
        }

        private static string CopyProjectFromTemplate(string templateDirectory, string id, string version, string tempProjectFolder)
        {
            var sourceFiles = Directory.GetFiles(templateDirectory, "*.*", SearchOption.AllDirectories);
            string projectPath = string.Empty;
            foreach (string sourceFile in sourceFiles)
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

        private static MemoryStream GetMemoryStreamForFile(string filename)
        {
            var memoryStream = new MemoryStream();
            using (FileStream fileStream = File.OpenRead(filename))
            {
                fileStream.CopyTo(memoryStream);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private class PackageToPrepare
        {
            public PackageToPrepare(Package package) 
                : this(package, unlist: false, deprecation: null)
            {
            }

            public PackageToPrepare(Package package, bool unlist)
                : this(package, unlist, deprecation: null)
            {
            }

            public PackageToPrepare(Package package, PackageDeprecationContext deprecation)
                : this(package, unlist: false, deprecation: deprecation)
            {
            }

            public PackageToPrepare(Package package, bool unlist, PackageDeprecationContext deprecation)
            {
                Package = package;
                Unlist = unlist;
                Deprecation = deprecation;
            }

            public Package Package { get; }
            public bool Unlist { get; }
            public PackageDeprecationContext Deprecation { get; }

            public override string ToString()
            {
                return Package.ToString();
            }
        }
    }
}
