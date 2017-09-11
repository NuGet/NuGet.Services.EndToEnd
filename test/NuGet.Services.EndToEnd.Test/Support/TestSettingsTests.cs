// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettingsTests
    {
        [Fact]
        public void TestSettingsManualConfigurationOverrideShouldBeEmpty()
        {
            /// This test is expected to fail when there is a local change to <see cref="TestSettings.ManualConfigurationOverride"/>.
            /// This is okay. We just want to be sure not to check this change in.
            Assert.Equal(string.Empty, TestSettings.ManualConfigurationOverride);
        }

        [Fact]
        public void TestSettingsAggressivePushShouldAlwaysBeTrue()
        {
            /// This test is expected to fail when there is a local change to <see cref="TestSettings.AggressivePush"/>.
            /// This is okay. We just want to be sure not to check this change in.
            Assert.True(TestSettings.AggressivePush);
        }

        [Theory]
        [InlineData("Dev")]
        [InlineData("Int")]
        [InlineData("Prod")]
        public async Task TestSettingsShouldNotMentionRealApiKeys(string configurationName)
        {
            // Arrange
            Assert.Equal("API_KEY", (await TestSettings.CreateLocalTestConfigurationAsync(configurationName)).ApiKey);
        }
    }
}
