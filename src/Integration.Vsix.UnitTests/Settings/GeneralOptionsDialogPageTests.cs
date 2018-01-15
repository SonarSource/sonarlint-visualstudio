/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class GeneralOptionsDialogPageTests
    {
        private class GeneralOptionsDialogPageTestable : GeneralOptionsDialogPage
        {
            public GeneralOptionsDialogControl Control => Child as GeneralOptionsDialogControl;

            public void ActivateAccessor()
            {
                var forceInitialization = Child;
                base.OnActivate(new CancelEventArgs());
            }

            public void ApplyAccessor(ApplyKind applyBehavior)
            {
                base.OnApply(new PageApplyEventArgs { ApplyBehavior = applyBehavior });
            }
        }

        [TestMethod]
        public void OnActivate_ControlsAreConfiguredFromSettings1()
        {
            var settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                ShowServerNuGetTrustWarning = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();
            
            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object);

            // Act
            page.ActivateAccessor();

            // Assert
            page.Control.Should().NotBeNull();
            page.Control.ShowServerNuGetTrustWarning.IsChecked.Should().Be(true);
            page.Control.DaemonVerbosity.SelectedItem.Should().Be(DaemonLogLevel.Verbose);
        }

        [TestMethod]
        public void OnActivate_ControlsAreConfiguredFromSettings2()
        {
            var settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Info,
                IsActivateMoreEnabled = true,
                ShowServerNuGetTrustWarning = false,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object);

            // Act
            page.ActivateAccessor();

            // Assert
            page.Control.Should().NotBeNull();
            page.Control.ShowServerNuGetTrustWarning.IsChecked.Should().Be(false);
            page.Control.DaemonVerbosity.SelectedItem.Should().Be(DaemonLogLevel.Info);
        }

        [TestMethod]
        public void OnApply_Cancel_SettingsAreNotUpdated()
        {
            var settings = new ConfigurableSonarLintSettings()
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                ShowServerNuGetTrustWarning = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object);
            page.ActivateAccessor();

            page.Control.ShowServerNuGetTrustWarning.IsChecked = false;
            page.Control.DaemonVerbosity.SelectedItem = DaemonLogLevel.Minimal;

            // Act
            page.ApplyAccessor(Microsoft.VisualStudio.Shell.DialogPage.ApplyKind.Cancel);

            // Assert
            settings.ShowServerNuGetTrustWarning.Should().BeTrue();
            settings.DaemonLogLevel.Should().Be(DaemonLogLevel.Verbose);
        }

        [TestMethod]
        public void OnApply_Save_SettingsAreUpdated()
        {
            var settings = new ConfigurableSonarLintSettings()
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                ShowServerNuGetTrustWarning = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object);
            page.ActivateAccessor();

            page.Control.ShowServerNuGetTrustWarning.IsChecked = false;
            page.Control.DaemonVerbosity.SelectedItem = DaemonLogLevel.Minimal;

            // Act
            page.ApplyAccessor(Microsoft.VisualStudio.Shell.DialogPage.ApplyKind.Apply);

            // Assert
            settings.ShowServerNuGetTrustWarning.Should().BeFalse();
            settings.DaemonLogLevel.Should().Be(DaemonLogLevel.Minimal);
        }

        private static void ConfigureSiteMock(GeneralOptionsDialogPage testSubject, ISonarLintSettings settings, ISonarLintDaemon daemon)
        {
            var mefHostMock = new Mock<IComponentModel>();
            mefHostMock.Setup(m => m.GetExtensions<ISonarLintDaemon>()).Returns(() => new[] { daemon });
            mefHostMock.Setup(m => m.GetExtensions<ISonarLintSettings>()).Returns(() => new[] { settings });

            var siteMock = new Mock<ISite>();
            siteMock.As<IServiceProvider>().Setup(m => m.GetService(It.Is<Type>(t => t == typeof(SComponentModel)))).Returns(mefHostMock.Object);

            testSubject.Site = siteMock.Object;
        }
    }
}
