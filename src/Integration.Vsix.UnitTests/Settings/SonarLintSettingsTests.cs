/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class SonarLintSettingsTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableWritableSettingsStore settingsStore;
        private ConfigurableSettingsManager settingsManager;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.settingsStore = new ConfigurableWritableSettingsStore();
            this.settingsManager = new ConfigurableSettingsManager(this.settingsStore);
        }

        [TestMethod]
        public void IntegrationSettings_Ctor_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SonarLintSettings((SettingsManager)null));
        }

        [TestMethod]
        public void IntegrationSettings_Ctor_InitializesStore()
        {
            // Sanity - should be empty store
            this.settingsStore.AssertCollectionDoesNotExist(SonarLintSettings.SettingsRoot);

            // Act
            SonarLintSettings testSubject = this.CreateTestSubject();

            // Assert
            this.settingsStore.AssertCollectionExists(SonarLintSettings.SettingsRoot);
        }

        [TestMethod]
        public void IntegrationSettings_GetValueOrDefault_Bool()
        {
            // Arrange
            SonarLintSettings testSubject = this.CreateTestSubject();

            // Test case 1: exists -> value
            // Arrange
            bool expected1 = false;
            this.settingsStore.SetBoolean(SonarLintSettings.SettingsRoot, "key1", expected1);

            // Act
            bool actual1 = testSubject.GetValueOrDefault("key1", true);

            // Assert
            actual1.Should().Be(expected1, "Did not load existing value");

            // Test case 2: does NOT exist -> default
            // Arrange
            bool expected2 = true;

            // Act
            bool actual2 = testSubject.GetValueOrDefault("key2", expected2);

            // Assert
            actual2.Should().Be(expected2, "Did not return default value");
        }

        [TestMethod]
        public void IntegrationSettings_GetValueOrDefault_Bool_NoStore()
        {
            // Arrange
            bool expected = true;
            SonarLintSettings testSubject;
            using (new AssertIgnoreScope())
            {
                testSubject = this.CreateTestSubject(storeLoadFailure: true);
            }

            // Act
            bool actual = testSubject.GetValueOrDefault("key1", expected);

            // Assert
            actual.Should().Be(expected, "Did not return default value in case of missing setting store");
        }

        [TestMethod]
        public void IntegrationSettings_SetValue_Bool()
        {
            // Arrange
            const string propertyKey = "key1";
            const string collection = SonarLintSettings.SettingsRoot;
            SonarLintSettings testSubject = this.CreateTestSubject();

            // Sanity
            this.settingsStore.AssertCollectionPropertyCount(collection, 0);

            // Test case 1: new property
            // Act
            testSubject.SetValue(propertyKey, true);

            // Assert
            this.settingsStore.AssertCollectionPropertyCount(collection, 1);
            this.settingsStore.AssertBoolean(collection, propertyKey, true);

            // Test case 2: overwrite existing property
            // Act
            testSubject.SetValue(propertyKey, false);

            // Assert
            this.settingsStore.AssertCollectionPropertyCount(collection, 1);
            this.settingsStore.AssertBoolean(collection, propertyKey, false);
        }

        [TestMethod]
        public void IntegrationSettings_SetValue_Bool_NoStore()
        {
            // Arrange
            SonarLintSettings testSubject;
            using (new AssertIgnoreScope())
            {
                testSubject = this.CreateTestSubject(storeLoadFailure: true);
            }

            // Act + Assert (no store -> no exception)
            testSubject.SetValue("key1", false);
        }

        #region Helpers

        private SonarLintSettings CreateTestSubject(bool storeLoadFailure = false)
        {
            this.settingsManager.StoreFailsToLoad = storeLoadFailure;
            return new SonarLintSettings(this.settingsManager);
        }

        #endregion Helpers
    }
}