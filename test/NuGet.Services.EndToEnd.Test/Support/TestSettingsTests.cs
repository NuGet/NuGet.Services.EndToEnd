// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettingsTests
    {
        [Fact]
        public void TestSettingsModeShouldAlwaysBeEnvironmentVariables()
        {
            /// This test is expected to fail when there is a local change to <see cref="TestSettings.CurrentMode"/>.
            /// This is okay. We just want to be sure not to check this change in.
            Assert.Equal(TestSettings.Mode.EnvironmentVariables, TestSettings.CurrentMode);
        }

        [Fact]
        public void TestSettingsAggressivePushShouldAlwaysBeTrue()
        {
            /// This test is expected to fail when there is a local change to <see cref="TestSettings.DefaultAggressivePush"/>.
            /// This is okay. We just want to be sure not to check this change in.
            Assert.True(TestSettings.DefaultAggressivePush);
        }

        [Theory]
        [InlineData(TestSettings.Mode.Dev)]
        [InlineData(TestSettings.Mode.Int)]
        [InlineData(TestSettings.Mode.Prod)]
        public void TestSettingsShouldNotMentionRealApiKeys(TestSettings.Mode mode)
        {
            // Arrange
            Assert.Equal("API_KEY", TestSettings.DefaultTestSettings[mode].ApiKey);
        }
    }
}
