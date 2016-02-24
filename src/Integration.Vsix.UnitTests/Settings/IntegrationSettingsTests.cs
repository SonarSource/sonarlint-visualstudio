//-----------------------------------------------------------------------
// <copyright file="IntegrationSettingsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;

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
            // Santity - should be empty store
            this.settingsStore.AssertCollectionDoesNotExist(IntegrationSettings.SettingsRoot);

            // Act
            IntegrationSettings testSubject = this.CreateTestSubject();

            // Verify
            this.settingsStore.AssertCollectionExists(IntegrationSettings.SettingsRoot);
        }

        [TestMethod]
        public void IntegrationSettings_GetValueOrDefault_Bool()
        {
            // Setup
            IntegrationSettings testSubject = this.CreateTestSubject();

            // Test case 1: exists -> value
            // Setup
            bool expected1 = false;
            this.settingsStore.SetBoolean(IntegrationSettings.SettingsRoot, "key1", expected1);

            // Act
            bool actual1 = testSubject.GetValueOrDefault("key1", true);

            // Verify
            Assert.AreEqual(expected1, actual1, "Did not load existing value");

            // Test case 2: does NOT exist -> default
            // Setup
            bool expected2 = true;

            // Act
            bool actual2 = testSubject.GetValueOrDefault("key2", expected2);

            // Verify
            Assert.AreEqual(expected2, actual2, "Did not return default value");
        }

        [TestMethod]
        public void IntegrationSettings_GetValueOrDefault_Bool_NoStore()
        {
            // Setup
            bool expected = true;
            IntegrationSettings testSubject;
            using (new AssertIgnoreScope())
            {
                testSubject = this.CreateTestSubject(storeLoadFailure: true);
            }

            // Act
            bool actual = testSubject.GetValueOrDefault("key1", expected);

            // Verify
            Assert.AreEqual(expected, actual, "Did not return default value in case of missing setting store");
        }


        [TestMethod]
        public void IntegrationSettings_SetValue_Bool()
        {
            // Setup
            const string propertyKey = "key1";
            const string collection = IntegrationSettings.SettingsRoot;
            IntegrationSettings testSubject = this.CreateTestSubject();

            // Sanity
            this.settingsStore.AssertCollectionPropertyCount(collection, 0);

            // Test case 1: new property
            // Act
            testSubject.SetValue(propertyKey, true);

            // Verify
            this.settingsStore.AssertCollectionPropertyCount(collection, 1);
            this.settingsStore.AssertBoolean(collection, propertyKey, true);

            // Test case 2: overwrite existing property
            // Act
            testSubject.SetValue(propertyKey, false);

            // Verify
            this.settingsStore.AssertCollectionPropertyCount(collection, 1);
            this.settingsStore.AssertBoolean(collection, propertyKey, false);
        }

        [TestMethod]
        public void IntegrationSettings_SetValue_Bool_NoStore()
        {
            // Setup
            IntegrationSettings testSubject;
            using (new AssertIgnoreScope())
            {
                testSubject = this.CreateTestSubject(storeLoadFailure: true);
            }

            // Act + Verify (no store -> no exception)
            testSubject.SetValue("key1", false);
        }

        #region Helpers

        private IntegrationSettings CreateTestSubject(bool storeLoadFailure = false)
        {
            this.settingsManager.StoreFailsToLoad = storeLoadFailure;
            return new IntegrationSettings(this.serviceProvider, this.settingsManager);
        }

        #endregion

    }
}
