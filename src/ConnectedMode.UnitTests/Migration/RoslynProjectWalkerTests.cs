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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class RoslynProjectWalkerTests
    {
        [TestMethod]
        [Ignore] // VsWorkspace is not mockable -> can't create an export
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RoslynProjectWalker, IRoslynProjectWalker>(
                MefTestHelpers.CreateExport<VisualStudioWorkspace>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<RoslynProjectWalker>();

        [TestMethod]
        public void Enumerate_NullSolution_NoError()
        {
            var workspace = new Mock<IVisualStudioWorkspace>();
            workspace.Setup(x => x.CurrentSolution).Returns((Solution)null);

            var testSubject = CreateTestSubject(workspace.Object);

            var actual = testSubject.Walk().ToArray();

            actual.Should().HaveCount(0);
            workspace.Verify(x => x.CurrentSolution, Times.AtLeastOnce());
        }

        [TestMethod]
        public void Enumerate_NoProjects_NoError()
        {
            var workspace = CreateWorkspace(/* no projects */);
            var testSubject = CreateTestSubject(workspace.Object);

            var actual = testSubject.Walk().ToArray();

            actual.Should().HaveCount(0);
            workspace.Verify(x => x.CurrentSolution, Times.AtLeastOnce());
        }

        [TestMethod]
        public void Enumerate_HasProjects_ProjectsWithHierachiesReturned()
        {
            var guid2 = Guid.NewGuid(); var hierarchy2 = Mock.Of<IVsHierarchy>();
            var guid4 = Guid.NewGuid(); var hierarchy4 = Mock.Of<IVsHierarchy>();

            var mapping = new Tuple<Guid, IVsHierarchy>[]
                {
                    new (Guid.NewGuid(), null),
                    new (guid2, hierarchy2),
                    new (Guid.NewGuid(), null),
                    new (guid4, hierarchy4),
                };

            var workspace = CreateWorkspace(mapping);

            var testSubject = CreateTestSubject(workspace.Object);

            // Act
            var actual = testSubject.Walk().ToArray();

            actual.Should().HaveCount(2);
            actual.Should().ContainInOrder(hierarchy2, hierarchy4);
        }
        private static RoslynProjectWalker CreateTestSubject(IVisualStudioWorkspace workspace = null, ILogger logger = null)
        {
            workspace ??= Mock.Of<IVisualStudioWorkspace>();
            logger ??= new TestLogger(logToConsole: true);
            return new RoslynProjectWalker(workspace, logger);
        }

        private static Mock<IVisualStudioWorkspace> CreateWorkspace(params Tuple<Guid, IVsHierarchy>[] projectGuidToHierachyMap)
        {
            projectGuidToHierachyMap ??= Array.Empty<Tuple<Guid, IVsHierarchy>>();

            // Create a real solution with a list of projects
            var solution = CreateSolution(projectGuidToHierachyMap);

            var workspace = new Mock<IVisualStudioWorkspace>();
            workspace.Setup(x => x.CurrentSolution).Returns(solution);

            // Setup up the ProjectId -> IVsHierarchy lookup
            workspace.Setup(x => x.GetHierarchy(It.IsAny<ProjectId>()))
                .Returns<ProjectId>(projecId => projectGuidToHierachyMap.FirstOrDefault(x => projecId.Id == x.Item1).Item2);
            return workspace;
        }

        private static Solution CreateSolution(params Tuple<Guid, IVsHierarchy>[] projectGuidToHierachyMap)
        {
            // We can't directly create or mock a Roslyn Solution. However, we can 
            // create a real Roslyn AdhocWorkspace and return it's Solution
            AdhocWorkspace adhocWorkspace = new AdhocWorkspace();

            var projects = projectGuidToHierachyMap.Select(x => CreateProjectInfo(x.Item1)).ToArray();

            var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null, 
                projects);
            var solution = adhocWorkspace.AddSolution(slnInfo);

            return adhocWorkspace.CurrentSolution;
        }

        private static ProjectInfo CreateProjectInfo(Guid guid)
             => ProjectInfo.Create(ProjectId.CreateFromSerialized(guid), VersionStamp.Default, guid.ToString(), guid.ToString(), LanguageNames.CSharp);
    }
}
