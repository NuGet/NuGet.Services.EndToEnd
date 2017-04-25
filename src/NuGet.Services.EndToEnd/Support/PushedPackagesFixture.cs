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
    public class PushedPackagesFixture : IDisposable
    {
        private readonly object _packagesLock;
        private readonly IDictionary<PackageType, PushedPackage> _packages;
        private readonly Clients _clients;

        public PushedPackagesFixture()
        {
            _clients = Clients.Initialize();
            _packagesLock = new object();
            _packages = new Dictionary<PackageType, PushedPackage>();
        }
        
        public async Task<Package> PushAsync(PackageType packageType, ITestOutputHelper logger)
        {
            PushedPackage pushedPackage;
            lock (_packagesLock)
            {
                if (!_packages.TryGetValue(packageType, out pushedPackage))
                {
                    var package = CreatePackage(packageType);
                    pushedPackage = new PushedPackage(package);
                    _packages[packageType] = pushedPackage;
                }
            }

            if (pushedPackage.Pushed)
            {
                logger.WriteLine($"Package {pushedPackage} push has already been pushed.");
                return pushedPackage.Package;
            }

            var acquired = await pushedPackage.PushLock.WaitAsync(0);
            try
            {
                if (!acquired)
                {
                    logger.WriteLine($"Another test is already pushing {pushedPackage}. Waiting.");
                    await pushedPackage.PushLock.WaitAsync();
                    acquired = true;
                }

                if (pushedPackage.Pushed)
                {
                    logger.WriteLine($"Package {pushedPackage} has already been pushed.");
                    return pushedPackage.Package;
                }

                using (var packageStream = new MemoryStream(pushedPackage.Package.NupkgBytes.ToArray()))
                {
                    logger.WriteLine($"Package {pushedPackage} is about to be pushed.");
                    await _clients.Gallery.PushAsync(packageStream);
                    logger.WriteLine($"Package {pushedPackage} has been successfully pushed.");
                    pushedPackage.MarkAsPushed();
                    return pushedPackage.Package;
                }
            }
            finally
            {
                if (acquired)
                {
                    pushedPackage.PushLock.Release();
                }
            }
        }

        public void Dispose()
        {
        }

        private Package CreatePackage(PackageType packageLabel)
        {
            switch (packageLabel)
            {
                default:
                    return Package.Create(packageLabel.ToString(), "1.0.0");
            }
        }

        private class PushedPackage
        {
            public PushedPackage(Package package)
            {
                PushLock = new SemaphoreSlim(1);
                Package = package;
                Pushed = false;
            }

            public SemaphoreSlim PushLock { get; }
            public Package Package { get; }
            public bool Pushed { get; private set; }

            public override string ToString()
            {
                return Package.ToString();
            }

            public void MarkAsPushed()
            {
                Pushed = true;
            }
        }
    }
}
