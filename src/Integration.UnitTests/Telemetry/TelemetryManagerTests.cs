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
        private Mock<ITimerFactory> timerFactoryMock;
        private Mock<IKnownUIContexts> knownUIContexts;

        [TestInitialize]
        public void TestInitialize()
        {
            activeSolutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            telemetryClientMock = new Mock<ITelemetryClient>();
            timerFactoryMock = new Mock<ITimerFactory>();
            knownUIContexts = new Mock<IKnownUIContexts>();
        }

        [TestMethod]
        public void Ctor_WhenGivenANullActiveSolutionBoundTracker_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(null, telemetryRepositoryMock.Object, telemetryClientMock.Object,
                timerFactoryMock.Object, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingTracker");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelmetryRepository_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, null, telemetryClientMock.Object,
                timerFactoryMock.Object, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryRepository");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelemetryClient_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object, null,
                timerFactoryMock.Object, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryClient");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTimerFactory_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                telemetryClientMock.Object, null, knownUIContexts.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("timerFactory");
        }

        [TestMethod]
        public void Ctor_InitialState_IsExpected()
        {
            // Arrange
            var timer1 = new Mock<ITimer>();
            var timer2 = new Mock<ITimer>();
            this.timerFactoryMock.Setup(x => x.Create())
                .Returns(new Queue<ITimer>(new[] { timer1.Object, timer2.Object }).Dequeue);
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(new TelemetryData());

            // Act
            var manager = CreateManager();

            // Assert
            this.timerFactoryMock.Verify(x => x.Create(), Times.Exactly(2));
            timer1.VerifySet(x => x.AutoReset = false, Times.Once);
            timer1.VerifySet(x => x.Interval = 1000 * 60 * 5, Times.Once);
            timer2.VerifySet(x => x.AutoReset = true, Times.Once);
            timer2.VerifySet(x => x.Interval = 1000 * 60 * 60 * 5, Times.Once);
        }

        [TestMethod]
        public void Ctor_WhenInstallationDateIsDateTimeMin_SetsCurrentDateAndSave()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTime.MinValue };
            this.timerFactoryMock.Setup(x => x.Create()).Returns(new TimerWrapper());
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
            this.timerFactoryMock.Setup(x => x.Create()).Returns(new TimerWrapper());
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
            var timer1 = new Mock<ITimer>();
            var timer2 = new Mock<ITimer>();
            this.timerFactoryMock.Setup(x => x.Create())
                .Returns(new Queue<ITimer>(new[] { timer1.Object, timer2.Object }).Dequeue);
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Act
            manager.OptIn();

            // Assert
            telemetryData.IsAnonymousDataShared.Should().BeTrue();
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            timer1.Verify(x => x.Start(), Times.Once);
            timer2.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        public void OptOut_SavesChoiceAndStopsTimersAndSendOptOutTelemetry()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTime.Now };
            var timer1 = new Mock<ITimer>();
            var timer2 = new Mock<ITimer>();
            this.timerFactoryMock.Setup(x => x.Create())
                .Returns(new Queue<ITimer>(new[] { timer1.Object, timer2.Object }).Dequeue);
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Act
            manager.OptOut();

            // Assert
            telemetryData.IsAnonymousDataShared.Should().BeFalse();
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            timer1.Verify(x => x.Stop(), Times.Once);
            timer2.Verify(x => x.Stop(), Times.Once);

            this.telemetryClientMock.Verify(x => x.OptOut(It.IsAny<TelemetryPayload>()), Times.Once);
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
            var firstCallDelayer = new Mock<ITimer>();
            var timer = new Mock<ITimer>();
            this.timerFactoryMock.Setup(x => x.Create())
                .Returns(new Queue<ITimer>(new[] { firstCallDelayer.Object, timer.Object }).Dequeue);
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Act
            firstCallDelayer.Raise(x => x.Elapsed += null, (EventArgs)null);

            // Assert
            telemetryData.LastUploadDate.Should().BeCloseTo(DateTime.Now, 200);
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            this.telemetryClientMock.Verify(x => x.SendPayload(It.IsAny<TelemetryPayload>()), Times.Once);
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
            var timer = new Mock<ITimer>();
            var tryUploadDataTimer = new Mock<ITimer>();
            this.timerFactoryMock.Setup(x => x.Create())
                .Returns(new Queue<ITimer>(new[] { timer.Object, tryUploadDataTimer.Object }).Dequeue);
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Act
            tryUploadDataTimer.Raise(x => x.Elapsed += null, (EventArgs)null);

            // Assert
            telemetryData.LastUploadDate.Should().BeCloseTo(DateTime.Now, CloseTimeThresholdInMilliseconds);
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            this.telemetryClientMock.Verify(x => x.SendPayload(It.IsAny<TelemetryPayload>()), Times.Once);
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
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            this.timerFactoryMock.Setup(x => x.Create()).Returns(new Mock<ITimer>().Object);
            var manager = CreateManager();

            // Act
            this.knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(true));

            // Assert
            telemetryData.LastSavedAnalysisDate.Should().BeCloseTo(DateTime.Now, CloseTimeThresholdInMilliseconds);
            telemetryData.NumberOfDaysOfUse.Should().Be(1);
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
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
            var date = DateTime.Now;
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTime.Now,
                LastSavedAnalysisDate = date
            };
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            this.timerFactoryMock.Setup(x => x.Create()).Returns(new Mock<ITimer>().Object);
            var manager = CreateManager();

            // Act
            this.knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(false));

            // Assert
            telemetryData.LastSavedAnalysisDate.Should().Be(date);
            telemetryData.NumberOfDaysOfUse.Should().Be(0);
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        private TelemetryManager CreateManager() => new TelemetryManager(activeSolutionTrackerMock.Object,
            telemetryRepositoryMock.Object, telemetryClientMock.Object, timerFactoryMock.Object, knownUIContexts.Object);
    }
}
