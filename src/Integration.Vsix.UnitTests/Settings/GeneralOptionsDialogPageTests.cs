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
using System.ComponentModel;
using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    /// <summary>
    /// Based on https://github.com/microsoft/vssdktestfx/blob/main/doc/mstest.md
    /// </summary>
    [TestClass]
    public class VsThreadingFixer
    {
        internal static GlobalServiceProvider MockServiceProvider { get; private set; }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            MockServiceProvider = new GlobalServiceProvider();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            MockServiceProvider.Dispose();
        }
    }

    [TestClass]
    // [Ignore("ThreadHelper - needs fix up after VSSDK package update")]
    public class GeneralOptionsDialogPageTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            VsThreadingFixer.MockServiceProvider.Reset();
        }

        private class GeneralOptionsDialogPageTestable : GeneralOptionsDialogPage
        {
            public GeneralOptionsDialogControl Control => Child as GeneralOptionsDialogControl;

            public void ActivateAccessor()
            {
                _ = Child; // touch the property to make sure any initialization has been done
                base.OnActivate(new CancelEventArgs());
            }

            public void ApplyAccessor(ApplyKind applyBehavior)
            {
                base.OnApply(new PageApplyEventArgs { ApplyBehavior = applyBehavior });
            }
        }

        [TestMethod]
        public void OnActivate_ControlsAreConfigured()
        {
            var settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
            };

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings);

            // Act
            page.ActivateAccessor();

            // Assert
            page.Control.Should().NotBeNull();
            page.Control.DaemonVerbosity.SelectedItem.Should().Be(DaemonLogLevel.Verbose);
            page.Control.VerbosityPanel.Visibility.Should().Be(Visibility.Visible);
        }

        [TestMethod]
        public void OnApply_Cancel_SettingsAreNotUpdated()
        {
            var settings = new ConfigurableSonarLintSettings()
            {
                DaemonLogLevel = DaemonLogLevel.Verbose,
                IsActivateMoreEnabled = true,
            };

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings);
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
            };

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, settings);
            page.ActivateAccessor();

            page.Control.DaemonVerbosity.SelectedItem = DaemonLogLevel.Minimal;

            // Act
            page.ApplyAccessor(Microsoft.VisualStudio.Shell.DialogPage.ApplyKind.Apply);

            // Assert
            settings.DaemonLogLevel.Should().Be(DaemonLogLevel.Minimal);
        }

        [TestMethod]
        public void ClickHyperlink_ShowWikiCommandIsCalled()
        {
            var browserService = new Mock<IVsBrowserService>();

            GeneralOptionsDialogPageTestable page = new GeneralOptionsDialogPageTestable();
            ConfigureSiteMock(page, vsBrowserService: browserService.Object);

            page.Control.ShowWikiHyperLink.Command.Should().NotBeNull();

            // Act
            page.Control.ShowWikiHyperLink.Command.Execute(null);

            // Assert
            browserService.Verify(x => x.Navigate("https://github.com/SonarSource/sonarlint-visualstudio/wiki"), Times.Once);
        }

        private static void ConfigureSiteMock(GeneralOptionsDialogPage testSubject,
            ISonarLintSettings settings = null,
            IVsBrowserService vsBrowserService = null)
        {
            settings ??= new ConfigurableSonarLintSettings();
            vsBrowserService ??= new Mock<IVsBrowserService>().Object;

            var mefHostMock = new Mock<IComponentModel>();
            mefHostMock.Setup(m => m.GetExtensions<ISonarLintSettings>()).Returns(() => new[] { settings });
            mefHostMock.Setup(m => m.GetExtensions<ILogger>()).Returns(() => new[] { new TestLogger() });
            mefHostMock.Setup(m => m.GetExtensions<IUserSettingsProvider>()).Returns(() => new[] { new Mock<IUserSettingsProvider>().Object });
            mefHostMock.Setup(m => m.GetExtensions<IVsBrowserService>()).Returns(() => new[] { vsBrowserService });

            var siteMock = new Mock<ISite>();
            siteMock.As<IServiceProvider>().Setup(m => m.GetService(It.Is<Type>(t => t == typeof(SComponentModel)))).Returns(mefHostMock.Object);

            testSubject.Site = siteMock.Object;
        }
    }
}
