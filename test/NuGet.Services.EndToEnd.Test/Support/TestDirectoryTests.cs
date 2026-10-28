// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestDirectoryTests
    {
        [Fact]
        public void ParallelDirectoriesHaveIndependentLifetimes()
        {
            var directories = new TestDirectory[64];

            try
            {
                Parallel.For(0, directories.Length, index => directories[index] = TestDirectory.Create());
                Assert.Equal(directories.Length, directories.Select(directory => directory.FullPath).Distinct().Count());

                var path = Path.Combine(directories[0].FullPath, "owned.txt");
                File.WriteAllText(path, "first directory");
                directories[1].Dispose();

                Assert.True(File.Exists(path));
            }
            finally
            {
                foreach (var directory in directories)
                {
                    directory?.Dispose();
                }
            }
        }
    }
}
