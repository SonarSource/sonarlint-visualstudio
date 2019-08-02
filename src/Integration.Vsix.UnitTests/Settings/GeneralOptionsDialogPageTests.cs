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
using System.Windows;
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
        public void OnActivate_WhenDaemonIsNotInstalled_ControlsAreConfiguredForActivation()
        {
            // Daemon is not installed. However, that should not affect the activation
            // status of the controls.
            var settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();
            var installerMock = new Mock<IDaemonInstaller>();
            installerMock.Setup<bool>(x => x.IsInstalled()).Returns(false);

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object, installerMock.Object);

            // Act
            page.ActivateAccessor();

            // Assert
            page.Control.Should().NotBeNull();
            page.Control.DaemonVerbosity.SelectedItem.Should().Be(DaemonLogLevel.Verbose);

            // User has enabled activation; the activation status of the daemon should be irrelevant
            page.Control.DeactivateButton.Visibility.Should().Be(Visibility.Visible);
            page.Control.DeactivateText.Visibility.Should().Be(Visibility.Visible);
            page.Control.VerbosityPanel.Visibility.Should().Be(Visibility.Visible);

            // ... and activate options should be visible
            page.Control.ActivateButton.Visibility.Should().Be(Visibility.Collapsed);
            page.Control.ActivateText.Visibility.Should().Be(Visibility.Collapsed);
        }


        [TestMethod]
        public void OnActivate_WhenDaemonIsInstalled_ControlsAreConfiguredFromSettings1()
        {
            // Daemon is installed so the settings as supplied should be used
            var settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();
            var installerMock = new Mock<IDaemonInstaller>();
            installerMock.Setup<bool>(x => x.IsInstalled()).Returns(true);

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object, installerMock.Object);

            // Act
            page.ActivateAccessor();

            // Assert
            page.Control.Should().NotBeNull();
            page.Control.DaemonVerbosity.SelectedItem.Should().Be(DaemonLogLevel.Verbose);

            // Daemon is activate, so deactivate options should be visible
            page.Control.DeactivateButton.Visibility.Should().Be(Visibility.Visible);
            page.Control.DeactivateText.Visibility.Should().Be(Visibility.Visible);
            page.Control.VerbosityPanel.Visibility.Should().Be(Visibility.Visible);

            // ... and active options should not
            page.Control.ActivateButton.Visibility.Should().Be(Visibility.Collapsed);
            page.Control.ActivateText.Visibility.Should().Be(Visibility.Collapsed);
        }

        [TestMethod]
        public void OnActivate_WhenDaemonIsInstalled_ControlsAreConfiguredFromSettings2()
        {
            // Daemon is installed so the settings as supplied should be used
            var settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Info,
                IsActivateMoreEnabled = false,
                SkipActivateMoreDialog = false
            };

            var daemonMock = new Mock<ISonarLintDaemon>();
            var installerMock = new Mock<IDaemonInstaller>();
            installerMock.Setup<bool>(x => x.IsInstalled()).Returns(true);

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object, installerMock.Object);

            // Act
            page.ActivateAccessor();

            // Assert
            page.Control.Should().NotBeNull();
            page.Control.DaemonVerbosity.SelectedItem.Should().Be(DaemonLogLevel.Info);

            // Daemon is inactive, so deactivate options should be collapsed
            page.Control.DeactivateButton.Visibility.Should().Be(Visibility.Collapsed);
            page.Control.DeactivateText.Visibility.Should().Be(Visibility.Collapsed);
            page.Control.VerbosityPanel.Visibility.Should().Be(Visibility.Collapsed);

            // ... and activate options should be visible
            page.Control.ActivateButton.Visibility.Should().Be(Visibility.Visible);
            page.Control.ActivateText.Visibility.Should().Be(Visibility.Visible);
        }

        [TestMethod]
        public void OnApply_Cancel_SettingsAreNotUpdated()
        {
            var settings = new ConfigurableSonarLintSettings()
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();
            var installerMock = new Mock<IDaemonInstaller>();

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object, installerMock.Object);
            page.ActivateAccessor();

            page.Control.DaemonVerbosity.SelectedItem = DaemonLogLevel.Minimal;

            // Act
            page.ApplyAccessor(Microsoft.VisualStudio.Shell.DialogPage.ApplyKind.Cancel);

            // Assert
            settings.DaemonLogLevel.Should().Be(DaemonLogLevel.Verbose);
        }

        [TestMethod]
        public void OnApply_Save_SettingsAreUpdated()
        {
            var settings = new ConfigurableSonarLintSettings()
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
                SkipActivateMoreDialog = true
            };

            var daemonMock = new Mock<ISonarLintDaemon>();
            var installerMock = new Mock<IDaemonInstaller>();

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings, daemonMock.Object, installerMock.Object);
            page.ActivateAccessor();

            page.Control.DaemonVerbosity.SelectedItem = DaemonLogLevel.Minimal;

            // Act
            page.ApplyAccessor(Microsoft.VisualStudio.Shell.DialogPage.ApplyKind.Apply);

            // Assert
            settings.DaemonLogLevel.Should().Be(DaemonLogLevel.Minimal);
        }

        private static void ConfigureSiteMock(GeneralOptionsDialogPage testSubject, ISonarLintSettings settings, ISonarLintDaemon daemon, IDaemonInstaller installer)
        {
            var mefHostMock = new Mock<IComponentModel>();
            mefHostMock.Setup(m => m.GetExtensions<ISonarLintDaemon>()).Returns(() => new[] { daemon });
            mefHostMock.Setup(m => m.GetExtensions<IDaemonInstaller>()).Returns(() => new[] { installer });
            mefHostMock.Setup(m => m.GetExtensions<ISonarLintSettings>()).Returns(() => new[] { settings });
            mefHostMock.Setup(m => m.GetExtensions<ILogger>()).Returns(() => new[] { new TestLogger() });

            var siteMock = new Mock<ISite>();
            siteMock.As<IServiceProvider>().Setup(m => m.GetService(It.Is<Type>(t => t == typeof(SComponentModel)))).Returns(mefHostMock.Object);

            testSubject.Site = siteMock.Object;
        }
    }
}
