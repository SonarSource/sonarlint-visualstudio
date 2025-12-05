/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Settings;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Integration.Vsix.Settings;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings;

[TestClass]
public class SonarLintSettingsTests
{
    private IWritableSettingsStoreFactory factory;
    private SonarLintSettings testSubject;
    private WritableSettingsStore store;

    [TestInitialize]
    public void TestInitialize()
    {
        factory = Substitute.For<IWritableSettingsStoreFactory>();
        store = Substitute.For<WritableSettingsStore>();
        MockStoreFactory(store);
        testSubject = new SonarLintSettings(factory);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SonarLintSettings, ISonarLintSettings>(
            MefTestHelpers.CreateExport<IWritableSettingsStoreFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SonarLintSettings>();

    [TestMethod]
    public void Ctor_DoesNotCallAnyServices() =>
        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.
        factory.ReceivedCalls().Should().BeEmpty();

    [TestMethod]
    public void LazyInitialization_FactoryIsOnlyCalledOnce()
    {
        factory.ReceivedCalls().Should().BeEmpty(); // Sanity check
        store.ClearReceivedCalls();

        // 1. Make any call to the test subject
        _ = testSubject.GetValueOrDefault("any", "any");

        factory.ReceivedCalls().Should().HaveCount(1);
        store.ReceivedCalls().Should().HaveCount(1);

        // 2. Call the settings again
        _ = testSubject.GetValueOrDefault("any", true);
        testSubject.SetValue("any", "any");

        factory.ReceivedCalls().Should().HaveCount(1);
        store.ReceivedCalls().Should().HaveCount(3);
    }

    [TestMethod]
    public void JreLocation_DefaultValue_ShouldBeEmpty()
    {
        testSubject.JreLocation.Should().BeEmpty();
        store.Received().GetString(SonarLintSettings.SettingsRoot, nameof(testSubject.JreLocation), string.Empty);
    }

    #region Boolean method tests

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void GetValueOrDefault_Bool(bool valueToReturn, bool defaultValueSuppliedByCaller)
    {
        store.GetBoolean(SonarLintSettings.SettingsRoot, "aProp", defaultValueSuppliedByCaller).Returns(valueToReturn);

        var actual = testSubject.GetValueOrDefault("aProp", defaultValueSuppliedByCaller);
        actual.Should().Be(valueToReturn);
    }

    [TestMethod]
    public void GetValueOrDefault_StoreThrows_ReturnsDefault_Bool()
    {
        var suppliedDefault = true;
        store.GetBoolean(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Boolean>())
            .Throws(new ArgumentException("thrown in a test"));

        var actual = testSubject.GetValueOrDefault("key1", suppliedDefault);
        actual.Should().Be(suppliedDefault);
    }

    [TestMethod]
    public void GetValueOrDefault_NoStore_Bool()
    {
        var expected = true;
        MockStoreFactory(storeToReturn: null);

        var actual = testSubject.GetValueOrDefault("key1", expected);
        actual.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("a key", true)]
    [DataRow("another key", false)]
    public void SetValue_Bool(string propertyName, bool valueToSet)
    {
        testSubject.SetValue(propertyName, valueToSet);

        CheckPropertySet(propertyName, valueToSet);
    }

    [TestMethod]
    public void SetValue_NoStore_Bool()
    {
        MockStoreFactory(storeToReturn: null);

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
        store.GetString(SonarLintSettings.SettingsRoot, "aProp", defaultValueSuppliedByCaller).Returns(valueToReturn);

        var actual = testSubject.GetValueOrDefault("aProp", defaultValueSuppliedByCaller);
        actual.Should().Be(valueToReturn);
    }

    [TestMethod]
    public void GetValueOrDefault_StoreThrows_ReturnsDefault_String()
    {
        var suppliedDefault = "the default";
        store.GetString(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new ArgumentException("thrown in a test"));

        var actual = testSubject.GetValueOrDefault("key1", suppliedDefault);
        actual.Should().Be(suppliedDefault);
    }

    [TestMethod]
    public void GetValueOrDefault_NoStore_String()
    {
        var expected = "the value";
        MockStoreFactory(storeToReturn: null);

        var actual = testSubject.GetValueOrDefault("key1", expected);
        actual.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("a key", "a value")]
    [DataRow("another key", "another value")]
    public void SetValue_String(string propertyName, string valueToSet)
    {
        testSubject.SetValue(propertyName, valueToSet);

        CheckPropertySet(propertyName, valueToSet);
    }

    [TestMethod]
    public void SetValue_NoStore_String()
    {
        MockStoreFactory(storeToReturn: null);

        // Act + Assert (no store -> no exception)
        Action act = () => testSubject.SetValue("key1", "a value");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void SetValue_Null_SetsEmptyString()
    {
        testSubject.SetValue("key1", null);

        store.Received(1).SetString(SonarLintSettings.SettingsRoot, "key1", string.Empty);
    }

    #endregion

    #region Int method tests

    [TestMethod]
    [DataRow(111, 222)]
    [DataRow(333, 222)]
    public void GetValueOrDefault_Int(int valueToReturn, int defaultValueSuppliedByCaller)
    {
        store.GetInt32(SonarLintSettings.SettingsRoot, "aProp", defaultValueSuppliedByCaller).Returns(valueToReturn);

        var actual = testSubject.GetValueOrDefault("aProp", defaultValueSuppliedByCaller);
        actual.Should().Be(valueToReturn);
    }

    [TestMethod]
    public void GetValueOrDefault_StoreThrows_ReturnsDefault_Int()
    {
        var suppliedDefault = -123;

        store.GetInt32(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Throws(new ArgumentException("thrown in a test"));

        var actual = testSubject.GetValueOrDefault("key1", suppliedDefault);
        actual.Should().Be(suppliedDefault);
    }

    [TestMethod]
    public void GetValueOrDefault_NoStore_Int()
    {
        var expected = 888;
        MockStoreFactory(storeToReturn: null);

        var actual = testSubject.GetValueOrDefault("key1", expected);
        actual.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("a key", 123)]
    [DataRow("another key", 456)]
    public void SetValue_Int(string propertyName, int valueToSet)
    {
        testSubject.SetValue(propertyName, valueToSet);

        CheckPropertySet(propertyName, valueToSet);
    }

    [TestMethod]
    public void SetValue_NoStore_Int()
    {
        MockStoreFactory(storeToReturn: null);

        // Act + Assert (no store -> no exception)
        Action act = () => testSubject.SetValue("key1", 123);

        act.Should().NotThrow();
    }

    #endregion

    [TestMethod]
    public void IsActivateMoreEnabled_WhenDisposed_ReturnsDefault()
    {
        testSubject.IsActivateMoreEnabled = true;
        var activateMoreEnabled = testSubject.IsActivateMoreEnabled;

        activateMoreEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void DaemonLogLevel_WhenDisposed_ReturnsDefault()
    {
        MockComDetachedTestSubject();

        testSubject.DaemonLogLevel = DaemonLogLevel.Verbose;
        var daemonLogLevel = testSubject.DaemonLogLevel;

        daemonLogLevel.Should().Be(DaemonLogLevel.Minimal);
    }

    [TestMethod]
    public void JreLocation_WhenDisposed_ReturnsDefault()
    {
        MockComDetachedTestSubject();
        testSubject.JreLocation = "/path/to/jre";
        var jreLocation = testSubject.JreLocation;

        jreLocation.Should().BeEmpty();
    }

    [TestMethod]
    public void ShowCloudRegion_DefaultValue_ShouldBeFalse()
    {
        testSubject.ShowCloudRegion.Should().BeFalse();
        store.Received().GetBoolean(SonarLintSettings.SettingsRoot, nameof(testSubject.ShowCloudRegion), false);
    }

    [TestMethod]
    public void ShowCloudRegion_WhenDisposed_ReturnsDefault()
    {
        MockComDetachedTestSubject();
        testSubject.ShowCloudRegion = true;
        var showCloudRegion = testSubject.ShowCloudRegion;

        showCloudRegion.Should().BeFalse();
    }

    [TestMethod]
    public void IsFocusOnNewCodeEnabled_DefaultValue_ShouldBeFalse()
    {
        testSubject.IsFocusOnNewCodeEnabled.Should().BeFalse();
        store.Received().GetBoolean(SonarLintSettings.SettingsRoot, nameof(testSubject.IsFocusOnNewCodeEnabled), false);
    }

    [TestMethod]
    public void IsFocusOnNewCodeEnabled_SetValue_GetsAndSetsCorrectly()
    {
        store.GetBoolean(SonarLintSettings.SettingsRoot, nameof(testSubject.IsFocusOnNewCodeEnabled), false).Returns(true);

        testSubject.IsFocusOnNewCodeEnabled = true;
        var value = testSubject.IsFocusOnNewCodeEnabled;

        value.Should().BeTrue();
        store.Received().SetBoolean(SonarLintSettings.SettingsRoot, nameof(testSubject.IsFocusOnNewCodeEnabled), true);
    }

    [TestMethod]
    public void IsFocusOnNewCodeEnabled_WhenDisposed_ReturnsDefault()
    {
        MockComDetachedTestSubject();
        testSubject.IsFocusOnNewCodeEnabled = true;
        var value = testSubject.IsFocusOnNewCodeEnabled;
        value.Should().BeFalse();
    }

    private void MockComDetachedTestSubject()
    {
        var comException = new InvalidComObjectException("COM object that has been separated from its underlying RCW cannot be used.");
        store.When(x => x.GetBoolean(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(x => throw comException);
        store.When(x => x.SetBoolean(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(x => throw comException);
        store.When(x => x.GetString(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
            .Do(x => throw comException);
        store.When(x => x.SetString(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
            .Do(x => throw comException);
        store.When(x => x.GetInt32(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()))
            .Do(x => throw comException);
        store.When(x => x.SetInt32(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()))
            .Do(x => throw comException);
        testSubject.Dispose();
    }

    private void MockStoreFactory(
        WritableSettingsStore storeToReturn = null) =>
        factory.Create(Arg.Any<string>()).Returns(storeToReturn);

    private void CheckPropertySet(string propertyKey, bool value) => store.Received(1).SetBoolean(SonarLintSettings.SettingsRoot, propertyKey, value);

    private void CheckPropertySet(string propertyKey, int value) => store.Received(1).SetInt32(SonarLintSettings.SettingsRoot, propertyKey, value);

    private void CheckPropertySet(string propertyKey, string value) => store.Received(1).SetString(SonarLintSettings.SettingsRoot, propertyKey, value);
}
