/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using Microsoft.VisualStudio.Settings;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Settings;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class SonarLintSettingsTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<SonarLintSettings, ISonarLintSettings>(
                MefTestHelpers.CreateExport<IWritableSettingsStoreFactory>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<SonarLintSettings>();

        [TestMethod]
        public void Ctor_DoesNotCallAnyServices()
        {
            var storeFactory = new Mock<IWritableSettingsStoreFactory>();
            _ = CreateTestSubject(storeFactory.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            storeFactory.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void LazyInitialization_FactoryIsOnlyCalledOnce()
        {
            var store = new Mock<WritableSettingsStore>();
            var factory = CreateStoreFactory(store.Object);

            var testSubject = CreateTestSubject(factory.Object);

            factory.Invocations.Should().BeEmpty(); // Sanity check
            store.Reset();

            // 1. Make any call to the test subject
            _ = testSubject.GetValueOrDefault("any", "any");

            factory.Invocations.Should().HaveCount(1);
            store.Invocations.Should().HaveCount(1);

            // 2. Call the settings again
            _ = testSubject.GetValueOrDefault("any", true);
            testSubject.SetValue("any", "any");

            factory.Invocations.Should().HaveCount(1);
            store.Invocations.Should().HaveCount(3);
        }

        #region Boolean method tests

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        [DataRow(false, false)]
        public void GetValueOrDefault_Bool(bool valueToReturn, bool defaultValueSuppliedByCaller)
        {
            var store = new Mock<WritableSettingsStore>();
            store.Setup(x => x.GetBoolean(SonarLintSettings.SettingsRoot, "aProp", defaultValueSuppliedByCaller)).Returns(valueToReturn);

            var testSubject = CreateTestSubject(store.Object);

            var actual = testSubject.GetValueOrDefault("aProp", defaultValueSuppliedByCaller);
            actual.Should().Be(valueToReturn);
        }

        [TestMethod]
        public void GetValueOrDefault_StoreThrows_ReturnsDefault_Bool()
        {
            var suppliedDefault = true;

            var store = new Mock<WritableSettingsStore>();
            store.Setup(x => x.GetBoolean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
                .Throws(new ArgumentException("thrown in a test"));

            var testSubject = CreateTestSubject(storeToReturn: store.Object);

            var actual = testSubject.GetValueOrDefault("key1", suppliedDefault);
            actual.Should().Be(suppliedDefault);
        }

        [TestMethod]
        public void GetValueOrDefault_NoStore_Bool()
        {
            var expected = true;
            var testSubject = CreateTestSubject(storeToReturn: null);

            var actual = testSubject.GetValueOrDefault("key1", expected);
            actual.Should().Be(expected);
        }

        [TestMethod]
        [DataRow("a key", true)]
        [DataRow("another key", false)]
        public void SetValue_Bool(string propertyName, bool valueToSet)
        {
            var settingsStore = new Mock<WritableSettingsStore>();

            var testSubject = CreateTestSubject(settingsStore.Object);

            testSubject.SetValue(propertyName, valueToSet);
            
            CheckPropertySet(settingsStore, propertyName, valueToSet);
        }

        [TestMethod]
        public void SetValue_NoStore_Bool()
        {
            var testSubject = CreateTestSubject(storeToReturn: null);

            // Act + Assert (no store -> no exception)
            Action act = () => testSubject.SetValue("key1", false);

            act.Should().NotThrow();
        }

        #endregion

        #region String method tests

        [TestMethod]
        [DataRow("val1", "unused default")]
        [DataRow("xxx", "xxx")]
        public void GetValueOrDefault_String(string valueToReturn, string defaultValueSuppliedByCaller)
        {
            var store = new Mock<WritableSettingsStore>();
            store.Setup(x => x.GetString(SonarLintSettings.SettingsRoot, "aProp", defaultValueSuppliedByCaller)).Returns(valueToReturn);

            var testSubject = CreateTestSubject(store.Object);

            var actual = testSubject.GetValueOrDefault("aProp", defaultValueSuppliedByCaller);
            actual.Should().Be(valueToReturn);
        }

        [TestMethod]
        public void GetValueOrDefault_StoreThrows_ReturnsDefault_String()
        {
            var suppliedDefault = "the default";

            var store = new Mock<WritableSettingsStore>();
            store.Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException("thrown in a test"));

            var testSubject = CreateTestSubject(storeToReturn: store.Object);

            var actual = testSubject.GetValueOrDefault("key1", suppliedDefault);
            actual.Should().Be(suppliedDefault);
        }

        [TestMethod]
        public void GetValueOrDefault_NoStore_String()
        {
            string expected = "the value";
            var testSubject = CreateTestSubject(storeToReturn: null);

            var actual = testSubject.GetValueOrDefault("key1", expected);
            actual.Should().Be(expected);
        }

        [TestMethod]
        [DataRow("a key", "a value")]
        [DataRow("another key", "another value")]
        public void SetValue_String(string propertyName, string valueToSet)
        {
            var settingsStore = new Mock<WritableSettingsStore>();

            var testSubject = CreateTestSubject(settingsStore.Object);

            testSubject.SetValue(propertyName, valueToSet);

            CheckPropertySet(settingsStore, propertyName, valueToSet);
        }

        [TestMethod]
        public void SetValue_NoStore_String()
        {
            var testSubject = CreateTestSubject(storeToReturn: null);

            // Act + Assert (no store -> no exception)
            Action act = () => testSubject.SetValue("key1", "a value");

            act.Should().NotThrow();
        }

        #endregion

        #region Int method tests

        [TestMethod]
        [DataRow(111, 222)]
        [DataRow(333, 222)]
        public void GetValueOrDefault_Int(int valueToReturn, int defaultValueSuppliedByCaller)
        {
            var store = new Mock<WritableSettingsStore>();
            store.Setup(x => x.GetInt32(SonarLintSettings.SettingsRoot, "aProp", defaultValueSuppliedByCaller)).Returns(valueToReturn);

            var testSubject = CreateTestSubject(store.Object);

            var actual = testSubject.GetValueOrDefault("aProp", defaultValueSuppliedByCaller);
            actual.Should().Be(valueToReturn);
        }

        [TestMethod]
        public void GetValueOrDefault_StoreThrows_ReturnsDefault_Int()
        {
            var suppliedDefault = -123;

            var store = new Mock<WritableSettingsStore>();
            store.Setup(x => x.GetInt32(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws(new ArgumentException("thrown in a test"));

            var testSubject = CreateTestSubject(storeToReturn: store.Object);

            var actual = testSubject.GetValueOrDefault("key1", suppliedDefault);
            actual.Should().Be(suppliedDefault);
        }

        [TestMethod]
        public void GetValueOrDefault_NoStore_Int()
        {
            int expected = 888;
            var testSubject = CreateTestSubject(storeToReturn: null);

            var actual = testSubject.GetValueOrDefault("key1", expected);
            actual.Should().Be(expected);
        }

        [TestMethod]
        [DataRow("a key", 123)]
        [DataRow("another key", 456)]
        public void SetValue_Int(string propertyName, int valueToSet)
        {
            var settingsStore = new Mock<WritableSettingsStore>();

            var testSubject = CreateTestSubject(settingsStore.Object);

            testSubject.SetValue(propertyName, valueToSet);

            CheckPropertySet(settingsStore, propertyName, valueToSet);
        }

        [TestMethod]
        public void SetValue_NoStore_Int()
        {
            var testSubject = CreateTestSubject(storeToReturn: null);

            // Act + Assert (no store -> no exception)
            Action act = () => testSubject.SetValue("key1", 123);

            act.Should().NotThrow();
        }

        #endregion
        private static SonarLintSettings CreateTestSubject(IWritableSettingsStoreFactory storeFactory)
            => new SonarLintSettings(storeFactory);

        private static SonarLintSettings CreateTestSubject(WritableSettingsStore storeToReturn)
            => CreateTestSubject(CreateStoreFactory(storeToReturn).Object);

        private static Mock<IWritableSettingsStoreFactory> CreateStoreFactory(
            WritableSettingsStore storeToReturn = null)
        {
            var factory = new Mock<IWritableSettingsStoreFactory>();
            factory.Setup(x => x.Create(It.IsAny<string>())).Returns(storeToReturn);
            return factory;
        }

        private static void CheckPropertySet(Mock<WritableSettingsStore> store, string propertyKey, bool value)
            => store.Verify(x => x.SetBoolean(SonarLintSettings.SettingsRoot, propertyKey, value), Times.Once);

        private static void CheckPropertySet(Mock<WritableSettingsStore> store, string propertyKey, int value)
            => store.Verify(x => x.SetInt32(SonarLintSettings.SettingsRoot, propertyKey, value), Times.Once);

        private static void CheckPropertySet(Mock<WritableSettingsStore> store, string propertyKey, string value)
            => store.Verify(x => x.SetString(SonarLintSettings.SettingsRoot, propertyKey, value), Times.Once);
    }
}
