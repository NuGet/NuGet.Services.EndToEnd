﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.AzureManagement;

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
            V2V3SearchClient v2v3Search,
            FlatContainerClient flatContainer,
            RegistrationClient registration,
            NuGetExeClient nuGetExe)
        {
            Gallery = gallery;
            V3Index = v3Index;
            V2V3Search = v2v3Search;
            FlatContainer = flatContainer;
            Registration = registration;
            NuGetExe = nuGetExe;
        }

        public IGalleryClient Gallery { get; }
        public V3IndexClient V3Index { get; }
        public V2V3SearchClient V2V3Search { get; }
        public FlatContainerClient FlatContainer { get; }
        public RegistrationClient Registration { get; }
        public NuGetExeClient NuGetExe { get; }

        /// <summary>
        /// In lieu of proper dependency injection, initialize dependencies manually.
        /// </summary>
        public static Clients Initialize()
        {
            var testSettings = TestSettings.Create();
            var httpClient = new SimpleHttpClient();
            var gallery = new GalleryClient(httpClient, testSettings);
            var v3Index = new V3IndexClient(httpClient, testSettings);
            var v2v3Search = new V2V3SearchClient(httpClient, v3Index, testSettings, GetAzureManagementAPIWrapper(testSettings));
            var flatContainer = new FlatContainerClient(httpClient, v3Index);
            var registration = new RegistrationClient(httpClient, v3Index);
            var nuGetExe = new NuGetExeClient(testSettings);

            return new Clients(
                gallery,
                v3Index,
                v2v3Search,
                flatContainer,
                registration,
                nuGetExe);
        }

        private static IAzureManagementAPIWrapper GetAzureManagementAPIWrapper(TestSettings testSettings)
        {
            if (testSettings.AzureManagementAPIWrapperConfiguration != null)
            {
                return new AzureManagementAPIWrapper(testSettings.AzureManagementAPIWrapperConfiguration);
            }

            return null;
        }
    }
}
