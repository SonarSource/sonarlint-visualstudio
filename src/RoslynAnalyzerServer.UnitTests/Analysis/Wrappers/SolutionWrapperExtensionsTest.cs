/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Wrappers;

[TestClass]
public class SolutionWrapperExtensionsTest
{
    private IRoslynSolutionWrapper solutionWrapper = null!;
    private IRoslynProjectWrapper projectWrapper1 = null!;
    private IRoslynProjectWrapper projectWrapper2 = null!;
    private IRoslynDocumentWrapper documentWrapper = null!;
    private const string TestFilePath = "file1.cs";

    [TestInitialize]
    public void TestInitialize()
    {
        solutionWrapper = Substitute.For<IRoslynSolutionWrapper>();
        projectWrapper1 = Substitute.For<IRoslynProjectWrapper>();
        projectWrapper2 = Substitute.For<IRoslynProjectWrapper>();
        solutionWrapper.Projects.Returns([projectWrapper1, projectWrapper2]);
        documentWrapper = Substitute.For<IRoslynDocumentWrapper>();
    }

    [TestMethod]
    public void ContainsDocument_FindsDocumentInFirstProject()
    {
        SetUpProjectContains(projectWrapper1, documentWrapper);
        SetUpProjectContains(projectWrapper2, null);

        solutionWrapper.ContainsDocument(TestFilePath, out var foundDocument).Should().BeTrue();

        foundDocument.Should().BeSameAs(documentWrapper);
    }

    [TestMethod]
    public void ContainsDocument_FindsDocumentInSecondProject()
    {
        SetUpProjectContains(projectWrapper1, null);
        SetUpProjectContains(projectWrapper2, documentWrapper);

        solutionWrapper.ContainsDocument(TestFilePath, out var foundDocument).Should().BeTrue();

        foundDocument.Should().BeSameAs(documentWrapper);
    }

    [TestMethod]
    public void ContainsDocument_DocumentNotFound_ReturnsFalse()
    {
        SetUpProjectContains(projectWrapper1, null);
        SetUpProjectContains(projectWrapper2, null);

        solutionWrapper.ContainsDocument(TestFilePath, out _).Should().BeFalse();
    }

    [TestMethod]
    public void ContainsDocument_NullProjects_ReturnsFalse()
    {
        solutionWrapper.Projects.Returns([]);

        solutionWrapper.ContainsDocument(TestFilePath, out _).Should().BeFalse();
    }

    private static void SetUpProjectContains(IRoslynProjectWrapper project, IRoslynDocumentWrapper? document) =>
        project.ContainsDocument(TestFilePath, out Arg.Any<IRoslynDocumentWrapper>()!)
            .Returns(x =>
            {
                x[1] = document;
                return document != null;
            });
}
