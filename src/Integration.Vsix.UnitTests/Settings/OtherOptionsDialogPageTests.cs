/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using static Microsoft.VisualStudio.Shell.DialogPage;

namespace SonarLint.VisualStudio.Integration.Vsix.Tests
{
    [TestClass]
    public class OtherOptionsDialogPageTests
    {
        private class OtherOptionsDialogPageTestable : OtherOptionsDialogPage
        {
            public OtherOptionsDialogControl Control => Child as OtherOptionsDialogControl;

            public void Activate()
            {
                _ = Child; // touch the property to make sure any initialization has been done
                base.OnActivate(new CancelEventArgs());
            }

            public void Apply(ApplyKind applyBehavior)
            {
                base.OnApply(new PageApplyEventArgs { ApplyBehavior = applyBehavior });
            }
        }

        [TestMethod]
        public void OnActivate_WhenDataIsShared_CheckBoxIsChecked()
        {
            // Arrange
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.IsAnonymousDataShared).Returns(true);

            var optionsPage = CreateTestSubject(telemetryManager.Object);

            // Act
            optionsPage.Activate();

            // Assert
            optionsPage.Control.Should().NotBeNull();
            optionsPage.Control.ShareAnonymousData.IsChecked.Should().BeTrue();
        }

        [TestMethod]
        public void OnActivate_WhenDataIsNotShared_CheckBoxIsUnchecked()
        {
            // Arrange
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.IsAnonymousDataShared).Returns(false);
            var optionsPage = CreateTestSubject(telemetryManager.Object);

            // Act
            optionsPage.Activate();

            // Assert
            optionsPage.Control.Should().NotBeNull();
            optionsPage.Control.ShareAnonymousData.IsChecked.Should().BeFalse();
        }

        [TestMethod]
        public void OnApply_WhenApplyBehaviorIsCancel_DoesNotCallOptInOrOptOut()
        {
            // Arrange
            var isOptInOrOptOutCalled = false;
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.OptIn()).Callback(() => isOptInOrOptOutCalled = true);
            telemetryManager.Setup(x => x.OptOut()).Callback(() => isOptInOrOptOutCalled = true);
            var optionsPage = CreateTestSubject(telemetryManager.Object);

            // Act
            optionsPage.Apply(ApplyKind.Cancel);

            // Assert
            isOptInOrOptOutCalled.Should().BeFalse();
        }

        [TestMethod]
        public void OnApply_WhenApplyBehaviorIsCancelNoNavigate_DoesNotCallOptInOrOptOut()
        {
            // Arrange
            var isOptInOrOptOutCalled = false;
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.OptIn()).Callback(() => isOptInOrOptOutCalled = true);
            telemetryManager.Setup(x => x.OptOut()).Callback(() => isOptInOrOptOutCalled = true);
            var optionsPage = CreateTestSubject(telemetryManager.Object);

            // Act
            optionsPage.Apply(ApplyKind.CancelNoNavigate);

            // Assert
            isOptInOrOptOutCalled.Should().BeFalse();
        }

        [TestMethod]
        public void OnApply_WhenApplyBehaviorIsApplyAndShareDataStaysFalse_DoesNotCallOptInOrOptOut()
        {
            // Arrange
            var isOptInOrOptOutCalled = false;
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.IsAnonymousDataShared).Returns(false);
            telemetryManager.Setup(x => x.OptIn()).Callback(() => isOptInOrOptOutCalled = true);
            telemetryManager.Setup(x => x.OptOut()).Callback(() => isOptInOrOptOutCalled = true);

            var optionsPage = CreateTestSubject(telemetryManager.Object);
            optionsPage.Control.ShareAnonymousData.IsChecked = false;

            // Act
            optionsPage.Apply(ApplyKind.Apply);

            // Assert
            isOptInOrOptOutCalled.Should().BeFalse();
        }

        [TestMethod]
        public void OnApply_WhenApplyBehaviorIsApplyAndShareDataStaysTrue_DoesNotCallOptInOrOptOut()
        {
            // Arrange
            var isOptInOrOptOutCalled = false;
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.IsAnonymousDataShared).Returns(true);
            telemetryManager.Setup(x => x.OptIn()).Callback(() => isOptInOrOptOutCalled = true);
            telemetryManager.Setup(x => x.OptOut()).Callback(() => isOptInOrOptOutCalled = true);

            var optionsPage = CreateTestSubject(telemetryManager.Object);
            optionsPage.Control.ShareAnonymousData.IsChecked = true;

            // Act
            optionsPage.Apply(ApplyKind.Apply);

            // Assert
            isOptInOrOptOutCalled.Should().BeFalse();
        }

        [TestMethod]
        public void OnApply_WhenApplyBehaviorIsApplyAndShareDataBecomesTrue_OnlyCallsOptIn()
        {
            // Arrange
            var isOptInCalled = false;
            var isOptOutCalled = false;
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.IsAnonymousDataShared).Returns(false);
            telemetryManager.Setup(x => x.OptIn()).Callback(() => isOptInCalled = true);
            telemetryManager.Setup(x => x.OptOut()).Callback(() => isOptOutCalled = true);

            var optionsPage = CreateTestSubject(telemetryManager.Object);
            optionsPage.Control.ShareAnonymousData.IsChecked = true;

            // Act
            optionsPage.Apply(ApplyKind.Apply);

            // Assert
            isOptInCalled.Should().BeTrue();
            isOptOutCalled.Should().BeFalse();
        }

        [TestMethod]
        public void OnApply_WhenApplyBehaviorIsApplyAndShareDataBecomesFalse_OnlyCallsOptOut()
        {
            // Arrange
            var isOptInCalled = false;
            var isOptOutCalled = false;
            var telemetryManager = new Mock<ITelemetryManager>();
            telemetryManager.Setup(x => x.IsAnonymousDataShared).Returns(true);
            telemetryManager.Setup(x => x.OptIn()).Callback(() => isOptInCalled = true);
            telemetryManager.Setup(x => x.OptOut()).Callback(() => isOptOutCalled = true);

            var optionsPage = CreateTestSubject(telemetryManager.Object);
            optionsPage.Control.ShareAnonymousData.IsChecked = false;

            // Act
            optionsPage.Apply(ApplyKind.Apply);

            // Assert
            isOptInCalled.Should().BeFalse();
            isOptOutCalled.Should().BeTrue();
        }

        private OtherOptionsDialogPageTestable CreateTestSubject(ITelemetryManager telemetryManager)
        {
            var testSubject = new OtherOptionsDialogPageTestable();

            // The telemetry manager is a MEF component, so the page obtains it by
            // using the "Site" property as service provider to get the IComponentModel MEF service.
            // The MEF service then returns the telemetry manager.
            Mock<ISite> mockSite = new Mock<ISite>();

            Mock<IComponentModel> mockMefHost = new Mock<IComponentModel>();
            mockMefHost.Setup(m => m.GetExtensions<ITelemetryManager>()).Returns(new[] { telemetryManager });

            mockSite.As<IServiceProvider>().Setup(m => m.GetService(It.Is<Type>(t => t == typeof(SComponentModel)))).Returns(mockMefHost.Object);

            mockSite.Object.GetMefService<ITelemetryManager>().Should().NotBeNull(); // sanity check of the test setup

            testSubject.Site = mockSite.Object;
            return testSubject;
        }
    }
}
