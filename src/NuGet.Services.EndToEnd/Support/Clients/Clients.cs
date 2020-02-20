// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A collection of the different clients necessary for interacting with NuGet's HTTP services.
    /// </summary>
    public class Clients
    {
        private static Clients _clients = null;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public Clients(
            IGalleryClient gallery,
            V3IndexClient v3Index,
            V2V3SearchClient v2v3Search,
            FlatContainerClient flatContainer,
            RegistrationClient registration,
            NuGetExeClient nuGetExe,
            SymbolServerClient symbolServerClient)
        {
            Gallery = gallery;
            V3Index = v3Index;
            V2V3Search = v2v3Search;
            FlatContainer = flatContainer;
            Registration = registration;
            NuGetExe = nuGetExe;
            SymbolServerClient = symbolServerClient;
        }

        public IGalleryClient Gallery { get; }
        public V3IndexClient V3Index { get; }
        public V2V3SearchClient V2V3Search { get; }
        public FlatContainerClient FlatContainer { get; }
        public RegistrationClient Registration { get; }
        public NuGetExeClient NuGetExe { get; }
        public SymbolServerClient SymbolServerClient { get; }

        public static Clients Initialize(TestSettings testSettings)
        {
            if (_clients != null)
            {
                return _clients;
            }

            try
            {
                _semaphore.Wait();

                if (_clients != null)
                {
                    return _clients;
                }

                _clients = InitializeInternal(testSettings);
            }
            finally
            {
                _semaphore.Release();
            }
            

            return _clients;
        }

        /// <summary>
        /// In lieu of proper dependency injection, initialize dependencies manually.
        /// </summary>
        private static Clients InitializeInternal(TestSettings testSettings)
        {
            // Ensure that SSLv3 is disabled and that Tls v1.2 is enabled.
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Ssl3;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var httpClient = new SimpleHttpClient();
            var gallery = new GalleryClient(httpClient, testSettings);
            var v3Index = new V3IndexClient(httpClient, testSettings);
            var v2v3Search = new V2V3SearchClient(httpClient, v3Index, testSettings);
            var flatContainer = new FlatContainerClient(httpClient, v3Index);
            var registration = new RegistrationClient(httpClient, v3Index);
            var nuGetExe = new NuGetExeClient(testSettings, gallery);
            var symbolServerClient = new SymbolServerClient(testSettings);

            return new Clients(
                gallery,
                v3Index,
                v2v3Search,
                flatContainer,
                registration,
                nuGetExe,
                symbolServerClient);
        }
    }
}
