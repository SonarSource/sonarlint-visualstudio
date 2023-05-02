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
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class ProjectLanguageIndicatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ProjectLanguageIndicator, IProjectLanguageIndicator>(
                MefTestHelpers.CreateExport<ISonarLanguageRecognizer>(),
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void HasTargetLanguage_NonCriticalExceptionInAccessingProjectProperties_False()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(isFolderWorkspace: false);
            var logger = new TestLogger();
            var predicate = new Mock<ITargetLanguagePredicate>();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, logger: logger);

            var project = CreateProject(projectItems: null, projectName: "some project");
            var result = testSubject.HasTargetLanguage(project, predicate.Object);

            result.Should().BeFalse();

            logger.AssertPartialOutputStringExists("some project", nameof(NullReferenceException));
        }

        [TestMethod]
        public void HasTargetLanguage_CriticalExceptionInAccessingProjectProperties_ExceptionNotCaught()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(isFolderWorkspace: false);
            var predicate = new Mock<ITargetLanguagePredicate>();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object);

            var project = CreateProject(exToThrow: new StackOverflowException("this is a test"));
            Action act = () => testSubject.HasTargetLanguage(project, predicate.Object);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");
        }

        [DataTestMethod]
        [DataRow("TopFile.json")]
        [DataRow("x.abc")]
        public void HasTargetLanguage_IgnoresUnsupportedFiles(string fileName)
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(true);
            var topFile = CreateItem(fileName);
            var items = CreateProjectItems(topFile);
            var project = CreateProject(items);

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            var result = testSubject.HasTargetLanguage(project, predicate.Object);

            result.Should().BeFalse();
            sonarLanguageRecognizer.Verify(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);
            predicate.Verify(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HasTargetLanguage_IgnoresNoExtension()
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(true);
            var topFile = CreateItem("noextensionfileorfolder");
            var items = CreateProjectItems(topFile);
            var project = CreateProject(items);

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            var result = testSubject.HasTargetLanguage(project, predicate.Object);

            result.Should().BeFalse();
            sonarLanguageRecognizer.Verify(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Never);
            predicate.Verify(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>()), Times.Never);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void HasTargetLanguage_SingleFile_ReturnsPredicateResult(bool predicateResult)
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(predicateResult);
            var file = CreateItem("File.js");
            var items = CreateProjectItems(file);
            var project = CreateProject(items);

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            var result = testSubject.HasTargetLanguage(project, predicate.Object);

            result.Should().Be(predicateResult);
            sonarLanguageRecognizer.Verify(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);
            predicate.Verify(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void HasTargetLanguage_dteProjectHasHierarchy_CorrectlyEnumeratesFiles()
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            /*
             --root
              |-- TopFile.cs 1
              |-- Folder1
                  |-- File1.cs 2
                  |-- Script.jsx 3
                  |-- data.json 4
              |-- Folder2
                  |-- Folder3
                     |-- File.css 5
                     |-- some_cplusplus_file.cpp 6
              |-- Folder4
                  |-- Style.scss 7
             */

            var topFile = CreateItem("TopFile.cs");
            var file1 = CreateItem("File1.cs");
            var script = CreateItem("Script.jsx");
            var data = CreateItem("data.json");
            var folder1 = CreateItem("Folder1", file1, script, data);
            var cssFile = CreateItem("File.css");
            var someCppFile = CreateItem("some_cplusplus_file.cpp");
            var folder3 = CreateItem("Folder3", cssFile, someCppFile);
            var folder2 = CreateItem("Folder2", folder3);
            var style = CreateItem("Style.scss");
            var folder4 = CreateItem("Folder4", style);
            var items = CreateProjectItems(topFile, folder1, folder2, folder4);
            var project = CreateProject(items);
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(false);

            var result = testSubject.HasTargetLanguage(project, predicate.Object);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(7));
            predicate
                .Invocations
                .Select(x => ((AnalysisLanguage)x.Arguments[0], (string)x.Arguments[1]))
                .Should()
                .BeEquivalentTo(new[]
                {
                    (AnalysisLanguage.RoslynFamily, "cs"),
                    (AnalysisLanguage.RoslynFamily, "cs"),
                    (AnalysisLanguage.Javascript, "jsx"),
                    (AnalysisLanguage.CascadingStyleSheets, "css"),
                    (AnalysisLanguage.CFamily, "cpp"),
                    (AnalysisLanguage.CascadingStyleSheets, "scss"),
                });

            result.Should().Be(false);
        }

        [TestMethod]
        public void HasTargetLanguage_dteProjectHasHierarchyHasMultipleLanguages_IsLazy()
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            /*
             --root
              |-- TopFile.cs 1
              |-- Folder1
                  |-- File1.cs 2
                  |-- Script.jsx 3
              |-- Folder2
                  |-- Folder3
                     |-- File.css 4
                     |-- some_cplusplus_file.cpp 5
              |-- Folder4
                  |-- Style.scss 6
             */

            var topFile = CreateItem("TopFile.cs");
            var file1 = CreateItem("File1.cs");
            var script = CreateItem("Script.jsx");
            var folder1 = CreateItem("Folder1", file1, script);
            var cssFile = CreateItem("File.css");
            var someCppFile = CreateItem("some_cplusplus_file.cpp");
            var folder3 = CreateItem("Folder3", cssFile, someCppFile);
            var folder2 = CreateItem("Folder2", folder3);
            var style = CreateItem("Style.scss");
            var folder4 = CreateItem("Folder4", style);
            var items = CreateProjectItems(topFile, folder1, folder2, folder4);
            var project = CreateProject(items);

            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(AnalysisLanguage.Javascript, "jsx")).Returns(true);

            var result = testSubject.HasTargetLanguage(project, predicate.Object);
            
            result.Should().BeTrue();
            predicate.Invocations.Should().HaveCount(3);
            sonarLanguageRecognizer.Invocations.Should().HaveCount(3);
        }

        [TestMethod]
        public void HasTargetLanguage_OpenAsFolder_EnumeratesCorrectly()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("Class.cs", "Settings.json", "script.jsx", "style.css", "script2.js", "some_cpp_file.cpp");
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(false);

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.HasTargetLanguage(null, predicate.Object);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(6));
            predicate
                .Invocations
                .Select(x => ((AnalysisLanguage)x.Arguments[0], (string)x.Arguments[1]))
                .Should()
                .BeEquivalentTo(new[]
                {
                    (AnalysisLanguage.RoslynFamily, "cs"),
                    (AnalysisLanguage.Javascript, "jsx"),
                    (AnalysisLanguage.CascadingStyleSheets, "css"),
                    (AnalysisLanguage.Javascript, "js"),
                    (AnalysisLanguage.CFamily, "cpp"),
                });

            actualResult.Should().Be(false);
        }

        [TestMethod]
        public void HasTargetLanguage_OpenAsFolder_IsLazy()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("Class.cs", "Settings.json", "script.jsx", "style.css", "script2.js", "some_cpp_file.cpp");
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(AnalysisLanguage.CascadingStyleSheets, "css")).Returns(true);

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.HasTargetLanguage(null, predicate.Object);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(4));
            predicate.Invocations.Should().HaveCount(3);
            
            actualResult.Should().Be(true);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void HasTargetLanguage_OpenAsFolder_SingleFile_ReturnsPredicateResult(bool predicateResult)
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(predicateResult);
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("script.js");

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            var result = testSubject.HasTargetLanguage(null, predicate.Object);

            result.Should().Be(predicateResult);
            sonarLanguageRecognizer.Verify(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);
            predicate.Verify(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>()), Times.Once);
        }

        [DataTestMethod]
        [DataRow("TopFile.json")]
        [DataRow("x.abc")]
        public void HasTargetLanguage_OpenAsFolder_IgnoresUnsupportedFiles(string fileName)
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(true);
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory(fileName);

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer: sonarLanguageRecognizer.Object);


            var result = testSubject.HasTargetLanguage(null, predicate.Object);

            result.Should().BeFalse();
            sonarLanguageRecognizer.Verify(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);
            predicate.Verify(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HasTargetLanguage_OpenAsFolder_IgnoresNoExtension()
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();
            var predicate = new Mock<ITargetLanguagePredicate>();
            predicate.Setup(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>())).Returns(true);
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("noextensionfileorfolder");

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer: sonarLanguageRecognizer.Object);


            var result = testSubject.HasTargetLanguage(null, predicate.Object);

            result.Should().BeFalse();
            sonarLanguageRecognizer.Verify(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Never);
            predicate.Verify(x => x.IsTargetLanguage(It.IsAny<AnalysisLanguage>(), It.IsAny<string>()), Times.Never);
        }

        private static Mock<IFolderWorkspaceService> CreateFolderWorkSpaceService(bool isFolderWorkspace = false)
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            folderWorkspaceService.Setup(f => f.IsFolderWorkspace()).Returns(isFolderWorkspace);
            folderWorkspaceService.Setup(f => f.FindRootDirectory()).Returns("Root");


            return folderWorkspaceService;
        }

        private static Mock<IDirectory> CreateDirectory(params string[] files)
        {
            var directory = new Mock<IDirectory>();
            directory.Setup(d => d.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new List<string>());

            if (files.Length > 0)
            {
                directory.Setup(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories)).Returns(files.ToList());
            }

            return directory;
        }

        private IFileSystem CreateFileSystem(IDirectory directory = null)
        {
            directory ??= Mock.Of<IDirectory>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory);

            return fileSystem.Object;
        }

        private IProjectLanguageIndicator CreateTestSubject(IFolderWorkspaceService folderWorkspaceService = null,
            IDirectory directory = null,
            ISonarLanguageRecognizer sonarLanguageRecognizer = null,
            ILogger logger = null)
        {

            folderWorkspaceService ??= Mock.Of<IFolderWorkspaceService>();
            sonarLanguageRecognizer ??= CreateSonarLanguageRecognizer().Object;
            logger ??= Mock.Of<ILogger>();

            var fileSystem = CreateFileSystem(directory); 

            return new ProjectLanguageIndicator(sonarLanguageRecognizer, folderWorkspaceService, logger, fileSystem);
        }

        private ProjectItem CreateItem(string itemName, params ProjectItem[] childItems)
        {
            var projectItems = CreateProjectItems(childItems);

            var projectItem = new Mock<ProjectItem>();
            projectItem.SetupGet(p => p.Name).Returns(itemName);
            projectItem.SetupGet(p => p.ProjectItems).Returns(projectItems);

            return projectItem.Object;
        }

        private ProjectItems CreateProjectItems(params ProjectItem[] childItems)
        {
            return CreateProjectItems(childItems.ToList());
        }

        private ProjectItems CreateProjectItems(IEnumerable<ProjectItem> childItems)
        {
            var projectItems = new Mock<ProjectItems>();
            projectItems.Setup(p => p.GetEnumerator()).Returns(childItems.GetEnumerator());
            projectItems.SetupGet(p => p.Count).Returns(childItems.Count);

            return projectItems.Object;
        }

        private Project CreateProject(ProjectItems projectItems = null, string projectName = null, Exception exToThrow = null)
        {
            var project = new Mock<Project>();
            project.SetupGet(p => p.ProjectItems).Returns(projectItems);
            project.SetupGet(p => p.Name).Returns(projectName);

            if (exToThrow != null)
            {
                project.SetupGet(x => x.ProjectItems).Throws(exToThrow);
            }

            return project.Object;
        }

        private Mock<ISonarLanguageRecognizer> CreateSonarLanguageRecognizer()
        {
            var sonarLanguageRecognizer = new Mock<ISonarLanguageRecognizer>();

            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>())).Returns((AnalysisLanguage?)null);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^(cs|CS)$"))).Returns(AnalysisLanguage.RoslynFamily);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^(cpp)$"))).Returns(AnalysisLanguage.CFamily);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^(js|JS|jsx)$"))).Returns(AnalysisLanguage.Javascript);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^(ts|TS)$"))).Returns(AnalysisLanguage.TypeScript);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^(css|CSS|scss)$"))).Returns(AnalysisLanguage.CascadingStyleSheets);

            return sonarLanguageRecognizer;
        }
    }
}
