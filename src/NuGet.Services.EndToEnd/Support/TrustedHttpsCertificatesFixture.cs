// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NuGet.Services.EndToEnd.Support
{
    public class TrustedHttpsCertificatesFixture : IDisposable
    {
        private readonly TestSettings _testSettings;

        public TrustedHttpsCertificatesFixture()
        {
            _testSettings = TestSettings.CreateFromEnvironment();
            ServicePointManager.ServerCertificateValidationCallback += ServerCertificateValidationCallback;
        }
        
        private bool ServerCertificateValidationCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // If the only SSL error is name mismatch and the HTTPS certificate's thumbprint is in the
            // list of trusted certificates, allow the certificate validation to pass.
            var x509certificate2 = certificate as X509Certificate2;
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch &&
                x509certificate2 != null &&
                _testSettings.TrustedHttpsCertificates.Contains(x509certificate2.Thumbprint, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            ServicePointManager.ServerCertificateValidationCallback -= ServerCertificateValidationCallback;
        }
    }

}
