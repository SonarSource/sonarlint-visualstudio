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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Tests.Helpers;

[TestClass]
public class ComponentKeyGeneratorTests
{
    [DataTestMethod]
    [DataRow(@"c:\dir\dir\root\dir\file.cs", @"c:\dir\dir\root\", "ProjectKey", "ProjectKey:dir/file.cs")]
    [DataRow(@"c:\dir\dir\root\file.cs", @"c:\dir\dir\root\", "ProjectKey", "ProjectKey:file.cs")]
    [DataRow(@"c:\dir\dir\root\dir1\dir2\file.cs", @"c:\dir\dir\root\", "Project:Key", "Project:Key:dir1/dir2/file.cs")]
    public void GetComponentKey(string filePath, string rootPath, string projectName, string expected)
    {
        ComponentKeyGenerator.GetComponentKey(filePath, rootPath, projectName).Should().Be(expected);
    }

    [TestMethod]
    public void GetComponentKey_NonRootedPath_Throws()
    {
        Action act = () => { ComponentKeyGenerator.GetComponentKey(@"c:\dir\dir\root\dir\file.cs", @".\non\rooted\", "project"); };

        act.Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be("Invalid root path format");
    }
    
    [TestMethod]
    public void GetComponentKey_NonFolderPath_Throws()
    {
        Action act = () => { ComponentKeyGenerator.GetComponentKey(@"c:\dir\dir\root\dir\file.cs", @"c:\not\folder", "project"); };

        act.Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be("Invalid root path format");
    }
    
    [TestMethod]
    public void GetComponentKey_NonMatchingRoot_Throws()
    {
        Action act = () => { ComponentKeyGenerator.GetComponentKey(@"c:\dir\dir\root\dir\file.cs", @"c:\not\same\folder\", "project"); };

        act.Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be("Local path is not under this root");
    }
}
