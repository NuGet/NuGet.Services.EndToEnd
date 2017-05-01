// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace NuGet.Services.EndToEnd.Support
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SemVer2InlineDataAttribute : DataAttribute
    {
        private readonly object[] _data;

        public SemVer2InlineDataAttribute(params object[] data)
        {
            _data = data;

            var testSettings = TestSettings.Create();
            if (!testSettings.SemVer2Enabled)
            {
                Skip = "SemVer 2.0.0 is not enabled. Set the SemVer2Enabled environment variable to true to enable this test.";
            }
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return new[] { _data };
        }
    }
}
