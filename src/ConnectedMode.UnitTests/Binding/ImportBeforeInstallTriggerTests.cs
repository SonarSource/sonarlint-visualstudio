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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class ImportBeforeInstallTriggerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ImportBeforeInstallTrigger, ImportBeforeInstallTrigger>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IImportBeforeFileGenerator>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void InvokeBindingChanged_StandaloneMode_ImportBeforeFileGeneratorIsNotCalled()
        {
            var configProvider = SetUpConfigProvider(SonarLintMode.Standalone);
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            var testSubject = CreateTestSubject(configProvider, importBeforeFileGenerator.Object);
            testSubject.TriggerUpdate().Forget();

            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Never);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void TriggerUpdate_ConnectedMode_ImportBeforeFileGeneratorIsCalled(SonarLintMode mode)
        {
            var configProvider = SetUpConfigProvider(mode);
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            var testSubject = CreateTestSubject(configProvider, importBeforeFileGenerator.Object);
            testSubject.TriggerUpdate().Forget();

            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);
        }

        private IConfigurationProvider SetUpConfigProvider(SonarLintMode mode)
        {
            var configProvider = new Mock<IConfigurationProvider>();
            var bindingConfig = new BindingConfiguration(
                                    new BoundSonarQubeProject(new Uri("http://localhost"), "test", ""),
                                    mode, "");

            configProvider.Setup(x => x.GetConfiguration()).Returns(bindingConfig);

            return configProvider.Object;
        }

        private ImportBeforeInstallTrigger CreateTestSubject(IConfigurationProvider configurationProvider, IImportBeforeFileGenerator importBeforeFileGenerator = null, IThreadHandling threadHandling = null)
        {
            importBeforeFileGenerator ??= Mock.Of<IImportBeforeFileGenerator>();
            threadHandling ??= new NoOpThreadHandler();


            return new ImportBeforeInstallTrigger(configurationProvider, importBeforeFileGenerator, threadHandling);
        }
    }
}
