/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class LiveIssueFactoryTests
    {
        private static readonly MetadataReference CorLibRef = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemRef = MetadataReference.CreateFromFile(typeof(System.ComponentModel.AddingNewEventArgs).Assembly.Location);
        private static readonly MetadataReference SystemCoreRef = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        [TestMethod]
        public void Ctor_WithNullWorkspace_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new LiveIssueFactory(null, new Mock<IVsSolution>().Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_WithNullVsSolution_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new LiveIssueFactory(new AdhocWorkspace(), null);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("vsSolution");
        }

        [TestMethod]
        public void Ctor_WithNonNullParameters_ReadProjectGuidOfAllProjectsInSolution()
        {
            // Arrange
            var vsSolutionMock = new Mock<IVsSolution>();
            uint fileCount = 2; // initialized with a value to actually assign a value to the out
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, null, out fileCount))
                .Returns(VSConstants.S_OK);
            var fileNames = new string[fileCount];
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, fileCount, fileNames, out fileCount))
                .OutCallback((uint x, uint y, string[] names, out uint z) =>
                    {
                        z = 0;
                        names[0] = "Project1";
                        names[1] = "Project2";
                    });
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns(Guid.Empty);

            // Act
            new LiveIssueFactory(new AdhocWorkspace(), vsSolutionMock.Object);

            // Assert
            vsSolutionMock.Verify(x => x.GetProjectFilesInSolution(0, It.IsAny<uint>(), It.IsAny<string[]>(), out fileCount),
                Times.Exactly(2));
            vsSolutionMock.As<IVsSolution5>().Verify(x => x.GetGuidOfProjectFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void Ctor_WhenFirstCallToGetProjectFilesInSolutionFails_Returns()
        {
            // Arrange
            var vsSolutionMock = new Mock<IVsSolution>();
            uint fileCount = 0;
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, null, out fileCount))
                .Returns(VSConstants.E_FAIL);
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns(Guid.Empty);

            // Act
            new LiveIssueFactory(new AdhocWorkspace(), vsSolutionMock.Object);

            // Assert
            vsSolutionMock.Verify(x => x.GetProjectFilesInSolution(0, It.IsAny<uint>(), It.IsAny<string[]>(), out fileCount),
                Times.Once);
            vsSolutionMock.As<IVsSolution5>().Verify(x => x.GetGuidOfProjectFile(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Ctor_WhenSecondCallToGetProjectFilesInSolutionFails_Returns()
        {
            // Arrange
            var vsSolutionMock = new Mock<IVsSolution>();
            uint fileCount = 0;
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, null, out fileCount))
                .Returns(VSConstants.S_OK);
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, new string[0], out fileCount))
                .Returns(VSConstants.E_FAIL);
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns(Guid.Empty);

            // Act
            new LiveIssueFactory(new AdhocWorkspace(), vsSolutionMock.Object);

            // Assert
            vsSolutionMock.Verify(x => x.GetProjectFilesInSolution(0, It.IsAny<uint>(), It.IsAny<string[]>(), out fileCount),
                Times.Exactly(2));
            vsSolutionMock.As<IVsSolution5>().Verify(x => x.GetGuidOfProjectFile(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Create_WhenSyntaxTreeIsNull_ReturnsNull()
        {
            // Arrange
            var vsSolutionMock = new Mock<IVsSolution>();
            uint fileCount = 2; // initialized with a value to actually assign a value to the out
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, null, out fileCount))
                .Returns(VSConstants.S_OK);
            var fileNames = new string[fileCount];
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, fileCount, fileNames, out fileCount))
                .OutCallback((uint x, uint y, string[] paths, out uint z) =>
                {
                    z = 0;
                    paths[0] = "Project1";
                    paths[1] = "Project2";
                });
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns(Guid.Empty);
            var liveIssueFactory = new LiveIssueFactory(new AdhocWorkspace(), vsSolutionMock.Object);

            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("id", "title", "message", "category",
                DiagnosticSeverity.Hidden, true), Location.None);

            // Act
            var result = liveIssueFactory.Create(null, diagnostic);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenProjectFilePathIsNull_ReturnsNull()
        {
            // Arrange & Act
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("id", "title", "message", "category",
                DiagnosticSeverity.Hidden, true), Location.None);
            var result = SetupAndCreate(diagnostic, filePath: null);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenProjectFilePathIsNotFoundInDictionary_ReturnsNull()
        {
            // Arrange & Act
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("id", "title", "message", "category",
                DiagnosticSeverity.Hidden, true), Location.None);
            var result = SetupAndCreate(diagnostic, filePath: "foo");

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenIssueIsProjectLevel_ReturnsExpectedIssue()
        {
            // Arrange & Act
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("id", "title", "message", "category",
                DiagnosticSeverity.Hidden, true), Location.None);
            var result = SetupAndCreate(diagnostic, filePath: "Project1");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostic.Should().Be(diagnostic);
            result.ProjectGuid.Should().Be("00000000-0000-0000-0000-000000000000");
            result.IssueFilePath.Should().Be("");
            result.StartLine.Should().Be(0);
            result.WholeLineText.Should().Be("");
            result.LineHash.Should().Be("");
        }

        [TestMethod]
        public void Create_WhenIssueIsFileLevel_ReturnsExpectedIssue()
        {
            // Arrange & Act
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("id", "title", "message", "category",
                DiagnosticSeverity.Hidden, true), Location.Create("MySource.cs", new TextSpan(0, 0), new LinePositionSpan()));
            var result = SetupAndCreate(diagnostic, filePath: "Project1");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostic.Should().Be(diagnostic);
            result.ProjectGuid.Should().Be("00000000-0000-0000-0000-000000000000");
            result.IssueFilePath.Should().Be("MySource.cs");
            result.StartLine.Should().Be(0);
            result.WholeLineText.Should().Be("");
            result.LineHash.Should().Be("");
        }

        [TestMethod]
        public void Create_WhenIssueIsLineLevel_ReturnsExpectedIssue()
        {
            // Arrange & Act
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("id", "title", "message", "category",
                DiagnosticSeverity.Hidden, true),
                Location.Create("MySource.cs",
                    new TextSpan(1, 1),
                    new LinePositionSpan(new LinePosition(1, 1), new LinePosition(1, 2))));
            var result = SetupAndCreate(diagnostic, filePath: "Project1");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostic.Should().Be(diagnostic);
            result.ProjectGuid.Should().Be("00000000-0000-0000-0000-000000000000");
            result.IssueFilePath.Should().Be("MySource.cs");
            result.StartLine.Should().Be(2);
            result.WholeLineText.Should().Be("    class Foo");
            result.LineHash.Should().Be("e1b4eea6db405a204a21bd5251c5385d");
        }

        private LiveIssue SetupAndCreate(Diagnostic diagnostic, string filePath)
        {
            // Arrange
            var vsSolutionMock = new Mock<IVsSolution>();
            uint fileCount = 1; // initialized with a value to actually assign a value to the out
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, null, out fileCount))
                .Returns(VSConstants.S_OK);
            var fileNames = new string[fileCount];
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, fileCount, fileNames, out fileCount))
                .OutCallback((uint x, uint y, string[] paths, out uint z) =>
                {
                    z = 0;
                    paths[0] = "Project1";
                });
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns(Guid.Empty);

            var projectId = ProjectId.CreateNewId();
            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Project1", "Assembly1", LanguageNames.CSharp,
                    filePath))
                .AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(projectId), "MySource.cs",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(@"namespace {
    class Foo
    {
    }
}"), VersionStamp.Default))));
            workspace.TryApplyChanges(solution);

            var liveIssueFactory = new LiveIssueFactory(workspace, vsSolutionMock.Object);

            var syntaxTree = workspace.CurrentSolution.Projects.First().GetCompilationAsync().Result.SyntaxTrees.First();

            // Act
            using (new AssertIgnoreScope())
            {
                return liveIssueFactory.Create(syntaxTree, diagnostic);
            }
        }
    }
}
