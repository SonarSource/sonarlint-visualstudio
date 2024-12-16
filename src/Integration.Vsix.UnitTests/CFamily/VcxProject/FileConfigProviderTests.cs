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

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject
{
    [TestClass]
    public class FileConfigProviderTests
    {
        private const string SourceFilePath = "any path";
        private TestLogger logger;
        private IFileInSolutionIndicator fileInSolutionIndicator;
        private DTE2 dte;
        private IVsUIServiceOperation uiServiceOperation;
        private FileConfigProvider testSubject;
        private const string ClFilePath = "C:\\path\\cl.exe";

        private static Mock<IFileSystem> CreateFileSystemWithExistingFile(string fullPath)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(fullPath)).Returns(true);
            return fileSystem;
        }
        private static Mock<IFileSystem> CreateFileSystemWithClCompiler() => CreateFileSystemWithExistingFile(ClFilePath);

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<FileConfigProvider, IFileConfigProvider>(
                MefTestHelpers.CreateExport<IFileInSolutionIndicator>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
                MefTestHelpers.CreateExport<IFileSystem>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FileConfigProvider>();

        [TestInitialize]
        public void TestInitialize()
        {
            logger = new TestLogger();
            fileInSolutionIndicator = CreateDefaultFileInSolutionIndicator();
            dte = Substitute.For<DTE2>();
            uiServiceOperation = CreateDefaultUiServiceOperation(dte);

            testSubject = new FileConfigProvider(uiServiceOperation, fileInSolutionIndicator, logger, new NoOpThreadHandler(), CreateFileSystemWithClCompiler().Object);
        }

        private static IFileInSolutionIndicator CreateDefaultFileInSolutionIndicator()
        {
            var mock = Substitute.For<IFileInSolutionIndicator>();
            mock.IsFileInSolution(Arg.Any<ProjectItem>()).Returns(true);
            return mock;
        }

        private static IVsUIServiceOperation CreateDefaultUiServiceOperation(DTE2 dte2)
        {
            var mock = Substitute.For<IVsUIServiceOperation>();
            mock.Execute<SDTE, DTE2, IFileConfig>(Arg.Any<Func<DTE2, IFileConfig>>()).Returns(info => info.Arg<Func<DTE2, IFileConfig>>()(dte2));
            return mock;
        }

        [TestMethod]
        public void Get_FailsToRetrieveProjectItem_NonCriticalException_ExceptionCaughtAndNullReturned()
        {
            dte.Solution.ThrowsForAnyArgs<NotImplementedException>();

            var result = testSubject.Get(SourceFilePath, new CFamilyAnalyzerOptions());

            result.Should().BeNull();
            logger.AssertPartialOutputStringExists(nameof(NotImplementedException), SourceFilePath);
        }

        [TestMethod]
        public void Get_FailsToRetrieveProjectItem_NonCriticalException_Pch_ExceptionCaughtNotLoggedAndNullReturned()
        {
            dte.Solution.ThrowsForAnyArgs<NotImplementedException>();

            var result = testSubject.Get(SourceFilePath, new CFamilyAnalyzerOptions{CreatePreCompiledHeaders = true});

            result.Should().BeNull();
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_FailsToRetrieveProjectItem_CriticalException_ExceptionThrown()
        {
            dte.Solution.ThrowsForAnyArgs<DivideByZeroException>();

            Action act = () => testSubject.Get(SourceFilePath, new CFamilyAnalyzerOptions());

            act.Should().Throw<DivideByZeroException>();
        }

        [TestMethod]
        public void Get_NoProjectItem_ReturnsNull()
        {
            dte.Solution.FindProjectItem(SourceFilePath).ReturnsNull();

            testSubject.Get(SourceFilePath, null)
                .Should().BeNull();

            Received.InOrder(() =>
            {
                uiServiceOperation.Execute<SDTE, DTE2, IFileConfig>(Arg.Any<Func<DTE2, IFileConfig>>());
                dte.Solution.FindProjectItem(SourceFilePath);
            });
        }

        [TestMethod]
        public void Get_ProjectItemNotInSolution_ReturnsNull()
        {
            var mockProjectItem = CreateMockProjectItem(SourceFilePath);
            dte.Solution.FindProjectItem(SourceFilePath).Returns(mockProjectItem.Object);
            fileInSolutionIndicator.IsFileInSolution(mockProjectItem.Object).Returns(false);

            testSubject.Get(SourceFilePath, null)
                .Should().BeNull();

            Received.InOrder(() =>
            {
                uiServiceOperation.Execute<SDTE, DTE2, IFileConfig>(Arg.Any<Func<DTE2, IFileConfig>>());
                dte.Solution.FindProjectItem(SourceFilePath);
                fileInSolutionIndicator.IsFileInSolution(mockProjectItem.Object);
            });
        }

        [TestMethod]
        public void Get_SuccessfulConfig_ConfigReturned()
        {
            var mockProjectItem = CreateMockProjectItem(SourceFilePath);
            dte.Solution.FindProjectItem(SourceFilePath).Returns(mockProjectItem.Object);

            testSubject.Get(SourceFilePath, null)
                .Should().NotBeNull();
        }
    }
}
