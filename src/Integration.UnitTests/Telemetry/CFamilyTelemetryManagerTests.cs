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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry
{
    [TestClass]
    public class CFamilyTelemetryManagerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CFamilyTelemetryManager, ICFamilyTelemetryManager>(
                MefTestHelpers.CreateExport<ICMakeProjectTypeIndicator>(),
                MefTestHelpers.CreateExport<ICompilationDatabaseLocator>(),
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<ITelemetryDataRepository>(),
                MefTestHelpers.CreateExport<IVcxProjectTypeIndicator>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Ctor_RegisterToSolutionEvents()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(activeSolutionTracker.Object);

            activeSolutionTracker.VerifyAdd(x => x.ActiveSolutionChanged += It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSolutionEvents()
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
            var cmakeProjectTypeIndicator = new Mock<ICMakeProjectTypeIndicator>();
            var compilationDatabaseLocator = new Mock<ICompilationDatabaseLocator>();
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: false);

            cmakeProjectTypeIndicator.Invocations.Should().BeEmpty();
            compilationDatabaseLocator.Invocations.Should().BeEmpty();
            telemetryRepository.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void OnSolutionChanged_HasOpenSolution_ProjectIsNotCMake_TelemetryNotChanged()
        {
            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: false);
            var compilationDatabaseLocator = new Mock<ICompilationDatabaseLocator>();
            var telemetryRepository = SetupTelemetryRepository(new TelemetryData());
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            cmakeProjectTypeIndicator.Verify(x=> x.IsCMake(), Times.Once);
            compilationDatabaseLocator.Invocations.Should().BeEmpty();

            telemetryRepository.Verify(x=> x.Save(), Times.Never);
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

            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: true);
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator(hasCompilationDatabase: true);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            cmakeProjectTypeIndicator.Verify(x => x.IsCMake(), Times.Once);
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

            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: true);
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator(hasCompilationDatabase: false);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            cmakeProjectTypeIndicator.Verify(x => x.IsCMake(), Times.Once);
            compilationDatabaseLocator.Verify(x => x.Locate(), Times.Once);

            telemetryData.CFamilyProjectTypes.IsCMakeAnalyzable.Should().Be(previousAnalyzableFlag); // should not be changed
            telemetryData.CFamilyProjectTypes.IsCMakeNonAnalyzable.Should().BeTrue();
            telemetryRepository.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_HasOpenSolution_ProjectIsCMake_VcxProjectTelemetryIsIgnored(bool isCMakeAnalyzable)
        {
            var telemetryData = new TelemetryData {CFamilyProjectTypes = new CFamilyProjectTypes()};

            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: true);
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator(isCMakeAnalyzable);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var vcxProjectTypeIndicator = new Mock<IVcxProjectTypeIndicator>();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                compilationDatabaseLocator.Object,
                telemetryRepository.Object,
                vcxProjectTypeIndicator.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            cmakeProjectTypeIndicator.Verify(x => x.IsCMake(), Times.Once);

            vcxProjectTypeIndicator.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_HasOpenSolution_VcxProject_NonAnalyzable_AnalyzableFlagIsUnchanged(bool previousAnalyzableFlag)
        {
            var telemetryData = new TelemetryData
            {
                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsVcxNonAnalyzable = false,
                    IsVcxAnalyzable = previousAnalyzableFlag
                }
            };

            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: false);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var vcxProjectTypeIndicator = new Mock<IVcxProjectTypeIndicator>();
            vcxProjectTypeIndicator
                .Setup(x => x.GetProjectTypes())
                .Returns(new VcxProjectTypesResult
                {
                    HasAnalyzableVcxProjects = false,
                    HasNonAnalyzableVcxProjects = true
                });

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                telemetryDataRepository: telemetryRepository.Object,
                vcxProjectTypeIndicator: vcxProjectTypeIndicator.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            telemetryData.CFamilyProjectTypes.IsVcxAnalyzable.Should().Be(previousAnalyzableFlag); // should not be changed
            telemetryData.CFamilyProjectTypes.IsVcxNonAnalyzable.Should().BeTrue();
            telemetryRepository.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_HasOpenSolution_VcxProject_Analyzable_NonAnalyzableFlagIsUnchanged(bool previousNonAnalyzableFlag)
        {
            var telemetryData = new TelemetryData
            {
                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsVcxNonAnalyzable = previousNonAnalyzableFlag,
                    IsVcxAnalyzable = false
                }
            };

            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: false);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var vcxProjectTypeIndicator = new Mock<IVcxProjectTypeIndicator>();
            vcxProjectTypeIndicator
                .Setup(x => x.GetProjectTypes())
                .Returns(new VcxProjectTypesResult
                {
                    HasAnalyzableVcxProjects = true,
                    HasNonAnalyzableVcxProjects = false
                });

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                telemetryDataRepository: telemetryRepository.Object,
                vcxProjectTypeIndicator: vcxProjectTypeIndicator.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            telemetryData.CFamilyProjectTypes.IsVcxAnalyzable.Should().BeTrue(); 
            telemetryData.CFamilyProjectTypes.IsVcxNonAnalyzable.Should().Be(previousNonAnalyzableFlag); // should not be changed
            telemetryRepository.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void OnSolutionChanged_HasOpenSolution_VcxProject_HasBothAnalyzableAndNonAnalyzable_BothFlagsChanged()
        {
            var telemetryData = new TelemetryData
            {
                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsVcxNonAnalyzable = false,
                    IsVcxAnalyzable = false
                }
            };

            var cmakeProjectTypeIndicator = SetupCMakeProjectIndicator(isCMake: false);
            var telemetryRepository = SetupTelemetryRepository(telemetryData);
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var vcxProjectTypeIndicator = new Mock<IVcxProjectTypeIndicator>();
            vcxProjectTypeIndicator
                .Setup(x => x.GetProjectTypes())
                .Returns(new VcxProjectTypesResult
                {
                    HasAnalyzableVcxProjects = true,
                    HasNonAnalyzableVcxProjects = true
                });

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object,
                telemetryDataRepository: telemetryRepository.Object,
                vcxProjectTypeIndicator: vcxProjectTypeIndicator.Object);

            RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);

            telemetryData.CFamilyProjectTypes.IsVcxAnalyzable.Should().BeTrue();
            telemetryData.CFamilyProjectTypes.IsVcxNonAnalyzable.Should().BeTrue();
            telemetryRepository.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        public void OnSolutionChanged_NonCriticalException_ExceptionCaught()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var cmakeProjectTypeIndicator = new Mock<ICMakeProjectTypeIndicator>();
            cmakeProjectTypeIndicator
                .Setup(x => x.IsCMake())
                .Throws<NotImplementedException>();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object);

            Action act = () => RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnSolutionChanged_CriticalException_ExceptionThrown()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var cmakeProjectTypeIndicator = new Mock<ICMakeProjectTypeIndicator>();
            cmakeProjectTypeIndicator
                .Setup(x => x.IsCMake())
                .Throws<StackOverflowException>();

            CreateTestSubject(
                activeSolutionTracker.Object,
                cmakeProjectTypeIndicator.Object);

            Action act = () => RaiseSolutionChangedEvent(activeSolutionTracker, isSolutionOpen: true);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        private CFamilyTelemetryManager CreateTestSubject(
            IActiveSolutionTracker activeSolutionTracker,
            ICMakeProjectTypeIndicator cmakeProjectTypeIndicator = null,
            ICompilationDatabaseLocator compilationDatabaseLocator = null,
            ITelemetryDataRepository telemetryDataRepository = null,
            IVcxProjectTypeIndicator vcxProjectTypeIndicator = null)
        {
            cmakeProjectTypeIndicator ??= Mock.Of<ICMakeProjectTypeIndicator>();
            compilationDatabaseLocator ??= Mock.Of<ICompilationDatabaseLocator>();
            telemetryDataRepository ??= SetupTelemetryRepository(new TelemetryData()).Object;

            return new CFamilyTelemetryManager(cmakeProjectTypeIndicator,
                compilationDatabaseLocator,
                activeSolutionTracker,
                telemetryDataRepository,
                vcxProjectTypeIndicator,
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

        private Mock<ICMakeProjectTypeIndicator> SetupCMakeProjectIndicator(bool isCMake)
        {
            var cmakeProjectTypeIndicator = new Mock<ICMakeProjectTypeIndicator>();

            cmakeProjectTypeIndicator.Setup(x => x.IsCMake()).Returns(isCMake);

            return cmakeProjectTypeIndicator;
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
