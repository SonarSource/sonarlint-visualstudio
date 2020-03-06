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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectKindTestProjectIndicatorTests
    {
        private Mock<IVsHierarchy> vsHierarchy;
        private Mock<IProjectSystemHelper> projectSystemHelper;
        private ProjectKindTestProjectIndicator testSubject;

        private Project project;
        private IList<Guid> projectKinds;

        [TestInitialize]
        public void TestInitialize()
        {
            projectSystemHelper = new Mock<IProjectSystemHelper>();
            vsHierarchy = new Mock<IVsHierarchy>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IProjectSystemHelper))).Returns(projectSystemHelper.Object);

            project = Mock.Of<Project>();
            projectKinds = new List<Guid>();
            projectSystemHelper.Setup(x=> x.GetIVsHierarchy(project)).Returns(vsHierarchy.Object);
            projectSystemHelper.Setup(x => x.GetAggregateProjectKinds(vsHierarchy.Object)).Returns(projectKinds);

            testSubject = new ProjectKindTestProjectIndicator(serviceProvider.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new BuildPropertyTestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void IsTestProject_ProjectGuidIsMsTestGuid_True()
        {
            projectKinds.Add(ProjectSystemHelper.TestProjectKindGuid);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_ProjectGuidIsNotTestGuid_Null()
        {
            projectKinds.Add(new Guid(ProjectSystemHelper.CSharpProjectKind));

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }
    }
}
