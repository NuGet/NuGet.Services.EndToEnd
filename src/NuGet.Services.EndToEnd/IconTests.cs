// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.EndToEnd.Support;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd
{
    [Collection(nameof(PushedPackagesCollection))]
    public class IconTests : IClassFixture<TrustedHttpsCertificatesFixture>
    {
        public IconTests(PushedPackagesFixture pushedPackages, ITestOutputHelper logger)
        {

        }

        [Theory]
        [InlineData("icon.jpg")]
        [InlineData("icon.png")]
        public async Task PushedPackageWithIconIsAvailableInV3(string iconFilename)
        {

        }
    }
}
