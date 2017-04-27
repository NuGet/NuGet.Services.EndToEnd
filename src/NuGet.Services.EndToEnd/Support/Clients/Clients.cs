// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// A collection of the different clients necessary for interacting with NuGet's HTTP services.
    /// </summary>
    public class Clients
    {
        public Clients(
            IGalleryClient gallery,
            V3IndexClient v3Index,
            V2V3SearchClient v3Search,
            FlatContainerClient flatContainer,
            RegistrationClient registration)
        {
            Gallery = gallery;
            V3Index = v3Index;
            V3Search = v3Search;
            FlatContainer = flatContainer;
            Registration = registration;
        }

        public IGalleryClient Gallery { get; }
        public V3IndexClient V3Index { get; }
        public V2V3SearchClient V3Search { get; }
        public FlatContainerClient FlatContainer { get; }
        public RegistrationClient Registration { get; }

        /// <summary>
        /// In lieu of proper dependency injection, initialize dependencies manually.
        /// </summary>
        public static Clients Initialize()
        {
            var testSettings = TestSettings.Create();
            var httpClient = new SimpleHttpClient();
            var gallery = new GalleryClient(testSettings);
            var v3Index = new V3IndexClient(httpClient, testSettings);
            var v3Search = new V2V3SearchClient(httpClient, v3Index, testSettings);
            var flatContainer = new FlatContainerClient(httpClient, v3Index);
            var registration = new RegistrationClient(httpClient, v3Index);

            return new Clients(gallery, v3Index, v3Search, flatContainer, registration);
        }
    }
}
