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

using System.Windows.Input;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Settings;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings;

[TestClass]
public class GeneralOptionsDialogControlViewModelTests
{
    private GeneralOptionsDialogControlViewModel testSubject;
    private ISonarLintSettings settings;
    private ICommand openSettingsFileCommand;
    private IBrowserService browserService;
    private IFocusOnNewCodeServiceUpdater focusOnNewCodeService;
    private const string JreLocation = "C:/jrePath";

    [TestInitialize]
    public void TestInitialize()
    {
        settings = Substitute.For<ISonarLintSettings>();
        openSettingsFileCommand = Substitute.For<ICommand>();
        browserService = Substitute.For<IBrowserService>();
        focusOnNewCodeService = Substitute.For<IFocusOnNewCodeServiceUpdater>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));
        testSubject = new GeneralOptionsDialogControlViewModel(settings, focusOnNewCodeService, browserService, openSettingsFileCommand);
    }

    [TestMethod]
    public void Ctor_OpenSettingsFileCommandNull_ThrowsException()
    {
        Action act = () => _ = new GeneralOptionsDialogControlViewModel(settings, focusOnNewCodeService, browserService, null);
        act.Should().Throw<ArgumentNullException>(nameof(openSettingsFileCommand));
    }

    [TestMethod]
    public void Ctor_BrowserServiceNull_ThrowsException()
    {
        Action act = () => _ = new GeneralOptionsDialogControlViewModel(settings, focusOnNewCodeService, null, openSettingsFileCommand);
        act.Should().Throw<ArgumentNullException>(nameof(browserService));
    }

    [TestMethod]
    [DataRow(DaemonLogLevel.Verbose)]
    [DataRow(DaemonLogLevel.Info)]
    [DataRow(DaemonLogLevel.Minimal)]
    public void Ctor_SetsDaemonLogLevelToValueFromSettings(DaemonLogLevel expectedLevel)
    {
        settings.DaemonLogLevel.Returns(expectedLevel);

        _ = settings.Received().DaemonLogLevel;
        testSubject.SelectedDaemonLogLevel = expectedLevel;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_SetsIsActivateMoreEnabledToValueFromSettings(bool expectedIsActivateMoreEnabled)
    {
        settings.IsActivateMoreEnabled.Returns(expectedIsActivateMoreEnabled);

        _ = settings.Received().IsActivateMoreEnabled;
        testSubject.IsActivateMoreEnabled = expectedIsActivateMoreEnabled;
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(JreLocation)]
    public void Ctor_SetsJreLocationToValueFromSettings(string expectedJreLocation)
    {
        settings.JreLocation.Returns(expectedJreLocation);

        _ = settings.Received().JreLocation;
        testSubject.JreLocation = expectedJreLocation;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_SetsShowCloudRegionToValueFromSettings(bool expectedShowRegion)
    {
        settings.ShowCloudRegion.Returns(expectedShowRegion);

        _ = settings.Received().ShowCloudRegion;
        testSubject.ShowCloudRegion = expectedShowRegion;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_SetsIsFocusOnNewCodeEnabled_FromService(bool expected)
    {
        var focusStatus = new FocusOnNewCodeStatus(expected);
        focusOnNewCodeService.Current.Returns(focusStatus);
        testSubject = new GeneralOptionsDialogControlViewModel(settings, focusOnNewCodeService, browserService, openSettingsFileCommand);
        testSubject.IsFocusOnNewCodeEnabled.Should().Be(expected);
    }

    [TestMethod]
    [DataRow(DaemonLogLevel.Verbose)]
    [DataRow(DaemonLogLevel.Info)]
    [DataRow(DaemonLogLevel.Minimal)]
    public void SaveSettings_SavesDaemonLogLevelToSettings(DaemonLogLevel expectedLevel)
    {
        testSubject.SelectedDaemonLogLevel = expectedLevel;

        testSubject.SaveSettings();

        settings.Received().DaemonLogLevel = expectedLevel;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SaveSettings_SavesIsActivateMoreEnabledToSettings(bool expectedIsActivateMoreEnabled)
    {
        testSubject.IsActivateMoreEnabled = expectedIsActivateMoreEnabled;

        testSubject.SaveSettings();

        settings.Received().IsActivateMoreEnabled = expectedIsActivateMoreEnabled;
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(JreLocation)]
    public void SaveSettings_SavesJreLocationToSettings(string expectedJreLocation)
    {
        testSubject.JreLocation = expectedJreLocation;

        testSubject.SaveSettings();

        settings.Received().JreLocation = expectedJreLocation;
    }

    [TestMethod]
    public void SaveSettings_TrimsJreLocation()
    {
        testSubject.JreLocation = $"   {JreLocation}   ";

        testSubject.SaveSettings();

        settings.Received().JreLocation = JreLocation;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SaveSettings_SavesShowCloudRegionToSettings(bool expectedShowCloudRegion)
    {
        testSubject.ShowCloudRegion = expectedShowCloudRegion;

        testSubject.SaveSettings();

        settings.Received().ShowCloudRegion = expectedShowCloudRegion;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SaveSettings_CallsFocusOnNewCodeServiceSet(bool isEnabled)
    {
        testSubject.IsFocusOnNewCodeEnabled = isEnabled;
        testSubject.SaveSettings();
        focusOnNewCodeService.Received(1).SetPreference(isEnabled);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void FocusOnNewCodeServiceUpdaterOnChanged_UpdatesIsFocusOnNewCodeEnabled(bool newValue)
    {
        var eventArgs = new NewCodeStatusChangedEventArgs(new FocusOnNewCodeStatus(newValue));
        focusOnNewCodeService.Changed += Raise.EventWith(focusOnNewCodeService, eventArgs);
        testSubject.IsFocusOnNewCodeEnabled.Should().Be(newValue);
    }

    [TestMethod]
    public void ViewInBrowser_NavigatesToTheCorrectUrl()
    {
        var urlToNavigate = "http://localhost";

        testSubject.ViewInBrowser(urlToNavigate);

        browserService.Received(1).Navigate(urlToNavigate);
    }

    [TestMethod]
    [DataRow(CredentialStoreType.Default)]
    [DataRow(CredentialStoreType.DPAPI)]
    public void Ctor_SetsCredentialStoreTypeToValueFromSettings(CredentialStoreType expectedType)
    {
        settings.CredentialStoreType.Returns(expectedType);
        _ = settings.Received().CredentialStoreType;
        testSubject.CredentialStoreType = expectedType;
        testSubject.CredentialStoreType.Should().Be(expectedType);
    }

    [TestMethod]
    [DataRow(CredentialStoreType.Default)]
    [DataRow(CredentialStoreType.DPAPI)]
    public void SaveSettings_SavesCredentialStoreTypeToSettings(CredentialStoreType expectedType)
    {
        testSubject.CredentialStoreType = expectedType;
        testSubject.SaveSettings();
        settings.Received().CredentialStoreType = expectedType;
    }
}
