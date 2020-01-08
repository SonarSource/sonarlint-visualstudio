﻿/*
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    public class BindingRefactoringDumpingGroundTests
    {
        [TestMethod]
        public void BindingUtilities_IsProjectLevelBindingRequired()
        {
            // 1. C# -> binding is required
            var csProject = new ProjectMock("c:\\foo.proj");
            csProject.SetCSProjectKind();

            BindingRefactoringDumpingGround.IsProjectLevelBindingRequired(csProject).Should().BeTrue();

            // 2. VB.NET -> binding is required
            var vbProject = new ProjectMock("c:\\foo.proj");
            vbProject.SetVBProjectKind();

            BindingRefactoringDumpingGround.IsProjectLevelBindingRequired(vbProject).Should().BeTrue();

            // 3. Cpp -> binding is required
            var cppProject = new ProjectMock("c:\\foo.proj");
            cppProject.ProjectKind = ProjectSystemHelper.CppProjectKind;

            BindingRefactoringDumpingGround.IsProjectLevelBindingRequired(cppProject).Should().BeFalse();

            // 4. Other -> binding is not required
            var otherProject = new ProjectMock("c:\\foo.proj");
            otherProject.ProjectKind = "{" + Guid.NewGuid().ToString() + "}";

            BindingRefactoringDumpingGround.IsProjectLevelBindingRequired(otherProject).Should().BeFalse();
        }

    }
}
