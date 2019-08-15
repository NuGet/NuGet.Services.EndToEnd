// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Reflection;

namespace NuGet.Services.EndToEnd.Support.Utilities
{
    public static class TestDataResourceUtility
    {
        private const string ResourceNameFormat = "NuGet.Services.EndToEnd.Support.TestData.{0}";
        private static readonly Assembly CurrentAssembly = typeof(TestDataResourceUtility).Assembly;

        public static byte[] GetResourceBytes(string name)
        {
            using (var reader = new BinaryReader(GetManifestResourceStream(name)))
            {
                return reader.ReadBytes((int)reader.BaseStream.Length);
            }
        }

        private static Stream GetManifestResourceStream(string name)
        {
            var resourceName = GetResourceName(name);

            return CurrentAssembly.GetManifestResourceStream(resourceName);
        }

        private static string GetResourceName(string name)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ResourceNameFormat,
                name);
        }
    }
}
