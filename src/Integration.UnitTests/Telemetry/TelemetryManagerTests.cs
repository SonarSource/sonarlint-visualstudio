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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryManagerTests
    {
        private const int CloseTimeThresholdInMilliseconds = 10000;

        // Fixed time to use in tests in which the specific time is not checked or manipulated
        private readonly DateTimeOffset AnyValidTime = new DateTimeOffset(2020, 06, 04, 11, 01, 02, TimeSpan.FromHours(1));

        private Mock<ITelemetryDataRepository> telemetryRepositoryMock;
        private Mock<ITelemetryClient> telemetryClientMock;
        private Mock<ILogger> loggerMock;
        private Mock<ITelemetryTimer> telemetryTimerMock;
        private Mock<IKnownUIContexts> knownUIContexts;
        private Mock<IUserSettingsProvider> userSettingsProvider;
        private Mock<ITelemetryPayloadCreator> payloadCreatorMock;
        private Mock<ICurrentTimeProvider> currentTimeProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            loggerMock = new Mock<ILogger>();
            telemetryClientMock = new Mock<ITelemetryClient>();
            telemetryTimerMock = new Mock<ITelemetryTimer>();
            knownUIContexts = new Mock<IKnownUIContexts>();
            userSettingsProvider = new Mock<IUserSettingsProvider>();
            payloadCreatorMock = new Mock<ITelemetryPayloadCreator>();
            currentTimeProvider = new Mock<ICurrentTimeProvider>();

            currentTimeProvider.Setup(mock => mock.Now).Returns(DateTime.Now);
            SetupUserSettingsProvider(new RulesSettings());
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
                    new Analysis {Language = "csharp"}
                },
                ShowHotspot = new ShowHotspot
                {
                    NumberOfRequests = 123
                },
                TaintVulnerabilities = new TaintVulnerabilities
                {
                    NumberOfIssuesInvestigatedRemotely = 777,
                    NumberOfIssuesInvestigatedLocally = 888
                },
                ServerNotifications = new ServerNotifications
                {
                    IsDisabled = true,
                    ServerNotificationCounters = new Dictionary<string, ServerNotificationCounter>
                    {
                        {
                            "QUALITY_GATE", new ServerNotificationCounter
                            {
                                ReceivedCount = 11,
                                ClickedCount = 22
                            }
                        },
                        {
                            "NEW_ISSUES", new ServerNotificationCounter
                            {
                                ReceivedCount = 33,
                                ClickedCount = 44
                            }
                        }
                    }
                },
                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsCMakeAnalyzable = true,
                    IsCMakeNonAnalyzable = true,
                    IsVcxNonAnalyzable = true,
                    IsVcxAnalyzable = true
                },
                RulesUsage = new RulesUsage
                {
                    DisabledByDefaultThatWereEnabled = new List<string> { "rule1", "rule2" },
                    EnabledByDefaultThatWereDisabled = new List<string> { "rule3", "rule4" },
                    RulesThatRaisedIssues = new List<string> { "rule5", "rule6" },
                    RulesWithAppliedQuickFixes = new List<string> { "rule7", "rule8" }
                }
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var manager = CreateManager();
            var now = DateTimeOffset.Now;

            // Act
            TriggerTimerElapsed(now);

            // Assert
            telemetryData.LastUploadDate.Should().Be(now);
            telemetryData.Analyses.Count.Should().Be(0); // should have cleared the list of installed languages
            telemetryData.ShowHotspot.NumberOfRequests.Should().Be(0); // should have cleared the counter
            telemetryData.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(0); // should have cleared the counter
            telemetryData.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(0); // should have cleared the counter
            telemetryData.ServerNotifications.IsDisabled.Should().BeFalse(); // should have cleared the value
            telemetryData.ServerNotifications.ServerNotificationCounters.Should().BeEmpty(); // should have cleared the value
            telemetryData.CFamilyProjectTypes.IsCMakeNonAnalyzable.Should().BeFalse(); // should have cleared the value
            telemetryData.CFamilyProjectTypes.IsCMakeAnalyzable.Should().BeFalse(); // should have cleared the value
            telemetryData.CFamilyProjectTypes.IsVcxAnalyzable.Should().BeFalse(); // should have cleared the value
            telemetryData.CFamilyProjectTypes.IsVcxNonAnalyzable.Should().BeFalse(); // should have cleared the value
            telemetryData.RulesUsage.DisabledByDefaultThatWereEnabled.Should().BeEmpty(); // should have cleared the value
            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEmpty(); // should have cleared the value
            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeEmpty(); // should have cleared the value
            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEmpty(); // should have cleared the value
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

        private TelemetryManager CreateManager(ICurrentTimeProvider mockTimeProvider = null) =>
            new(telemetryRepositoryMock.Object, userSettingsProvider.Object, payloadCreatorMock.Object, loggerMock.Object, telemetryClientMock.Object,
            telemetryTimerMock.Object, knownUIContexts.Object, mockTimeProvider ?? currentTimeProvider.Object);

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

        [TestMethod]
        public void Sequence_InstallFollowedByMultipleUpdates()
        {
            // Arrange
            var telemetryData = new TelemetryData { InstallationDate = DateTimeOffset.MinValue };

            var installationTime = DateTimeOffset.Parse("2020-05-31T09:30:00.000+00:00");
            var mockTimeProvider = new Mock<ICurrentTimeProvider>();
            mockTimeProvider.Setup(x => x.LocalTimeZone).Returns(TimeZoneInfo.Utc);
            resetMocks(installationTime);

            // 1. Create -> initial values set
            var testSubject = CreateManager(mockTimeProvider.Object);

            telemetryData.NumberOfDaysOfUse.Should().Be(0);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            // 2. First update on day 1 -> days of use updated and saved
            var firstUpdateTime = installationTime.AddMinutes(1);
            resetMocks(firstUpdateTime);

            testSubject.Update();

            telemetryData.NumberOfDaysOfUse.Should().Be(1);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            // 3. Second update on day 1 -> no change to days of use
            var secondUpdateTime = installationTime.AddMinutes(2);
            resetMocks(secondUpdateTime);

            testSubject.Update();

            telemetryData.NumberOfDaysOfUse.Should().Be(1);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Never);

            // 4. First update on day 2 -> days of use updated and saved
            var secondDay_firstUpdateTime = installationTime.AddDays(1);
            resetMocks(secondDay_firstUpdateTime);

            testSubject.Update();

            telemetryData.NumberOfDaysOfUse.Should().Be(2);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);

            // 5. Second update on day 2 -> no change to days of use
            var secondDay_secondUpdateTime = secondDay_firstUpdateTime.AddMinutes(1);
            resetMocks(secondDay_secondUpdateTime);

            testSubject.Update();

            telemetryData.NumberOfDaysOfUse.Should().Be(2);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Never);

            void resetMocks(DateTimeOffset newNow)
            {
                mockTimeProvider.Setup(x => x.Now).Returns(newNow);
                telemetryRepositoryMock.Reset();
                telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            }
        }

        [TestMethod]
        [DataRow("2020-05-31T20:30:00.000+00:00", "2020-06-01T06:30:00.000+08:00", 8, false)] // regression test for #1434
        [DataRow("2020-06-01T04:30:00.000+00:00", "2020-06-01T06:30:00.000+00:00", 0, false)] // same day -> don't update
        [DataRow("2020-05-31T20:30:00.000+00:00", "2020-06-01T06:30:00.000+00:00", 0, true)] // different day -> update
        [DataRow("2020-06-01T04:30:00.000+00:00", "2020-08-23T02:30:00.000+08:00", 8, true)] // many days later -> update
        public void Update_IfNotSameDayAsLastUpdate_DaysOfUseIsIncremented(string lastSavedDate, string now, int utcOffset, bool expectedIncrement)
        {
            var lastSavedTimeUTC = DateTimeOffset.Parse(lastSavedDate);
            var nowDateTimeOffset = DateTimeOffset.Parse(now);
            const int initialDaysOfUse = 2;

            // Arrange
            var telemetryData = new TelemetryData
            {
                InstallationDate = lastSavedTimeUTC.AddDays(-100), // anything, as long as it's earlier
                LastSavedAnalysisDate = lastSavedTimeUTC,
                NumberOfDaysOfUse = initialDaysOfUse
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var mockTimeProvider = CreateMockTimeProvider(nowDateTimeOffset, utcOffset);

            var testSubject = CreateManager(mockTimeProvider.Object);

            // Act
            testSubject.Update();

            // Assert
            mockTimeProvider.Verify(x => x.Now, Times.Once);
            mockTimeProvider.Verify(x => x.LocalTimeZone, Times.Once);

            if (expectedIncrement)
            {
                telemetryData.LastSavedAnalysisDate.Should().Be(nowDateTimeOffset);
                telemetryData.NumberOfDaysOfUse.Should().Be(initialDaysOfUse + 1);
                this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
            }
            else
            {
                telemetryData.LastSavedAnalysisDate.Should().Be(lastSavedTimeUTC);
                telemetryData.NumberOfDaysOfUse.Should().Be(initialDaysOfUse);
                this.telemetryRepositoryMock.Verify(x => x.Save(), Times.Never);
            }
        }


        [TestMethod]
        [DataRow(0, 0, 1)] // first check on day of initial installation -> increment days of use
        [DataRow(0, 1, 1)] // second and subsequent check on day of installation -> can't be incremented above 1
        [DataRow(4, 4, 5)] // used every day since installation, first check on day 5 -> incremented
        [DataRow(4, 5, 5)] // used every day since installation, second and subsequent checks on day 5 -> not incremented further
        [DataRow(1001, 100, 101)] // not used every day -> incremented and saved
        [DataRow(1, 100, 2)] // bad data - should be capped at (days since installation + 1)
        [DataRow(999, 1001, 1000)] // bad data - should be capped at (days since installation + 1)
        public void Update_DaysOfUseIsCorrectlyCalculated(int daysSinceInstallation, int daysOfUse,
            int expectedDaysOfUse)
        {
            var currentTimeUTC = DateTimeOffset.UtcNow;
            var installationDate = currentTimeUTC.AddDays(-(daysSinceInstallation));
            var lastSavedTimeUTC = currentTimeUTC.AddDays(-1); // last saved yesterday so update is triggered

            // Arrange
            var telemetryData = new TelemetryData
            {
                InstallationDate = installationDate,
                LastSavedAnalysisDate = lastSavedTimeUTC,
                NumberOfDaysOfUse = daysOfUse
            };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            var mockTimeProvider = CreateMockTimeProvider(currentTimeUTC, 0);

            var testSubject = CreateManager(mockTimeProvider.Object);

            // Act
            testSubject.Update();

            telemetryData.NumberOfDaysOfUse.Should().Be(expectedDaysOfUse);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(100)]
        public void ShowHotspotRequested_CounterIncremented(int previousCounter)
        {
            var telemetryData = new TelemetryData { ShowHotspot = new ShowHotspot { NumberOfRequests = previousCounter } };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var testSubject = CreateManager();

            telemetryRepositoryMock.Reset();
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            testSubject.ShowHotspotRequested();

            telemetryData.ShowHotspot.NumberOfRequests.Should().Be(previousCounter + 1);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(100)]
        public void TaintIssueInvestigatedLocally_CounterIncremented(int previousCounter)
        {
            var telemetryData = new TelemetryData { TaintVulnerabilities = new TaintVulnerabilities { NumberOfIssuesInvestigatedLocally = previousCounter } };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var testSubject = CreateManager();

            telemetryRepositoryMock.Reset();
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            testSubject.TaintIssueInvestigatedLocally();

            telemetryData.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(previousCounter + 1);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(100)]
        public void TaintIssueInvestigatedRemotely_CounterIncremented(int previousCounter)
        {
            var telemetryData = new TelemetryData { TaintVulnerabilities = new TaintVulnerabilities { NumberOfIssuesInvestigatedRemotely = previousCounter } };
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);
            var testSubject = CreateManager();

            telemetryRepositoryMock.Reset();
            telemetryRepositoryMock.Setup(x => x.Data).Returns(telemetryData);

            testSubject.TaintIssueInvestigatedRemotely();

            telemetryData.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(previousCounter + 1);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void SendPayload_DisabledSecretRulesAreUpdated()
        {
            var ruleSettings = new RulesSettings();
            ruleSettings.Rules.Add("rule1", new RuleConfig { Level = RuleLevel.Off }); // wrong repo
            ruleSettings.Rules.Add("secret:rule2", new RuleConfig { Level = RuleLevel.Off }); // wrong repo
            ruleSettings.Rules.Add("secrets:rule3", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("Secrets:rule4", new RuleConfig { Level = RuleLevel.Off }); // wrong case
            ruleSettings.Rules.Add("SECRETS:rule5", new RuleConfig { Level = RuleLevel.Off }); // wrong case
            ruleSettings.Rules.Add("secrets:rule6", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:rule7", new RuleConfig { Level = RuleLevel.On }); // enabled

            SetupUserSettingsProvider(ruleSettings);

            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            var payload = new TelemetryPayload();

            payloadCreatorMock.Setup(x => x.Create(
                    It.Is((TelemetryData data) =>
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Count == 2 &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Contains("secrets:rule3") &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Contains("secrets:rule6"))))
                .Returns(payload);

            TriggerTimerElapsed();

            payloadCreatorMock.VerifyAll();
            telemetryClientMock.Verify(x => x.SendPayloadAsync(payload), Times.Once);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void SendPayload_PreviousRulesAreOverriden()
        {
            var ruleSettings = new RulesSettings();
            ruleSettings.Rules.Add("secrets:rule1", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:rule2", new RuleConfig { Level = RuleLevel.On });
            ruleSettings.Rules.Add("secrets:rule3", new RuleConfig { Level = RuleLevel.Off });

            SetupUserSettingsProvider(ruleSettings);

            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled = new List<string> { "secrets:rule4", "secrets:rule5" };

            CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            var payload = new TelemetryPayload();

            payloadCreatorMock.Setup(x => x.Create(
                    It.Is((TelemetryData data) =>
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Count == 2 &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Contains("secrets:rule1") &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Contains("secrets:rule3"))))
                .Returns(payload);

            TriggerTimerElapsed();

            payloadCreatorMock.VerifyAll();
            telemetryClientMock.Verify(x => x.SendPayloadAsync(payload), Times.Once);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void SendPayload_ListIsOrdered()
        {
            var ruleSettings = new RulesSettings();
            ruleSettings.Rules.Add("secrets:cccc", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:bbbb", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:aaaa456", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:aaaa123", new RuleConfig { Level = RuleLevel.Off });

            SetupUserSettingsProvider(ruleSettings);

            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            var payload = new TelemetryPayload();

            payloadCreatorMock.Setup(x => x.Create(
                    It.Is((TelemetryData data) =>
                        data.RulesUsage.EnabledByDefaultThatWereDisabled.Count == 4 &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled[0] == "secrets:aaaa123" &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled[1] == "secrets:aaaa456" &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled[2] == "secrets:bbbb" &&
                        data.RulesUsage.EnabledByDefaultThatWereDisabled[3] == "secrets:cccc")))
                .Returns(payload);

            TriggerTimerElapsed();

            payloadCreatorMock.VerifyAll();
            telemetryClientMock.Verify(x => x.SendPayloadAsync(payload), Times.Once);
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void QuickFixApplied_NoPreviousIssues_TelemetryUpdated()
        {
            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            var testSubject = CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            testSubject.QuickFixApplied("rule1");

            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule1");
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void QuickFixApplied_HasPreviousIssues_TelemetryUpdated()
        {
            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            var testSubject = CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            testSubject.QuickFixApplied("rule1");
            testSubject.QuickFixApplied("rule2");
            testSubject.QuickFixApplied("rule3");

            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule1", "rule2", "rule3");
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(3));
        }

        [TestMethod]
        public void QuickFixApplied_DuplicatedIssuesAreIgnored()
        {
            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            var testSubject = CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            testSubject.QuickFixApplied("rule1");
            testSubject.QuickFixApplied("rule1");
            testSubject.QuickFixApplied("rule1");

            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule1");

            // should be called only for the first time
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(1));
        }

        [TestMethod]
        public void QuickFixApplied_RuleAlreadyInTheList_TelemetryNotUpdated()
        {
            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            var testSubject = CreateManager();

            telemetryRepositoryMock.Invocations.Clear();

            testSubject.QuickFixApplied("rule1");

            var oldList = telemetryData.RulesUsage.RulesWithAppliedQuickFixes;

            testSubject.QuickFixApplied("rule1");

            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeSameAs(oldList);
            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule1");

            // should be called only for the first time
            telemetryRepositoryMock.Verify(x => x.Save(), Times.Exactly(1));
        }

        [TestMethod]
        public void QuickFixApplied_ListIsOrdered()
        {
            var telemetryData = CreateRulesUsageTelemetryData();
            SetupTelemetryRepository(telemetryData);

            var testSubject = CreateManager();

            testSubject.QuickFixApplied("cccc");
            testSubject.QuickFixApplied("bbbb");
            testSubject.QuickFixApplied("aaaa:456");
            testSubject.QuickFixApplied("aaaa:123");

            telemetryData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo(
                "aaaa:123",
                "aaaa:456",
                "bbbb",
                "cccc");
        }

        private TelemetryData CreateRulesUsageTelemetryData() =>
            new()
            {
                RulesUsage = new RulesUsage(),
                IsAnonymousDataShared = true
            };

        private void SetupUserSettingsProvider(RulesSettings ruleSettings)
        {
            userSettingsProvider.Setup(x => x.UserSettings).Returns(new UserSettings(ruleSettings));
        }

        private void SetupTelemetryRepository(TelemetryData data)
        {
            telemetryRepositoryMock.SetupGet(x => x.Data).Returns(data);
        }

        private static Mock<ICurrentTimeProvider> CreateMockTimeProvider(DateTimeOffset now, int timeZoneOffsetFromUTC)
        {
            var localTimeZone = TimeZoneInfo.CreateCustomTimeZone("test", TimeSpan.FromHours(timeZoneOffsetFromUTC), "test", "test");

            var mock = new Mock<ICurrentTimeProvider>();
            mock.Setup(x => x.Now).Returns(now);
            mock.Setup(x => x.LocalTimeZone).Returns(localTimeZone);
            return mock;
        }

        private void TriggerTimerElapsed(DateTimeOffset? dateTimeOffset = null)
        {
            dateTimeOffset ??= DateTimeOffset.Now;

            telemetryTimerMock.Raise(x => x.Elapsed += null, new TelemetryTimerEventArgs(dateTimeOffset.Value));
        }
    }
}
