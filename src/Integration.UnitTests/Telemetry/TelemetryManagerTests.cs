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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryManagerTests
    {
        private const int CloseTimeThresholdInMilliseconds = 10000;

        // Fixed time to use in tests in which the specific time is not checked or manipulated
        private readonly DateTimeOffset AnyValidTime = new DateTimeOffset(2020, 06, 04, 11, 01, 02, TimeSpan.FromHours(1));

        private Mock<IActiveSolutionBoundTracker> activeSolutionTrackerMock;
        private Mock<ITelemetryDataRepository> telemetryRepositoryMock;
        private Mock<ITelemetryClient> telemetryClientMock;
        private Mock<ILogger> loggerMock;
        private Mock<ITelemetryTimer> telemetryTimerMock;
        private Mock<IKnownUIContexts> knownUIContexts;
        private ICurrentTimeProvider currentTimeProvider = DefaultCurrentTimeProvider.Instance;

        [TestInitialize]
        public void TestInitialize()
        {
            activeSolutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            loggerMock = new Mock<ILogger>();
            telemetryClientMock = new Mock<ITelemetryClient>();
            telemetryTimerMock = new Mock<ITelemetryTimer>();
            knownUIContexts = new Mock<IKnownUIContexts>();

            activeSolutionTrackerMock.Setup(x => x.CurrentConfiguration).Returns(BindingConfiguration.Standalone);
        }

        [TestMethod]
        public void Ctor_WhenGivenANullActiveSolutionBoundTracker_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(null, telemetryRepositoryMock.Object, loggerMock.Object,
                telemetryClientMock.Object, telemetryTimerMock.Object, knownUIContexts.Object, currentTimeProvider);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingTracker");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelmetryRepository_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, null, loggerMock.Object,
                telemetryClientMock.Object, telemetryTimerMock.Object, knownUIContexts.Object, currentTimeProvider);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("telemetryRepository");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullLogger_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object, null,
                telemetryClientMock.Object, telemetryTimerMock.Object, knownUIContexts.Object, currentTimeProvider);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelemetryClient_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                loggerMock.Object, null, telemetryTimerMock.Object, knownUIContexts.Object, currentTimeProvider);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("telemetryClient");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelemetryTimer_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                loggerMock.Object, telemetryClientMock.Object, null, knownUIContexts.Object, currentTimeProvider);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("telemetryTimer");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullKnownUIContexts_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                loggerMock.Object, telemetryClientMock.Object, telemetryTimerMock.Object, null, currentTimeProvider);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("knownUIContexts");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullCurrentTimeProvider_ThrowsArgumentNullException()
        {
            // Act
            Action action = () => new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                loggerMock.Object, telemetryClientMock.Object, telemetryTimerMock.Object, knownUIContexts.Object, null);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("currentTimeProvider");
        }

        [TestMethod]
        public void Ctor_WhenInstallationDateIsDateTimeMin_SetsCurrentDateAndSave()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTimeOffset.MinValue };
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            // Act
            CreateManager();

            // Assert
            telemetryData.InstallationDate.Should().BeAfter(DateTimeOffset.Now.AddMinutes(-5));
            telemetryData.InstallationDate.Should().BeOnOrBefore(DateTimeOffset.Now);
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void IsAnonymousDataShared_ReturnsValueFromRepository()
        {
            // Arrange
            this.telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { InstallationDate = AnyValidTime, IsAnonymousDataShared = true });
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
            var telemetryData = new TelemetryData { InstallationDate = AnyValidTime };
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
            var telemetryData = new TelemetryData { InstallationDate = AnyValidTime };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();

            // Act
            manager.OptOut();

            // Assert
            telemetryData.IsAnonymousDataShared.Should().BeFalse();

            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            telemetryTimerMock.Verify(x => x.Stop(), Times.Once);

            telemetryClientMock.Verify(x => x.OptOutAsync(It.IsAny<TelemetryPayload>()), Times.Once);
        }

        [TestMethod]
        public void WhenFirstCallDelayerAndNewDay_ChangeLastUploadAndSaveAndSendPayload()
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTimeOffset.Now,
                LastUploadDate = DateTimeOffset.Now.AddDays(-1),
                Analyses = new System.Collections.Generic.List<Analysis>()
                {
                    new Analysis { Language = "csharp" }
                }
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();
            var now = DateTimeOffset.Now;

            // Act
            telemetryTimerMock.Raise(x => x.Elapsed += null, new TelemetryTimerEventArgs(now));

            // Assert
            telemetryData.LastUploadDate.Should().Be(now);
            telemetryData.Analyses.Count.Should().Be(0); // should have cleared the list of installed languages
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            telemetryClientMock.Verify(x => x.SendPayloadAsync(It.IsAny<TelemetryPayload>()), Times.Once);
        }

        [TestMethod]
        public void WhenTryUploadDataTimerElapsedAndNewDay_ChangeLastUploadAndSaveAndSendPayload()
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTimeOffset.Now,
                LastUploadDate = DateTimeOffset.Now.AddDays(-1),
                Analyses = new System.Collections.Generic.List<Analysis>()
                {
                    new Analysis { Language = "csharp" }
                }
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();
            var now = DateTimeOffset.Now;

            // Act
            telemetryTimerMock.Raise(x => x.Elapsed += null, new TelemetryTimerEventArgs(now));

            // Assert
            telemetryData.LastUploadDate.Should().Be(now);
            telemetryData.Analyses.Count.Should().Be(0); // should have cleared the list of installed languages
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            telemetryClientMock.Verify(x => x.SendPayloadAsync(It.IsAny<TelemetryPayload>()), Times.Once);
        }

        [TestMethod]
        public void WhenSolutionBuildingContextChangedAndNewDay_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTimeOffset.Now.AddDays(-1),
                x => x.SolutionBuildingContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionExistsAndFullyLoadedContextChangedAndNewDay_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTimeOffset.Now.AddDays(-1),
                x => x.SolutionExistsAndFullyLoadedContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionBuildingContextChangedAndDateTimeMinValue_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTimeOffset.MinValue, x => x.SolutionBuildingContextChanged += null);
        }

        [TestMethod]
        public void WhenSolutionExistsAndFullyLoadedContextChangedAndDateTimeMinValue_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave()
        {
            WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTimeOffset.MinValue, x => x.SolutionExistsAndFullyLoadedContextChanged += null);
        }

        private void WhenUIContextsEventAndGivenLastDate_ChangeLastAnalysisDateAndUpdateDaysOfUseAndSave(DateTimeOffset lastSavedAnalysisDate, Action<IKnownUIContexts> eventExpression)
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTimeOffset.Now,
                LastSavedAnalysisDate = lastSavedAnalysisDate
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();

            // Act
            knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(true));

            // Assert
            telemetryData.LastSavedAnalysisDate.Should().BeCloseTo(DateTimeOffset.Now, CloseTimeThresholdInMilliseconds);
            telemetryData.NumberOfDaysOfUse.Should().Be(1);

            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void WhenCSharpOrVBProjectContextChangedAndActive_LanguagesUpdated()
        {
            // Arrange
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = AnyValidTime,
                LastSavedAnalysisDate = AnyValidTime
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();

            // 1. CSharp context changed
            RaiseEventAndCheckResult(SonarLanguageKeys.CSharp, x => x.CSharpProjectContextChanged += null);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            // 2. ... and again
            RaiseEventAndCheckResult(SonarLanguageKeys.CSharp, x => x.CSharpProjectContextChanged += null);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once); // still only saved once

            // 3. VB context changed
            RaiseEventAndCheckResult(SonarLanguageKeys.VBNet, x => x.VBProjectContextChanged += null);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(2)); // new languages, so saved again

            // 4. ... and again
            RaiseEventAndCheckResult(SonarLanguageKeys.VBNet, x => x.VBProjectContextChanged += null);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(2)); // still only saved twice

            telemetryData.Analyses.Count.Should().Be(2); // check the collection of data is cumulative

            void RaiseEventAndCheckResult(string expectedLanguageKey, Action<IKnownUIContexts> eventExpression)
            {
                knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(true));

                // Assert
                var matches = telemetryData.Analyses.Count(x => string.Equals(x.Language, expectedLanguageKey, StringComparison.OrdinalIgnoreCase));
                matches.Should().Be(1);
            }
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

        [TestMethod]
        public void WhenCSharpContextChangedAndNotActivate_DoNothing()
        {
            WhenEventAndNotActivate_DoNothing(x => x.CSharpProjectContextChanged += null);
        }

        [TestMethod]
        public void WhenVBContextChangedAndNotActivate_DoNothing()
        {
            WhenEventAndNotActivate_DoNothing(x => x.VBProjectContextChanged += null);
        }

        private void WhenEventAndNotActivate_DoNothing(Action<IKnownUIContexts> eventExpression)
        {
            // Arrange
            var now = DateTimeOffset.Now;
            var telemetryData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                InstallationDate = DateTimeOffset.Now,
                LastSavedAnalysisDate = now
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Act
            knownUIContexts.Raise(eventExpression, new UIContextChangedEventArgs(false));

            // Assert
            telemetryData.LastSavedAnalysisDate.Should().Be(now);
            telemetryData.NumberOfDaysOfUse.Should().Be(0);
            telemetryData.Analyses.Count.Should().Be(0);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        private TelemetryManager CreateManager() => new TelemetryManager(activeSolutionTrackerMock.Object,
            telemetryRepositoryMock.Object, loggerMock.Object, telemetryClientMock.Object,
            telemetryTimerMock.Object, knownUIContexts.Object, currentTimeProvider);

        #region Languages analyzed tests

        [TestMethod]
        public void LanguageAnalyzed_RepositoryOnlySavedWhenNewLanguage()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTimeOffset.MinValue };
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var manager = CreateManager();

            // Set up the telemetry mock after creating the manager, since the manager will
            // may saved data on initial creation
            this.telemetryRepositoryMock.Reset();
            this.telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            // Act

            // 1. New language analyzed for the first time -> should be saved in repo
            manager.LanguageAnalyzed("cpp");

            CheckExpectedLanguages(telemetryData, "cpp");
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(1)); // Saved

            // 2. New language analyzed for the first time -> should be saved in repo
            manager.LanguageAnalyzed("js");

            CheckExpectedLanguages(telemetryData, "cpp", "js");
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(2)); // Saved

            // 3. Repeat a language -> not saved
            manager.LanguageAnalyzed("JS");

            CheckExpectedLanguages(telemetryData, "cpp", "js");
            this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(2)); // Saved
        }

        private static void CheckExpectedLanguages(TelemetryData data, params string[] expectedLanguageKeys)
        {
            var actualLanguageKeys = data.Analyses.Select(a => a.Language).ToList();
            actualLanguageKeys.Should().BeEquivalentTo(expectedLanguageKeys);
        }

        #endregion
    }
}
