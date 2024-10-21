﻿/*
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
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MigrationCheckerTests
    {
        private static BoundSonarQubeProject AnyBoundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "any-key", "any-name");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MigrationChecker, MigrationChecker>(
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<IMefFactory>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IObsoleteConfigurationProvider>()); 
        }

        [TestMethod]
        public void CheckIsSingletonMefComponent()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<MigrationChecker>();
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone, SonarLintMode.Standalone, false)]
        [DataRow(SonarLintMode.Connected, SonarLintMode.Standalone, true)]
        [DataRow(SonarLintMode.LegacyConnected, SonarLintMode.Standalone, true)]
        [DataRow(SonarLintMode.Connected, SonarLintMode.Connected, true)]
        [DataRow(SonarLintMode.LegacyConnected, SonarLintMode.Connected, true)]
        [DataRow(null, SonarLintMode.Connected, false)]
        [DataRow(SonarLintMode.LegacyConnected, null, false)]
        public async Task Migrate_BindingGetsCalledWithCorrectCondition(SonarLintMode? obsoleteMode, SonarLintMode? mode, bool expectBindingToBeCalled)
        {
            var migrationPrompt = new Mock<IMigrationPrompt>();

            var configurationProvider = CreateNewConfigProvider(mode);
            var obsoleteConfigurationProvider = CreateObsoleteConfigProvider(obsoleteMode);

            var testSubject = CreateTestSubject(Mock.Of<IActiveSolutionTracker>(), migrationPrompt.Object, configurationProvider.Object, obsoleteConfigurationProvider.Object);
            await testSubject.DisplayMigrationPromptIfMigrationIsNeededAsync();

            migrationPrompt.Verify(x => x.ShowAsync(It.IsAny<BoundSonarQubeProject>(), mode == SonarLintMode.Connected), expectBindingToBeCalled ? Times.Once : Times.Never);
        }

        [TestMethod]
        public async Task Migrate_CorrectProjectPassed()
        {
            var migrationPrompt = new Mock<IMigrationPrompt>();

            var configurationProvider = CreateNewConfigProvider(SonarLintMode.Standalone);

            var obsoleteConfigurationProvider = new Mock<IObsoleteConfigurationProvider>();
            var oldConfiguration = CreateLegacyBindingConfiguration(SonarLintMode.Connected);
            obsoleteConfigurationProvider.Setup(x => x.GetConfiguration()).Returns(oldConfiguration);

            var testSubject = CreateTestSubject(Mock.Of<IActiveSolutionTracker>(), migrationPrompt.Object, configurationProvider.Object, obsoleteConfigurationProvider.Object);
            await testSubject.DisplayMigrationPromptIfMigrationIsNeededAsync();

            migrationPrompt.Verify(x => x.ShowAsync(oldConfiguration.Project, false), Times.Once);
        }
      
        [TestMethod]
        public void Ctor_SubscribeToSolutionChangedRaised_SolutionOpenedClose_MigrationPromptShowDisposeInvoked()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            var migrationPrompt = new Mock<IMigrationPrompt>();

            _ = CreateTestSubject(activeSolutionTracker.Object, migrationPrompt.Object);
            migrationPrompt.Invocations.Clear();

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true, "anysln"));
            migrationPrompt.Verify(x => x.ShowAsync(It.IsAny<BoundSonarQubeProject>(), false), Times.Once);

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(false, null));
            migrationPrompt.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task Dispose_UnsubscribeFromEvents_DisposeMigrationPrompt()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            var migrationPrompt = new Mock<IMigrationPrompt>();

            var testSubject = CreateTestSubject(activeSolutionTracker.Object, migrationPrompt.Object);
            await testSubject.DisplayMigrationPromptIfMigrationIsNeededAsync();
            testSubject.Dispose();
            migrationPrompt.Verify(x => x.Dispose(), Times.Once);

            migrationPrompt.Invocations.Clear();
            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, EventArgs.Empty);

            migrationPrompt.Verify(x => x.ShowAsync(AnyBoundProject, false), Times.Never);
        }

        private static Mock<IConfigurationProvider> CreateNewConfigProvider(SonarLintMode? sonarLintMode)
        {
            var provider = new Mock<IConfigurationProvider>();
            provider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(sonarLintMode));

            return provider;
        }

        private static Mock<IObsoleteConfigurationProvider> CreateObsoleteConfigProvider(SonarLintMode? sonarLintMode)
        {
            var provider = new Mock<IObsoleteConfigurationProvider>();
            provider.Setup(x => x.GetConfiguration()).Returns(CreateLegacyBindingConfiguration(sonarLintMode));

            return provider;
        }

        private static LegacyBindingConfiguration CreateLegacyBindingConfiguration(SonarLintMode? mode)
        {
            if (mode.HasValue)
            {
                return new LegacyBindingConfiguration(new BoundSonarQubeProject(new Uri("http://localhost"), "test", ""), mode.Value, "");
            }
            return null;
        }
        
        private static BindingConfiguration CreateBindingConfiguration(SonarLintMode? mode)
        {
            if (mode.HasValue)
            {
                return new BindingConfiguration(new BoundServerProject("solution", "project", new ServerConnection.SonarCloud("org")), mode.Value, "");
            }
            return null;
        }

        private static IMefFactory CreateMefFactory(IMigrationPrompt migrationPrompt = null)
        {
            var mefFactory = new Mock<IMefFactory>();
            mefFactory.Setup(x => x.CreateAsync<IMigrationPrompt>()).Returns(Task.FromResult(migrationPrompt));

            return mefFactory.Object;
        }

        private static MigrationChecker CreateTestSubject(IActiveSolutionTracker activeSolutionTracker = null, IMigrationPrompt migrationPromp = null, IConfigurationProvider configurationProvider = null, IObsoleteConfigurationProvider obsoleteConfigurationProvider = null)
        {
            activeSolutionTracker ??= Mock.Of<IActiveSolutionTracker>();
            var mefFactory = CreateMefFactory(migrationPromp);

            if (configurationProvider == null)
            {
                var configurationProviderMock = new Mock<IConfigurationProvider>();
                configurationProviderMock.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(SonarLintMode.Standalone));

                configurationProvider = configurationProviderMock.Object;
            }

            if (obsoleteConfigurationProvider == null)
            {
                var obsoleteConfigurationProviderMock = new Mock<IObsoleteConfigurationProvider>();
                obsoleteConfigurationProviderMock.Setup(x => x.GetConfiguration()).Returns(CreateLegacyBindingConfiguration(SonarLintMode.Connected));

                obsoleteConfigurationProvider = obsoleteConfigurationProviderMock.Object;
            }

            var testSubject = new MigrationChecker(activeSolutionTracker, mefFactory, configurationProvider, obsoleteConfigurationProvider);

            return testSubject;
        }
    }
}
