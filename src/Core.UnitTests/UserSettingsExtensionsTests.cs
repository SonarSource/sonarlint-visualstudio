/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class UserSettingsExtensionsTests
    {
        [TestMethod]
        public void IsDisabled_NullSettings_ReturnsFalse()
        {
            UserSettingsExtensions.IsLanguageDisabled(null, "foo").Should().BeFalse();
        }

        [TestMethod]
        public void IsDisabled_NotDisabled_ReturnsFalse()
        {
            var userSettings = CreateSettings("aaa", "bbb");
            userSettings.IsLanguageDisabled("xxx").Should().BeFalse();
        }

        [TestMethod]
        [DataRow("aaa")]
        [DataRow("AAA")]
        public void IsDisabled_IsDisabled_ReturnsTrue(string languageKey)
        {
            var userSettings = CreateSettings("aaa", "bbb");
            userSettings.IsLanguageDisabled(languageKey).Should().BeTrue();
        }

        [TestMethod]
        public void Set_NullSettings_Throws()
        {
            UserSettings userSettings = null;
            Action act = () => userSettings.SetLanguageStatus("foo", false);
            act.Should().ThrowExactly<NullReferenceException>();
        }

        [TestMethod]
        public void Set_DisableLanguage_NotInitiallyDisabled_IsDisabled()
        {
            var userSettings = CreateSettings("aaa", "bbb");

            userSettings.SetLanguageStatus("XXX", true);
            CheckExpectedLanguages(userSettings, "aaa", "bbb", "XXX"); // other languages should be preserved
        }

        [TestMethod]
        [DataRow("zzz")]
        [DataRow("ZZZ")]
        public void Set_DisableLanguage_AlreadyDisabled_IsDisabled(string key)
        {
            var userSettings = CreateSettings("aaa", "bbb", "zzz");

            userSettings.SetLanguageStatus(key, true);
            CheckExpectedLanguages(userSettings, "aaa", "bbb", "zzz");
        }

        [TestMethod]
        public void Set_EnableLanguage_NotInitiallyDisabled_IsEnabled()
        {
            var userSettings = CreateSettings("aaa", "bbb");

            userSettings.SetLanguageStatus("XXX", false);
            CheckExpectedLanguages(userSettings, "aaa", "bbb"); // other languages should be preserved
        }

        [TestMethod]
        [DataRow("xxx")]
        [DataRow("XXX")]
        public void Set_EnableLanguage_InitiallyDisabled_IsEnabled(string key)
        {
            var userSettings = CreateSettings("aaa", "bbb", "XXX");

            userSettings.SetLanguageStatus(key, false);
            CheckExpectedLanguages(userSettings, "aaa", "bbb"); // other languages should be preserved
        }


        private static UserSettings CreateSettings(params string[] disabledLanguages)
        {
            var userSettings = new UserSettings(new RulesSettings());

            foreach(string item in disabledLanguages)
            {
                userSettings.RulesSettings.General.DisableLanguages.Add(item);
            }

            return userSettings;
        }

        private static void CheckExpectedLanguages(UserSettings userSettings, params string[] expected) =>
            userSettings.RulesSettings.General.DisableLanguages.Should().BeEquivalentTo(expected);
    }
}
