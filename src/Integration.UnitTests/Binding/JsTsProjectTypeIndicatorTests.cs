using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class JsTsProjectTypeIndicatorTests
    {
        

        [TestInitialize]
        public void TestInitialize()
        {

        }

        [DataRow("script.js", true)]
        [DataRow("file.cs", false)]
        [DataRow("Folder", false)]
        [DataRow("js", false)]
        [TestMethod]
        public void IsJsTs_dteProjectWithNoHierarchy_ReturnsCorrectly(string fileName, bool expectedResult)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService();
            var directory = CreateDirectory();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object);

            var item = CreateItem(fileName);
            var items = CreateProjectItems(item);
            var project = CreateProject(items);

            var actualResult = testSubject.IsJsTs(project);


            //Sanity Check to make sure no disk search is done
            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Never);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Never);
            directory.Verify(d => d.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);

            actualResult.Should().Be(expectedResult);
        }

        [TestMethod]
        public void IsJsTs_dteProjectMultipleFiles_ReturnsWhenFindJS()
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

            var result = testSubject.IsJsTs(project);

            //Sanity Check to make sure no disk search is done
            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Never);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Never);
            directory.Verify(d => d.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(2));
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(".cs"), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(".js"), Times.Once);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsJsTs_dteProjectHasHierarchyHasJS_ReturnsTrue()
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            /*
             --root
              |-- TopFile.cs
              |-- Folder1
                  |--File1.cs
                  |--Script.js
              |-- Folder2
                  |--Folder3
                     |--File2.cs
             */

            var topFile = CreateItem("TopFile.cs");
            var file1 = CreateItem("File1.cs");
            var file2 = CreateItem("Script.js");
            var folder1 = CreateItem("Folder1", file1, file2);
            var script = CreateItem("File2.cs");
            var folder3 = CreateItem("Folder3", script);
            var folder2 = CreateItem("Folder2", folder3);
            var items = CreateProjectItems(topFile, folder1, folder2);
            var project = CreateProject(items);

            var result = testSubject.IsJsTs(project);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(4));

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsJsTs_dteProjectHasHierarchyHasNoJS_ReturnsFalse()
        {
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(sonarLanguageRecognizer: sonarLanguageRecognizer.Object);

            /*
             --root
              |-- TopFile.cs
              |-- Folder1
                  |--File1.cs
                  |--File2.js
              |-- Folder2
                  |--Folder3
                     |--File3.cs
             */

            var topFile = CreateItem("TopFile.cs");
            var file1 = CreateItem("File1.cs");
            var file2 = CreateItem("File2.cs");
            var folder1 = CreateItem("Folder1", file1, file2);
            var script = CreateItem("File3.cs");
            var folder3 = CreateItem("Folder3", script);
            var folder2 = CreateItem("Folder2", folder3);
            var items = CreateProjectItems(topFile, folder1, folder2);
            var project = CreateProject(items);

            var result = testSubject.IsJsTs(project);

            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(7));

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsJsTs_dteProjecetNull_NotOpenAsFolder_ReturnsFalse()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(false);
            var directory = CreateDirectory();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object);

            var result = testSubject.IsJsTs(null);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Never);
            directory.Verify(d => d.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);

            result.Should().BeFalse();
        }

        [DataRow("script.js", true)]
        [DataRow("file.cs", false)]
        [DataRow("Folder", false)]
        [DataRow("js", false)]
        [TestMethod]
        public void IsJsTs_OpenAsFolder_ReturnsCorrectly(string fileName, bool expectedResult)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory(fileName);
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.IsJsTs(null);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Once);

            actualResult.Should().Be(expectedResult);
        }

        [TestMethod]
        public void IsJsTs_OpenAsFolderHasJS_ReturnsTrue()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("Class.cs", "Settings.json", "script.js", "script2.js");
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.IsJsTs(null);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(3));
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(".js"), Times.Once);

            actualResult.Should().Be(true);
        }

        [TestMethod]
        public void IsJsTs_OpenAsFolderHasNoJS_ReturnsFalse()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true);
            var directory = CreateDirectory("Class.cs", "Settings.json", "Class1.cs", "Class2.cs");
            var sonarLanguageRecognizer = CreateSonarLanguageRecognizer();

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, directory.Object, sonarLanguageRecognizer.Object);

            var actualResult = testSubject.IsJsTs(null);

            folderWorkspaceService.Verify(f => f.IsFolderWorkspace(), Times.Once);
            folderWorkspaceService.Verify(f => f.FindRootDirectory(), Times.Once);
            directory.Verify(d => d.EnumerateFiles("Root", "*", SearchOption.AllDirectories), Times.Once);
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(It.IsAny<string>()), Times.Exactly(4));
            sonarLanguageRecognizer.Verify(s => s.GetAnalysisLanguageFromExtension(".js"), Times.Never);

            actualResult.Should().Be(false);
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
            directory = directory ?? Mock.Of<IDirectory>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory);

            return fileSystem.Object;
        }

        private JsTsProjectTypeIndicator CreateTestSubject(IFolderWorkspaceService folderWorkspaceService = null, IDirectory directory = null, ISonarLanguageRecognizer sonarLanguageRecognizer = null)
        {

            folderWorkspaceService = folderWorkspaceService ?? Mock.Of<IFolderWorkspaceService>();
            sonarLanguageRecognizer = sonarLanguageRecognizer ?? CreateSonarLanguageRecognizer().Object;

            var fileSystem = CreateFileSystem(directory); 

            return new JsTsProjectTypeIndicator(sonarLanguageRecognizer, folderWorkspaceService, fileSystem);
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

        private Project CreateProject(ProjectItems projectItems)
        {
            var project = new Mock<Project>();
            project.SetupGet(p => p.ProjectItems).Returns(projectItems);

            return project.Object;
        }

        private Mock<ISonarLanguageRecognizer> CreateSonarLanguageRecognizer()
        {
            var sonarLanguageRecognizer = new Mock<ISonarLanguageRecognizer>();

            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(It.IsAny<string>())).Returns((AnalysisLanguage?)null);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(".cs")).Returns(AnalysisLanguage.RoslynFamily);
            sonarLanguageRecognizer.Setup(x => x.GetAnalysisLanguageFromExtension(".js")).Returns(AnalysisLanguage.Javascript);

            return sonarLanguageRecognizer;
        }
    }
}
