/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;
using static SonarLint.VisualStudio.Integration.UnitTests.Helpers.MoqExtensions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class LiveIssueFactoryTests
    {
        // Well-known value for a project that exists in the solution
        private const string ProjectInSolutionFilePath = "C:\\Project1.csproj";

        // Text span to use when the value doesn't matter
        private readonly TextSpan AnyTextSpan = new TextSpan(123, 45);

        [TestMethod]
        public void Ctor_WithNullWorkspace_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new LiveIssueFactory(null, new Mock<IVsSolution>().Object);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_WithNullVsSolution_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new LiveIssueFactory(new AdhocWorkspace(), null);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("vsSolution");
        }

        [TestMethod]
        public void Ctor_WithNonNullParameters_ReadProjectGuidOfAllProjectsInSolution()
        {
            // Arrange
            uint fileCountOut;
            var vsSolutionMock = SetupSolutionMocks(
                new KeyValuePair<string, string>("Project1", "11111111-1111-1111-1111-111111111111"),
                new KeyValuePair<string, string>("Project2", "22222222-2222-2222-2222-222222222222"));

            // Act
            new LiveIssueFactory(new AdhocWorkspace(), vsSolutionMock.Object);

            // Assert
            vsSolutionMock.Verify(x => x.GetProjectFilesInSolution(0, It.IsAny<uint>(), It.IsAny<string[]>(), out fileCountOut),
                Times.Exactly(2));
            vsSolutionMock.As<IVsSolution5>().Verify(x => x.GetGuidOfProjectFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void BuildMap_DuplicateSolutionFolderNamesAreIgnored()
        {
            // See #413: https://github.com/SonarSource/sonarlint-visualstudio/issues/413

            // Arrange
            var vsSolutionMock = SetupSolutionMocks(
                new KeyValuePair<string, string>("SolutionFolder1", "11111111-1111-1111-1111-111111111111"),
                new KeyValuePair<string, string>("Item1", "22222222-2222-2222-2222-222222222222"),
                new KeyValuePair<string, string>("SolutionFolder2", "33333333-3333-3333-3333-333333333333"),
                new KeyValuePair<string, string>("Item1", "44444444-4444-4444-4444-444444444444"),
                new KeyValuePair<string, string>("realProject.csproj", "55555555-5555-5555-5555-555555555555"),
                new KeyValuePair<string, string>("item1", "66666666-6666-6666-6666-666666666666"),
                new KeyValuePair<string, string>("ITEM1", "77777777-7777-7777-7777-777777777777")
                );

            uint fileCountOut;

            // Act
            IDictionary<string, string> map = LiveIssueFactory.BuildProjectPathToIdMap(vsSolutionMock.Object);

            // Assert
            vsSolutionMock.Verify(x => x.GetProjectFilesInSolution(0, It.IsAny<uint>(), It.IsAny<string[]>(), out fileCountOut),
                Times.Exactly(2));
            vsSolutionMock.As<IVsSolution5>().Verify(x => x.GetGuidOfProjectFile(It.IsAny<string>()), Times.Exactly(7));

            // Duplicate entries should have been ignored
            map.ContainsKey("SolutionFolder1").Should().BeTrue();
            map.ContainsKey("Item1").Should().BeTrue();
            map.ContainsKey("SolutionFolder2").Should().BeTrue();
            map.ContainsKey("realProject.csproj").Should().BeTrue();
            map.Count.Should().Be(4);

            map["SolutionFolder1"].Should().Be("11111111-1111-1111-1111-111111111111");
            map["SolutionFolder2"].Should().Be("33333333-3333-3333-3333-333333333333");
            map["realProject.csproj"].Should().Be("55555555-5555-5555-5555-555555555555");
            map["item1"].Should().Be("77777777-7777-7777-7777-777777777777");
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
                .Callback(new OutAction<uint, uint, string[], uint>((uint x, uint y, string[] paths, out uint z) =>
                {
                    z = 0;
                    paths[0] = "Project1";
                    paths[1] = "Project2";
                }));
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns(Guid.Empty);
            var liveIssueFactory = new LiveIssueFactory(new AdhocWorkspace(), vsSolutionMock.Object);

            var diagnostic = CreateDiagnostic(Location.None);

            // Act
            var result = liveIssueFactory.Create(null, diagnostic);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenProjectFilePathIsNull_ReturnsNull()
        {
            // Arrange & Act
            var diagnostic = CreateDiagnostic(Location.None);
            var result = SetupAndCreate(diagnostic, diagnosticProjectFilePath: null);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenProjectFilePathIsNotFoundInDictionary_ReturnsNull()
        {
            // Arrange & Act
            var diagnostic = CreateDiagnostic(Location.None);
            var result = SetupAndCreate(diagnostic, diagnosticProjectFilePath: "d:\\missing project file.path");

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenIssueIsModuleLevel_ReturnsExpectedIssue()
        {
            // Arrange & Act
            var diagnostic = CreateDiagnostic(Location.None);
            var result = SetupAndCreate(diagnostic, diagnosticProjectFilePath: ProjectInSolutionFilePath);

            // Assert
            result.Should().NotBeNull();
            result.Diagnostic.Should().Be(diagnostic);
            result.ProjectGuid.Should().Be("31d0daac-8606-40fe-8df0-01784706ea3e");
            result.FilePath.Should().BeNull();
            result.StartLine.Should().BeNull();
            result.WholeLineText.Should().BeNull();
            result.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenIssueIsFileLevel_ReturnsExpectedIssue()
        {
            // Arrange & Act
            var location = Location.Create("C:\\MySource.cs", AnyTextSpan, new LinePositionSpan());
            var diagnostic = CreateDiagnostic(location);

            var result = SetupAndCreate(diagnostic, diagnosticProjectFilePath: ProjectInSolutionFilePath);

            // Assert
            result.Should().NotBeNull();
            result.Diagnostic.Should().Be(diagnostic);
            result.ProjectGuid.Should().Be("31d0daac-8606-40fe-8df0-01784706ea3e");
            result.FilePath.Should().Be("C:\\MySource.cs");
            result.StartLine.Should().BeNull();
            result.WholeLineText.Should().BeNull();
            result.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Create_WhenIssueIsLineLevel_ReturnsExpectedIssue()
        {
            // Arrange & Act
            const int inRangeLineNumber = 1;
            var location = Location.Create("C:\\MySource.cs",
                    AnyTextSpan,
                    new LinePositionSpan(new LinePosition(inRangeLineNumber, 1), new LinePosition(inRangeLineNumber, 2)));
            var diagnostic = CreateDiagnostic(location);

            var result = SetupAndCreate(diagnostic, diagnosticProjectFilePath: ProjectInSolutionFilePath);

            // Assert
            result.Should().NotBeNull();
            result.Diagnostic.Should().Be(diagnostic);
            result.ProjectGuid.Should().Be("31d0daac-8606-40fe-8df0-01784706ea3e");
            result.FilePath.Should().Be("C:\\MySource.cs");
            result.StartLine.Should().Be(2);
            result.WholeLineText.Should().Be("    class Foo");
            result.LineHash.Should().Be("e1b4eea6db405a204a21bd5251c5385d");
        }

        [TestMethod]
        public void Create_WhenDiagnosticLocationIsNotInTheTextBuffer_ReturnsNull()
        {
            // Regression test for #1010
            // Arrange & Act
            const int outOfRangeLineNumber = 999;

            var location = Location.Create("C:\\anyfile.cs",
                    AnyTextSpan,
                    new LinePositionSpan(new LinePosition(outOfRangeLineNumber, 100), new LinePosition(outOfRangeLineNumber, 200)));
            var diagnostic = CreateDiagnostic(location);

            var result = SetupAndCreate(diagnostic, diagnosticProjectFilePath: ProjectInSolutionFilePath);

            // Assert
            result.Should().BeNull();
        }

        private Diagnostic CreateDiagnostic(Location location)
        {
            var anyDescriptor = new DiagnosticDescriptor("id",
                "title", "message", "category", DiagnosticSeverity.Hidden, true);

            return Diagnostic.Create(anyDescriptor, location);
        }

        private LiveIssue SetupAndCreate(Diagnostic diagnostic, string diagnosticProjectFilePath)
        {
            // Arrange
            var vsSolutionMock = SetupSolutionMocks(
                new KeyValuePair<string, string>(ProjectInSolutionFilePath, "{31D0DAAC-8606-40FE-8DF0-01784706EA3E}"));

            var projectId = ProjectId.CreateNewId();
            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Project1", "Assembly1", LanguageNames.CSharp,
                    diagnosticProjectFilePath))
                .AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(projectId), "name is unimportant.cs",
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

        private Mock<IVsSolution> SetupSolutionMocks(params KeyValuePair<string, string>[] pathToGuidMapping)
        {
            var vsSolutionMock = new Mock<IVsSolution>();
            uint fileCount = (uint)pathToGuidMapping.Length;

            // Calls to GetProjectFilesInSolution with a zero-length array return the number of solution items
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, 0, null, out fileCount))
                .Returns(VSConstants.S_OK);

            // Calls with the number of items get the array containing those items
            vsSolutionMock.Setup(x => x.GetProjectFilesInSolution(0, fileCount, It.IsAny<string[]>(), out fileCount))
                .Callback(new OutAction<uint, uint, string[], uint>((uint x, uint y, string[] names, out uint z) =>
                {
                    y.Should().Be(fileCount);

                    z = fileCount;
                    for (int i = 0; i < fileCount; i++)
                    {
                        names[i] = pathToGuidMapping[i].Key;
                    }
                }));

            // Return the guid matching the path
            vsSolutionMock.As<IVsSolution5>().Setup(x => x.GetGuidOfProjectFile(It.IsAny<string>()))
                .Returns((string path) =>
                {
                    var guidString = pathToGuidMapping.FirstOrDefault(item => item.Key == path).Value;
                    return new Guid(guidString);
                });

            return vsSolutionMock;
        }

    }
}
