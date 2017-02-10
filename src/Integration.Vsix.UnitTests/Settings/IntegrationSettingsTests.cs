/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class IntegrationSettingsTests
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
            Exceptions.Expect<ArgumentNullException>(() => new IntegrationSettings(null));
        }

        [TestMethod]
        public void IntegrationSettings_Ctor_InitializesStore()
        {
            // Sanity - should be empty store
            this.settingsStore.AssertCollectionDoesNotExist(IntegrationSettings.SettingsRoot);

            // Act
            IntegrationSettings testSubject = this.CreateTestSubject();

            // Assert
            this.settingsStore.AssertCollectionExists(IntegrationSettings.SettingsRoot);
        }

        [TestMethod]
        public void IntegrationSettings_GetValueOrDefault_Bool()
        {
            // Arrange
            IntegrationSettings testSubject = this.CreateTestSubject();

            // Test case 1: exists -> value
            // Arrange
            bool expected1 = false;
            this.settingsStore.SetBoolean(IntegrationSettings.SettingsRoot, "key1", expected1);

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
            IntegrationSettings testSubject;
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
            const string collection = IntegrationSettings.SettingsRoot;
            IntegrationSettings testSubject = this.CreateTestSubject();

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
            IntegrationSettings testSubject;
            using (new AssertIgnoreScope())
            {
                testSubject = this.CreateTestSubject(storeLoadFailure: true);
            }

            // Act + Assert (no store -> no exception)
            testSubject.SetValue("key1", false);
        }

        #region Helpers

        private IntegrationSettings CreateTestSubject(bool storeLoadFailure = false)
        {
            this.settingsManager.StoreFailsToLoad = storeLoadFailure;
            return new IntegrationSettings(this.serviceProvider, this.settingsManager);
        }

        #endregion Helpers
    }
}