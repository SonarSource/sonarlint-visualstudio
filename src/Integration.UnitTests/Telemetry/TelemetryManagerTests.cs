/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryManagerTests
    {
        private const int CloseTimeThresholdInMilliseconds = 10000;

        private Mock<IActiveSolutionBoundTracker> activeSolutionTrackerMock;
        private Mock<ITelemetryDataRepository> telemetryRepositoryMock;
        private Mock<ITelemetryClient> telemetryClientMock;
        private Mock<ITelemetryTimer> telemetryTimerMock;
        private Mock<IKnownUIContexts> knownUIContexts;

        [TestInitialize]
        public void TestInitialize()
        {
            activeSolutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            telemetryClientMock = new Mock<ITelemetryClient>();
            telemetryTimerMock = new Mock<ITelemetryTimer>();
            knownUIContexts = new Mock<IKnownUIContexts>();
        }

        [TestMethod]
        public void Ctor_WhenGivenANullActiveSolutionBoundTracker_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(null, telemetryRepositoryMock.Object, telemetryClientMock.Object,
                telemetryTimerMock.Object, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingTracker");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelmetryRepository_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, null, telemetryClientMock.Object,
                telemetryTimerMock.Object, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryRepository");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelemetryClient_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object, null,
                telemetryTimerMock.Object, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryClient");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelemetryTimer_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                telemetryClientMock.Object, null, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryTimer");
        }

        [TestMethod]
        public void Ctor_WhenInstallationDateIsDateTimeMin_SetsCurrentDateAndSave()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTime.MinValue };
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            // Act
            CreateManager();

            // Assert
            telemetryData.InstallationDate.Should().BeAfter(DateTime.Now.AddMinutes(-5));
            telemetryData.InstallationDate.Should().BeBefore(DateTime.Now);
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void IsAnonymousDataShared_ReturnsValueFromRepository()
        {
            // Arrange
            this.telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { InstallationDate = DateTime.Now, IsAnonymousDataShared = true });
            var manager = CreateManager();

            // Act
            var result = manager.IsAnonymousDataShared;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void OptIn_SavesChoiceAndStartsTimers()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTime.Now };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();

            // Act
            manager.OptIn();

            // Assert
            telemetryData.IsAnonymousDataShared.Should().BeTrue();

            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            telemetryTimerMock.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        public void OptOut_SavesChoiceAndStopsTimersAndSendOptOutTelemetry()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTime.Now };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();

            // Act
            manager.OptOut();

            // Assert
            telemetryData.IsAnonymousDataShared.Should().BeFalse();

            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            telemetryTimerMock.Verify(x => x.Stop(), Times.Once);

            telemetryClientMock.Verify(x => x.OptOut(It.IsAny<TelemetryPayload>()), Times.Once);
        }

        [TestMethod]
        public void WhenFirstCallDelayerAndNewDay_ChangeLastUploadAndSaveAndSendPayload()
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTime.Now,
                LastUploadDate = DateTime.Now.AddDays(-1)
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();
            var now = DateTime.Now;

            // Act
            telemetryTimerMock.Raise(x => x.Elapsed += null, new TelemetryTimerEventArgs(now));

            // Assert
            telemetryData.LastUploadDate.Should().Be(now);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            telemetryClientMock.Verify(x => x.SendPayload(It.IsAny<TelemetryPayload>()), Times.Once);
        }

        [TestMethod]
        public void WhenTryUploadDataTimerElapsedAndNewDay_ChangeLastUploadAndSaveAndSendPayload()
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTime.Now,
                LastUploadDate = DateTime.Now.AddDays(-1)
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();
            var now = DateTime.Now;

            // Act
            telemetryTimerMock.Raise(x => x.Elapsed += null, new TelemetryTimerEventArgs(now));

            // Assert
            telemetryData.LastUploadDate.Should().Be(now);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            telemetryClientMock.Verify(x => x.SendPayload(It.IsAny<TelemetryPayload>()), Times.Once);
        }

        [TestMethod]
        public void WhenSolutionBuildingContextChangedAndNewDay_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTime.Now.AddDays(-1),
                x => x.SolutionBuildingContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionExistsAndFullyLoadedContextChangedAndNewDay_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTime.Now.AddDays(-1),
                x => x.SolutionExistsAndFullyLoadedContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionBuildingContextChangedAndDateTimeMinValue_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTime.MinValue, x => x.SolutionBuildingContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionExistsAndFullyLoadedContextChangedAndDateTimeMinValue_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTime.MinValue, x => x.SolutionExistsAndFullyLoadedContextChanged += null);
        }

        private void WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTime lastSavedAnalysisDate, Action<IKnownUIContexts> eventExpression)
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTime.Now,
                LastSavedAnalysisDate = lastSavedAnalysisDate
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();

            // Act
            knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(true));

            // Assert
            telemetryData.LastSavedAnalysisDate.Should().BeCloseTo(DateTime.Now, CloseTimeThresholdInMilliseconds);
            telemetryData.NumberOfDaysOfUse.Should().Be(1);

            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void WhenSolutionBuildingContextChangedAndNotActivate_DoNothing()
        {
            WhenEventAndNotActivate_DoNothing(x => x.SolutionBuildingContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionExistsAndFullyLoadedContextChangedAndNotActivate_DoNothing()
        {
            WhenEventAndNotActivate_DoNothing(x => x.SolutionExistsAndFullyLoadedContextChanged += null);
        }

        private void WhenEventAndNotActivate_DoNothing(Action<IKnownUIContexts> eventExpression)
        {
            // Arrange
            var now = DateTime.Now;
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTime.Now,
                LastSavedAnalysisDate = now
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Act
            knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(false));

            // Assert
            telemetryData.LastSavedAnalysisDate.Should().Be(now);
            telemetryData.NumberOfDaysOfUse.Should().Be(0);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        private TelemetryManager CreateManager() => new TelemetryManager(activeSolutionTrackerMock.Object,
            telemetryRepositoryMock.Object, telemetryClientMock.Object, telemetryTimerMock.Object, knownUIContexts.Object);
    }
}
