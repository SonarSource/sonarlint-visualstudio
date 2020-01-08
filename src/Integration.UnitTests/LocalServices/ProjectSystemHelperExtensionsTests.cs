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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class IProjectSystemHelperExtensionsTests
    {
        #region Test boilerplate

        private ConfigurableVsProjectSystemHelper projectSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            var sp = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(sp);
        }

        #endregion Test boilerplate

        [TestMethod]
        public void IProjectSystemHelperExtensions_IsKnownTestProject_ArgChecks()
        {
            // Arrange
            IVsHierarchy vsProject = new ProjectMock("myproject.proj");

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => IProjectSystemHelperExtensions.IsKnownTestProject(null, vsProject));
            Exceptions.Expect<ArgumentNullException>(() => IProjectSystemHelperExtensions.IsKnownTestProject(this.projectSystem, null));
        }

        [TestMethod]
        public void IProjectSystemHelperExtensions_IsKnownTestProject_IsTestProject_ReturnsTrue()
        {
            // Arrange
            var vsProject = new LegacyProjectMock("myproject.proj");
            vsProject.SetAggregateProjectTypeGuids(ProjectSystemHelper.TestProjectKindGuid);

            // Act + Assert
            IProjectSystemHelperExtensions.IsKnownTestProject(this.projectSystem, vsProject).Should().BeTrue("Expected project with test project kind to be known test project");
        }

        [TestMethod]
        public void IProjectSystemHelperExtensions_IsKnownTestProject_IsNotTestProject_ReturnsFalse()
        {
            // Arrange
            var vsProject = new LegacyProjectMock("myproject.proj");

            // Act + Assert
            IProjectSystemHelperExtensions.IsKnownTestProject(this.projectSystem, vsProject).Should().BeFalse("Expected project without test project kind NOT to be known test project");
        }
    }
}
