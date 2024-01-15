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

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class RoslynProjectProviderTests
    {
        [TestMethod]
        [Ignore] // VsWorkspace is not mockable -> can't create an export
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RoslynProjectProvider, IRoslynProjectProvider>(
                MefTestHelpers.CreateExport<VisualStudioWorkspace>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<RoslynProjectProvider>();

        [TestMethod]
        public void Get_NullSolution_NoError()
        {
            var workspace = new Mock<IVisualStudioWorkspace>();
            workspace.Setup(x => x.CurrentSolution).Returns((Solution)null);

            var testSubject = CreateTestSubject(workspace.Object);

            var actual = testSubject.Get().ToArray();

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
            workspace.Verify(x => x.CurrentSolution, Times.AtLeastOnce());
        }

        [TestMethod]
        public void Get_NoProjects_NoError()
        {
            var workspace = CreateWorkspace(/* no projects */);
            var testSubject = CreateTestSubject(workspace.Object);

            var actual = testSubject.Get().ToArray();

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
            workspace.Verify(x => x.CurrentSolution, Times.AtLeastOnce());
        }

        [TestMethod]
        public void Get_HasProjects_ReturnsExpectedProjects()
        {
            var project1 = CreateProjectInfo();
            var project2 = CreateProjectInfo();
            var project3 = CreateProjectInfo();

            var workspace = CreateWorkspace(project1, project2, project3);

            var testSubject = CreateTestSubject(workspace.Object);

            // Act
            var actual = testSubject.Get().ToArray();

            actual.Should().HaveCount(3);
            var actualIds = actual.Select(x => x.Id).ToArray();
            
            actualIds.Should().ContainInOrder(project1.Id, project2.Id, project3.Id);
        }

        private static RoslynProjectProvider CreateTestSubject(IVisualStudioWorkspace workspace = null)
        {
            workspace ??= Mock.Of<IVisualStudioWorkspace>();
            return new RoslynProjectProvider(workspace);
        }

        private static Mock<IVisualStudioWorkspace> CreateWorkspace(params ProjectInfo[] projectInfos)
        {
            // Create a real solution with a list of projects
            var solution = CreateSolution(projectInfos);

            var workspace = new Mock<IVisualStudioWorkspace>();
            workspace.Setup(x => x.CurrentSolution).Returns(solution);

            return workspace;
        }

        private static Solution CreateSolution(params ProjectInfo[] projectInfos)
        {
            // We can't directly create or mock a Roslyn Solution. However, we can 
            // create a real Roslyn AdhocWorkspace and return it's Solution
            AdhocWorkspace adhocWorkspace = new AdhocWorkspace();

            var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null,
                projectInfos);
            adhocWorkspace.AddSolution(slnInfo);

            return adhocWorkspace.CurrentSolution;
        }

        private static ProjectInfo CreateProjectInfo()
        {
            var guid = Guid.NewGuid();
            return ProjectInfo.Create(ProjectId.CreateFromSerialized(guid), VersionStamp.Default, guid.ToString(), guid.ToString(), LanguageNames.CSharp);
        }
    }
}
