/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class BindingRequiredIndicatorTests
    {
        [TestMethod]
        public void IsBindingRequired_NoUnboundProjects_False()
        {
            var unboundProjects = Array.Empty<EnvDTE.Project>();

            var testSubject = CreateTestSubject(unboundProjects);

            var result = testSubject.IsBindingRequired();

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsBindingRequired_NoUnboundProjects_NoLogs()
        {
            var unboundProjects = Array.Empty<EnvDTE.Project>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(unboundProjects, logger);

            testSubject.IsBindingRequired();

            logger.OutputStrings.Should().BeEmpty();
        }

        [TestMethod]
        public void IsBindingRequired_HasUnboundProjects_True()
        {
            var unboundProjects = new[] { new ProjectMock("unbound.csproj") };

            var testSubject = CreateTestSubject(unboundProjects);

            var result = testSubject.IsBindingRequired();

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsBindingRequired_HasUnboundProjects_UnboundProjectsWrittenToLog()
        {
            var unboundProjects = new[] { new ProjectMock("unbound.csproj") };
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(unboundProjects, logger);

            testSubject.IsBindingRequired();

            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("unbound.csproj");
        }

        private BindingRequiredIndicator CreateTestSubject(EnvDTE.Project[] unboundProjects, ILogger logger = null)
        {
            var unboundProjectFinder = new Mock<IUnboundProjectFinder>();
            unboundProjectFinder.Setup(x => x.GetUnboundProjects()).Returns(unboundProjects);

            logger ??= new TestLogger();

            return new BindingRequiredIndicator(unboundProjectFinder.Object, logger);
        }
    }
}
