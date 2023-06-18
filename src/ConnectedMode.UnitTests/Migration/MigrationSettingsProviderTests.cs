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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MigrationSettingsProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MigrationSettingsProvider, IMigrationSettingsProvider>(
                MefTestHelpers.CreateExport<IServiceProvider>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<MigrationSettingsProvider>();

        [TestMethod]
        public async Task Get_ReturnsExpectedValue()
        {
            var solution = CreateIVsSolution("c:\\rootfolder");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = await testSubject.GetAsync("slvs_samples_bound_vs2019");
            actual.Should().NotBeNull();
            actual.LegacySonarLintFolderPath.Should().Be("c:\\rootfolder\\.sonarlint");
            actual.PartialCSharpRuleSetPath.Should().Be(".sonarlint\\slvs_samples_bound_vs2019csharp.ruleset");
            actual.PartialCSharpSonarLintXmlPath.Should().Be(".sonarlint\\slvs_samples_bound_vs2019\\CSharp\\SonarLint.xml");
            actual.PartialVBRuleSetPath.Should().Be(".sonarlint\\slvs_samples_bound_vs2019vb.ruleset");
            actual.PartialVBSonarLintXmlPath.Should().Be(".sonarlint\\slvs_samples_bound_vs2019\\VB\\SonarLint.xml");
        }

        private static MigrationSettingsProvider CreateTestSubject(IServiceProvider serviceProvider = null,
            IThreadHandling threadHandling = null)
        {
            serviceProvider ??= Mock.Of<IServiceProvider>();
            threadHandling ??= new NoOpThreadHandler();

            return new MigrationSettingsProvider(serviceProvider, threadHandling);
        }

        private static Mock<IServiceProvider> CreateServiceProviderWithSolution(IVsSolution solution)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution);

            return serviceProvider;
        }

        private static Mock<IVsSolution> CreateIVsSolution(string pathToReturn)
        {
            var solution = new Mock<IVsSolution>();

            object solutionDirectory = pathToReturn;
            solution
                .Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionDirectory))
                .Returns(VSConstants.S_OK);

            return solution;
        }
    }
}
