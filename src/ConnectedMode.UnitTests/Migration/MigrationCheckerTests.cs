/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MigrationCheckerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MigrationChecker, MigrationChecker>(
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<IMigrationPrompt>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IObsoleteConfigurationProvider>()); 
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone, SonarLintMode.Standalone, false)]
        [DataRow(SonarLintMode.Connected, SonarLintMode.Standalone, true)]
        [DataRow(SonarLintMode.LegacyConnected, SonarLintMode.Standalone, true)]
        [DataRow(SonarLintMode.Connected, SonarLintMode.Connected, false)]
        [DataRow(SonarLintMode.LegacyConnected, SonarLintMode.Connected, false)]
        public void Migrate_BindingGetsCalledWithCorrectCondition(SonarLintMode obsoleteMode, SonarLintMode mode, bool expectBindingToBeCalled)
        {
            var migrationPrompt = new Mock<IMigrationPrompt>();

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(mode));

            var obsoleteConfigurationProvider = new Mock<IObsoleteConfigurationProvider>();
            obsoleteConfigurationProvider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(obsoleteMode));

            _ = new MigrationChecker(Mock.Of<IActiveSolutionTracker>(), migrationPrompt.Object, configurationProvider.Object, obsoleteConfigurationProvider.Object);

            migrationPrompt.Verify(x => x.ShowAsync(), expectBindingToBeCalled ? Times.Once : Times.Never);
        }

        [TestMethod]
        public void Ctor_SubscribeToSolutionChangedRaised_SolutionOpenedCLose_MigrationPromptShowClearInvoked()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            var migrationPrompt = new Mock<IMigrationPrompt>();

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(SonarLintMode.Standalone));

            var obsoleteConfigurationProvider = new Mock<IObsoleteConfigurationProvider>();
            obsoleteConfigurationProvider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(SonarLintMode.Connected));

            new MigrationChecker(activeSolutionTracker.Object, migrationPrompt.Object, configurationProvider.Object, obsoleteConfigurationProvider.Object);
            migrationPrompt.Invocations.Clear();

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));
            migrationPrompt.Verify(x => x.ShowAsync(), Times.Once);

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(false));
            migrationPrompt.Verify(x => x.Clear(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribeFromEvents()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            var migrationPrompt = new Mock<IMigrationPrompt>();
        
            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(SonarLintMode.Standalone));

            var obsoleteConfigurationProvider = new Mock<IObsoleteConfigurationProvider>();
            obsoleteConfigurationProvider.Setup(x => x.GetConfiguration()).Returns(CreateBindingConfiguration(SonarLintMode.Connected));


            var testSubject = new MigrationChecker(activeSolutionTracker.Object, migrationPrompt.Object, configurationProvider.Object, obsoleteConfigurationProvider.Object);
            testSubject.Dispose();
            migrationPrompt.Invocations.Clear();

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, EventArgs.Empty);

            migrationPrompt.Verify(x => x.ShowAsync(), Times.Never);
        }

        private BindingConfiguration CreateBindingConfiguration(SonarLintMode mode)
        {
            return new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://localhost"), "test", ""), mode, "");
        }
    }
}
