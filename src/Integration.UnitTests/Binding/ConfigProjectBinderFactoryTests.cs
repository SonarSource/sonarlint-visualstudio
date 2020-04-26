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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    public class ConfigProjectBinderFactoryTests
    {
        [TestMethod]
        public void Get_CSharpProject_RoslynProjectBinderReturned()
        {
            var project = new ProjectMock("c:\\foo.proj");
            project.SetCSProjectKind();

            var configProjectBinder = new ConfigProjectBinderFactory().Get(project);
            configProjectBinder.Should().BeOfType<RoslynConfigProjectBinder>();
        }

        [TestMethod]
        public void Get_VbNetProject_RoslynProjectBinderReturned()
        {
            var project = new ProjectMock("c:\\foo.proj");
            project.SetVBProjectKind();

            var configProjectBinder = new ConfigProjectBinderFactory().Get(project);
            configProjectBinder.Should().BeOfType<RoslynConfigProjectBinder>();
        }

        [TestMethod]
        public void Get_CppProject_NullReturned()
        {
            var project = new ProjectMock("c:\\foo.proj");
            project.ProjectKind = ProjectSystemHelper.CppProjectKind;

            var configProjectBinder = new ConfigProjectBinderFactory().Get(project);
            configProjectBinder.Should().BeNull();
        }

        [TestMethod]
        public void Get_NonRoslynProject_NullReturned()
        {
            var project = new ProjectMock("c:\\foo.proj");
            project.ProjectKind = "{" + Guid.NewGuid() + "}";

            var configProjectBinder = new ConfigProjectBinderFactory().Get(project);
            configProjectBinder.Should().BeNull();
        }
    }
}
