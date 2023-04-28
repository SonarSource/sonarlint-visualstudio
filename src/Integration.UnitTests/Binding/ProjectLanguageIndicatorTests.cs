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
        public void HasOnOfTargetLanguages_JsTs_NonCriticalExceptionInAccessingProjectProperties_False()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(isFolderWorkspace: false);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, logger: logger);

            var project = CreateProject(projectItems: null, projectName: "some project");
            var result = testSubject.HasOneOfTargetLanguages(project, AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript);

            result.Should().BeFalse();

            logger.AssertPartialOutputStringExists("some project", nameof(NullReferenceException));
        }

        [TestMethod]
        public void HasOnOfTargetLanguages_JsTs_CriticalExceptionInAccessingProjectProperties_ExceptionNotCaught()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(isFolderWorkspace: false);

            var testSubject = CreateTestSubject(folderWorkspaceService.Object);

            var project = CreateProject(exToThrow: new StackOverflowException("this is a test"));
            Action act = () => testSubject.HasOneOfTargetLanguages(project, AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");
        }

        [DataRow("script.js", true)]
        [DataRow("script.ts", true)]
        [DataRow("file.cs", false)]
        [DataRow("Folder", false)]
        [DataRow("js", false)]
        [TestMethod]
        public void HasOnOfTargetLanguages_JsTs_dteProjectWithNoHierarchy_ReturnsCorrectly(string fileName, bool expectedResult)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService();
            var directory = CreateDirectory();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object);

            var item = CreateItem(fileName);
            var items = CreateProjectItems(item);
            var project = CreateProject(items);

            var actualResult = testSubject.HasOneOfTargetLanguages(project, AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript);

            //Sanity Check to make sure no disk search is done
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Never);
            directory.Verify(d => d.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);

            actualResult.Should().Be(expectedResult);
        }

        [TestMethod]
        public void HasOnOfTargetLanguages_JsTs_dteProjectMultipleFiles_ReturnsWhenFindJS()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService();
            var directory = CreateDirectory();
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var item1 = CreateItem("File1.cs");
            var item2 = CreateItem("Script1.js");
            var item3 = CreateItem("Script2.js");
            var items = CreateProjectItems(item1, item2, item3);
            var project = CreateProject(items);

            var result = testSubject.HasOneOfTargetLanguages(project, AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript);

            //Sanity Check to make sure no disk search is done
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Never);
            directory.Verify(d => d.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(2));
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension("File1.cs"), Times.Exactly(1));
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension("Script1.js"), Times.Once);

            result.Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow(AnalysisLanguage.Javascript, true, 4)]
        [DataRow(AnalysisLanguage.CascadingStyleSheets, true, 7)]
        [DataRow(AnalysisLanguage.CFamily, false, 8)]
        public void HasOnOfTargetLanguages_dteProjectHasHierarchyHasMultipleLanguages_Returns(AnalysisLanguage languageToSearch, bool expectedResult, int filesChecked)
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            /*
             --root
              |-- TopFile.cs 1
              |-- Folder1 2
                  |--File1.cs 3
                  |--Script.js 4
              |-- Folder2 5
                  |--Folder3 6
                     |--File.css 7
                     |--File2.cs 8
             */

            var topFile = CreateItem("TopFile.cs");
            var file1 = CreateItem("File1.cs");
            var script = CreateItem("Script.js");
            var folder1 = CreateItem("Folder1", file1, script);
            var file2 = CreateItem("File2.cs");
            var cssFile = CreateItem("File.css");
            var folder3 = CreateItem("Folder3", cssFile, file2);
            var folder2 = CreateItem("Folder2", folder3);
            var items = CreateProjectItems(topFile, folder1, folder2);
            var project = CreateProject(items);

            var result = testSubject.HasOneOfTargetLanguages(project, languageToSearch);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(filesChecked));

            result.Should().Be(expectedResult);
        }

        [DataTestMethod]
        [DataRow(AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript, true, 7)]
        [DataRow(AnalysisLanguage.Javascript, AnalysisLanguage.CascadingStyleSheets, true, 4)]
        [DataRow(AnalysisLanguage.CFamily, AnalysisLanguage.RoslynFamily, false, 8)]
        public void
            HasOnOfTargetLanguages_dteProjectHasHierarchyHasMultipleLanguages_RequiresAtLeastOneLanguageToBeFound(
                AnalysisLanguage languageToSearch1,
                AnalysisLanguage languageToSearch2,
                bool expectedResult,
                int filesChecked)
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            /*
             --root
              |-- TopFile.esproj 1
              |-- Folder1 2
                  |--File1.json 3
                  |--File2.css 4
              |-- Folder2 5
                  |--Folder3 6
                     |--File3.js 7
                     |--File4.css 8
             */

            var topFile = CreateItem("TopFile.esproj");
            var file1 = CreateItem("File1.json");
            var file2 = CreateItem("File2.css");
            var folder1 = CreateItem("Folder1", file1, file2);
            var file3 = CreateItem("File3.js");
            var file4 = CreateItem("File4.css");
            var folder3 = CreateItem("Folder3", file3, file4);
            var folder2 = CreateItem("Folder2", folder3);
            var items = CreateProjectItems(topFile, folder1, folder2);
            var project = CreateProject(items);

            var result = testSubject.HasOneOfTargetLanguages(project, languageToSearch1, languageToSearch2);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(filesChecked));

            result.Should().Be(expectedResult);
        }


        [TestMethod]
        public void HasOnOfTargetLanguages_JsTs_dteProject_OpenAsFolder_DoesFileSearch()
        {
            string fileName = "Script.js";

            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory(fileName);
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var item = CreateItem(fileName);
            var items = CreateProjectItems(item);
            var project = CreateProject(items);

            var actualResult = testSubject.HasOneOfTargetLanguages(project, AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);

            actualResult.Should().Be(true);
        }

        [DataRow("script.js", true)]
        [DataRow("script.ts", true)]
        [DataRow("file.cs", false)]
        [DataRow("Folder", false)]
        [DataRow("js", false)]
        [TestMethod]
        public void HasOnOfTargetLanguages_JsTs_OpenAsFolder_ReturnsCorrectly(string fileName, bool expectedResult)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory(fileName);
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.HasOneOfTargetLanguages(null, AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);

            actualResult.Should().Be(expectedResult);
        }

        [DataTestMethod]
        [DataRow(AnalysisLanguage.Javascript, true, 3)]
        [DataRow(AnalysisLanguage.CascadingStyleSheets, true, 4)]
        [DataRow(AnalysisLanguage.CFamily, false, 5)]
        public void HasOnOfTargetLanguages_OpenAsFolder_Returns(AnalysisLanguage languageToSearch, bool expectedResult, int filesChecked)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("Class.cs", "Settings.json", "script.js", "style.css", "script2.js");
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.HasOneOfTargetLanguages(null, languageToSearch);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(filesChecked));

            actualResult.Should().Be(expectedResult);
        }

        [DataTestMethod]
        [DataRow(AnalysisLanguage.Javascript, AnalysisLanguage.TypeScript, true, 3)]
        [DataRow(AnalysisLanguage.Javascript, AnalysisLanguage.CascadingStyleSheets, true, 2)]
        [DataRow(AnalysisLanguage.CFamily, AnalysisLanguage.RoslynFamily, false, 3)]
        public void HasOnOfTargetLanguages_OpenAsFolder_RequiresAtLeastOneLanguageToBeFound(AnalysisLanguage languageToSearch1, AnalysisLanguage languageToSearch2, bool expectedResult, int filesChecked)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("script.json", "style.css", "script2.js");
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.HasOneOfTargetLanguages(null, languageToSearch1, languageToSearch2);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(filesChecked));

            actualResult.Should().Be(expectedResult);
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
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^.*\\.(cs|CS)$"))).Returns(AnalysisLanguage.RoslynFamily);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^.*\\.(js|JS)$"))).Returns(AnalysisLanguage.Javascript);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^.*\\.(ts|TS)$"))).Returns(AnalysisLanguage.TypeScript);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsRegex("^.*\\.(css|CSS)$"))).Returns(AnalysisLanguage.CascadingStyleSheets);

            return sonarLanguageRecognizer;
        }
    }
}
