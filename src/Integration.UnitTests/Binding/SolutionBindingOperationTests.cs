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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class SolutionBindingOperationTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private MockFileSystem fileSystem;

        private const string SolutionRoot = @"c:\solution";

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.solutionItemsProject = this.solutionMock.AddOrGetProject("Solution items");
            this.projectSystemHelper.SolutionItemsProject = this.solutionItemsProject;
            this.projectSystemHelper.CurrentActiveSolution = this.solutionMock;
            this.fileSystem = new MockFileSystem();
            this.sccFileSystem = new ConfigurableSourceControlledFileSystem(fileSystem);

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFileSystem);
        }

        #region Tests

        [TestMethod]
        public void SolutionBindingOperation_ArgChecks()
        {
            Action act = () => new SolutionBindingOperation(null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets_ArgChecks()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.RegisterKnownConfigFiles(null));
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject();
            var languageToFileMap = new Dictionary<Language, IBindingConfig>
            {
                [Language.CSharp] = CreateMockConfigFile("c:\\csharp.txt").Object,
                [Language.VBNET] = CreateMockConfigFile("c:\\vbnet.txt").Object
            };

            // Sanity
            testSubject.RuleSetsInformationMap.Should().BeEmpty("Not expecting any registered rulesets");

            // Act
            testSubject.RegisterKnownConfigFiles(languageToFileMap);

            // Assert
            CollectionAssert.AreEquivalent(languageToFileMap.Keys.ToArray(), testSubject.RuleSetsInformationMap.Keys.ToArray());
            testSubject.RuleSetsInformationMap[Language.CSharp].Should().Be(languageToFileMap[Language.CSharp]);
            testSubject.RuleSetsInformationMap[Language.VBNET].Should().Be(languageToFileMap[Language.VBNET]);
        }

        [TestMethod]
        public void SolutionBindingOperation_GetRuleSetInformation()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject();

            // Test case 1: unknown ruleset map
            var ruleSetMap = new Dictionary<Language, IBindingConfig>();
            testSubject.RegisterKnownConfigFiles(ruleSetMap);

            // Act + Assert
            using (new AssertIgnoreScope())
            {
                testSubject.GetBindingConfig(Language.CSharp).Should().BeNull();
            }

            // Test case 2: known ruleset map
            // Arrange
            ruleSetMap[Language.CSharp] = CreateMockConfigFile("c:\\csharp.txt").Object;
            ruleSetMap[Language.VBNET] = CreateMockConfigFile("c:\\vb.txt").Object;

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            // Act
            var config = testSubject.GetBindingConfig(Language.CSharp);

            // Assert
            config.Should().Be(testSubject.RuleSetsInformationMap[Language.CSharp]);
        }

        [TestMethod]
        public void SolutionBindingOperation_Initialization()
        {
            // Arrange
            var testSubject = CreateTestSubject();

            // Act
            testSubject.Initialize();

            // Assert
            testSubject.SolutionFullPath.Should().Be(Path.Combine(SolutionRoot, "xxx.sln"));
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_SolutionLevelFilesAreSaved()
        {
            // Arrange
            var csConfigFile = CreateMockConfigFile("c:\\csharp.txt");

            var vbConfigFile = CreateMockConfigFile("c:\\vb.txt");

            var testSubject = CreateTestSubject();

            var ruleSetMap = new Dictionary<Language, IBindingConfig>
            {
                [Language.CSharp] = csConfigFile.Object,
                [Language.VBNET] = vbConfigFile.Object
            };

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize();

            // Sanity
            testSubject.RuleSetsInformationMap[Language.CSharp].Should().Be(csConfigFile.Object);
            testSubject.RuleSetsInformationMap[Language.VBNET].Should().Be(vbConfigFile.Object);

            // Act
            testSubject.Prepare(CancellationToken.None);
            // Act (write pending)
            sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            CheckRuleSetFileWasSaved(csConfigFile);
            CheckRuleSetFileWasSaved(vbConfigFile);
        }

        [TestMethod]
        public void SolutionBindingOperation_CommitSolutionBinding()
        {
            var expectedFilePath = $"c:\\{Guid.NewGuid()}.txt";

            // Arrange
            var csConfigFile = CreateMockConfigFile(expectedFilePath);

            SolutionBindingOperation testSubject = this.CreateTestSubject();

            var languageToFileMap = new Dictionary<Language, IBindingConfig>()
            {
                { Language.CSharp, csConfigFile.Object }
            };

            testSubject.RegisterKnownConfigFiles(languageToFileMap);

            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            // Act
            var commitResult = testSubject.CommitSolutionBinding();

            // Assert
            commitResult.Should().BeTrue();

            solutionItemsProject.Files.Count.Should().Be(0, "Not expecting any items to be added to the solution in new connected mode");
            fileSystem.GetFile(expectedFilePath).Should().NotBe(null); // check the file was saved
        }

        #endregion Tests

        #region Helpers

        private SolutionBindingOperation CreateTestSubject()
        {
            return new SolutionBindingOperation(serviceProvider, fileSystem);
        }

        private Mock<IBindingConfig> CreateMockConfigFile(string expectedFilePath)
        {
            var configFile = new Mock<IBindingConfig>();
            configFile.SetupGet(x => x.SolutionLevelFilePaths).Returns(new List<string> {expectedFilePath});

            // Simulate an update to the scc file system on Save (prevents an assertion
            // in the product code).
            configFile.Setup(x => x.Save())
                .Callback(() =>
                {
                    fileSystem.AddFile(expectedFilePath, new MockFileData(""));
                });

            return configFile;
        }

        private static void CheckRuleSetFileWasSaved(Mock<IBindingConfig> mock)
        {
            mock.Verify(x => x.Save(), Times.Once);
        }

        #endregion Helpers
    }
}
