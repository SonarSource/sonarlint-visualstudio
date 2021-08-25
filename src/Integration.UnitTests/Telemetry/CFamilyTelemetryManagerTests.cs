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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry
{
    [TestClass]
    public class CFamilyTelemetryManagerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CFamilyTelemetryManager, ICFamilyTelemetryManager>(null, new[]
            {
                MefTestHelpers.CreateExport<ICFamilyProjectTypeIndicator>(Mock.Of<ICFamilyProjectTypeIndicator>()),
                MefTestHelpers.CreateExport<ICompilationDatabaseLocator>(Mock.Of<ICompilationDatabaseLocator>()),
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(Mock.Of<IActiveSolutionTracker>()),
                MefTestHelpers.CreateExport<ITelemetryDataRepository>(Mock.Of<ITelemetryDataRepository>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Ctor_RegisterToSolutionChangedEvent()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(activeSolutionTracker.Object);

            activeSolutionTracker.VerifyAdd(x => x.ActiveSolutionChanged += It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSolutionChangedEvent()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var testSubject = CreateTestSubject(activeSolutionTracker.Object);

            activeSolutionTracker.VerifyRemove(x => x.ActiveSolutionChanged -= It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Never);

            testSubject.Dispose();

            activeSolutionTracker.VerifyRemove(x => x.ActiveSolutionChanged -= It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void OnSolutionChanged_NoOpenSolution_TelemetryNotChanged()
        {
            var projectTypeIndicator = new Mock<ICFamilyProjectTypeIndicator>();
            var compilationDatabaseLocator = new Mock<ICompilationDatabaseLocator>();
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                projectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: false);

            projectTypeIndicator.Invocations.Should().BeEmpty();
            compilationDatabaseLocator.Invocations.Should().BeEmpty();
            telemetryRepository.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void OnSolutionChanged_HasOpenSolution_ProjectIsNotCMake_TelemetryNotChanged()
        {
            var projectTypeIndicator = SetupProjectType(isCMake: false);
            var compilationDatabaseLocator = new Mock<ICompilationDatabaseLocator>();
            var telemetryRepository = SetupTelemetryRepository(new TelemetryData());
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                projectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            projectTypeIndicator.Verify(x=> x.IsCMake(), Times.Once);
            compilationDatabaseLocator.Invocations.Should().BeEmpty();

            telemetryRepository.VerifyGet(x=> x.Data);
            telemetryRepository.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_HasOpenSolution_ProjectIsCMake_Analyzable_NonAnalyzableFlagIsUnchanged(bool previousNonAnalyzableFlag)
        {
            var telemetryData = new TelemetryData
            {
                CFamilyProjectTypes = new CFamilyProjectTypes {IsCMakeNonAnalyzable = previousNonAnalyzableFlag, IsCMakeAnalyzable = false}
            };

            var projectTypeIndicator = SetupProjectType(isCMake: true);
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator(hasCompilationDatabase: true);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                projectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            projectTypeIndicator.Verify(x => x.IsCMake(), Times.Once);
            compilationDatabaseLocator.Verify(x=> x.Locate(), Times.Once);

            telemetryData.CFamilyProjectTypes.IsCMakeAnalyzable.Should().BeTrue();
            telemetryData.CFamilyProjectTypes.IsCMakeNonAnalyzable.Should().Be(previousNonAnalyzableFlag); // should not be changed
            telemetryRepository.Verify(x=> x.Save(), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_HasOpenSolution_ProjectIsCMake_NonAnalyzable_AnalyzableFlagIsUnchanged(bool previousAnalyzableFlag)
        {
            var telemetryData = new TelemetryData
            {
                CFamilyProjectTypes = new CFamilyProjectTypes { IsCMakeNonAnalyzable = false, IsCMakeAnalyzable = previousAnalyzableFlag }
            };

            var projectTypeIndicator = SetupProjectType(isCMake: true);
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator(hasCompilationDatabase: false);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                projectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            projectTypeIndicator.Verify(x => x.IsCMake(), Times.Once);
            compilationDatabaseLocator.Verify(x => x.Locate(), Times.Once);

            telemetryData.CFamilyProjectTypes.IsCMakeAnalyzable.Should().Be(previousAnalyzableFlag); // should not be changed
            telemetryData.CFamilyProjectTypes.IsCMakeNonAnalyzable.Should().BeTrue();
            telemetryRepository.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void OnSolutionChanged_NonCriticalException_ExceptionCaught()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var projectTypeIndicator = new Mock<ICFamilyProjectTypeIndicator>();
            projectTypeIndicator
                .Setup(x => x.IsCMake())
                .Throws<NotImplementedException>();

            CreateTestSubject(
                activeSolutionTracker.Object,
                projectTypeIndicator.Object);

            Action act = () => RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnSolutionChanged_CriticalException_ExceptionThrown()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var projectTypeIndicator = new Mock<ICFamilyProjectTypeIndicator>();
            projectTypeIndicator
                .Setup(x => x.IsCMake())
                .Throws<StackOverflowException>();

            CreateTestSubject(
                activeSolutionTracker.Object,
                projectTypeIndicator.Object);

            Action act = () => RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        private CFamilyTelemetryManager CreateTestSubject(
            IActiveSolutionTracker activeSolutionTracker,
            ICFamilyProjectTypeIndicator projectTypeIndicator = null,
            ICompilationDatabaseLocator compilationDatabaseLocator = null,
            ITelemetryDataRepository telemetryDataRepository = null)
        {
            projectTypeIndicator ??= Mock.Of<ICFamilyProjectTypeIndicator>();
            compilationDatabaseLocator ??= Mock.Of<ICompilationDatabaseLocator>();
            telemetryDataRepository ??= SetupTelemetryRepository(new TelemetryData()).Object;

            return new CFamilyTelemetryManager(projectTypeIndicator,
                compilationDatabaseLocator,
                activeSolutionTracker,
                telemetryDataRepository,
                Mock.Of<ILogger>());
        }

        private static Mock<IActiveSolutionTracker> SetupActiveSolutionTracker()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            activeSolutionTracker.SetupAdd(x => x.ActiveSolutionChanged += null);
            activeSolutionTracker.SetupRemove(x => x.ActiveSolutionChanged -= null);

            return activeSolutionTracker;
        }

        private void RaiseSolutionChangedEvent(Mock<IActiveSolutionTracker> activeSolutionTracker, bool isSolutionOpen)
        {
            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(isSolutionOpen));
        }

        private Mock<ICFamilyProjectTypeIndicator> SetupProjectType(bool isCMake)
        {
            var cFamilyProjectTypeIndicator = new Mock<ICFamilyProjectTypeIndicator>();

            cFamilyProjectTypeIndicator.Setup(x => x.IsCMake()).Returns(isCMake);

            return cFamilyProjectTypeIndicator;
        }

        private Mock<ICompilationDatabaseLocator> SetupCompilationDatabaseLocator(bool hasCompilationDatabase)
        {
            var compilationDatabaseLocator = new Mock<ICompilationDatabaseLocator>();

            var dummyLocation = hasCompilationDatabase ? "some location" : null;
            compilationDatabaseLocator.Setup(x => x.Locate()).Returns(dummyLocation);

            return compilationDatabaseLocator;
        }

        private Mock<ITelemetryDataRepository> SetupTelemetryRepository(TelemetryData data)
        {
           var telemetryRepository = new Mock<ITelemetryDataRepository>();

           telemetryRepository
               .SetupGet(x => x.Data)
               .Returns(data);

           return telemetryRepository;
        }
    }
}
