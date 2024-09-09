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
    private const string JreLocation = "C:/jrePath";

    [TestInitialize]
    public void TestInitialize()
    {
        settings = Substitute.For<ISonarLintSettings>();
        openSettingsFileCommand = Substitute.For<ICommand>();
        browserService = Substitute.For<IBrowserService>();
        testSubject = new GeneralOptionsDialogControlViewModel(settings, browserService, openSettingsFileCommand);
    }

    [TestMethod]
    public void Ctor_OpenSettingsFileCommandNull_ThrowsException()
    {
        Action act = () => _ = new GeneralOptionsDialogControlViewModel(settings, browserService, null);
        act.Should().Throw<ArgumentNullException>(nameof(openSettingsFileCommand));
    }

    [TestMethod]
    public void Ctor_BrowserServiceNull_ThrowsException()
    {
        Action act = () => _ = new GeneralOptionsDialogControlViewModel(settings, null, openSettingsFileCommand);
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
    public void ClickHyperlink_ShowWikiCommandIsCalled()
    {
        testSubject.ShowWikiCommand.Should().NotBeNull();

        testSubject.ShowWikiCommand.Execute(null);

        browserService.Received(1).Navigate(DocumentationLinks.DisablingARule);
    }
}
