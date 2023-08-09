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

using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles
{
    [TestClass]
    public class QualityProfileUpdaterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<QualityProfileUpdater, IQualityProfileUpdater>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IQualityProfileDownloader>(),
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<QualityProfileUpdater>();

        [TestMethod]
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task UpdateBoundSolutionAsync_NotNewConnectedMode_DoesNotCallDownloader(SonarLintMode mode)
        {
            var configProvider = CreateConfigProvider(mode);
            var qpDownloader = new Mock<IQualityProfileDownloader>();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            qpDownloader.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_CallsDownloader()
        {
            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            qpDownloader.Verify(x => x.UpdateAsync(boundProject, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        private Mock<IConfigurationProvider> CreateConfigProvider(SonarLintMode mode, BoundSonarQubeProject boundProject = null)
        {
            var config = new BindingConfiguration(boundProject ?? new BoundSonarQubeProject(), mode, "any directory");

            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(config);

            return configProvider;
        }

        private static QualityProfileUpdater CreateTestSubject(
            IConfigurationProvider configProvider = null,
            IQualityProfileDownloader qpDownloader = null)
            => new QualityProfileUpdater(
                configProvider, qpDownloader, new TestLogger(logToConsole: true));
    }
}
