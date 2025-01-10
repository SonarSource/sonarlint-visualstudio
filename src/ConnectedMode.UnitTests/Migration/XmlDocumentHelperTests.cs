﻿/*
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

using SonarLint.VisualStudio.ConnectedMode.Migration;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration;

[TestClass]
public class XmlDocumentHelperTests
{
    [TestMethod]
    public void TryLoadFromString_NotXml_ReturnsFalse()
    {
        var input = "{'json':'isnotsupported'}";

        var testSubject = new XmlDocumentHelper();

        testSubject.TryLoadFromString(input, out _).Should().BeFalse();
    }

    [TestMethod]
    public void TryLoadFromString_SaveToString_NoModification_ReturnsEquivalentString()
    {
        var input =
@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>SDK_VB_DoublyIndirectRefToGenerated</RootNamespace>
    <TargetFramework>net6.0</TargetFramework>

    <CodeAnalysisRuleSet>Local_Direct.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>

  <ItemGroup>

  </ItemGroup>

</Project>";

        var testSubject = new XmlDocumentHelper();

        testSubject.TryLoadFromString(input, out var document).Should().BeTrue();
        testSubject.SaveToString(document).Should().BeEquivalentTo(input);
    }

    [TestMethod]
    public void TryLoadFromString_SaveToString_NoModification_XmlHeaderPreserved()
    {
        var input =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>SDK_VB_DoublyIndirectRefToGenerated</RootNamespace>
    <TargetFramework>net6.0</TargetFramework>

    <CodeAnalysisRuleSet>Local_Direct.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>

  <ItemGroup>

  </ItemGroup>

</Project>";

        var testSubject = new XmlDocumentHelper();

        testSubject.TryLoadFromString(input, out var document).Should().BeTrue();
        testSubject.SaveToString(document).Should().BeEquivalentTo(input);
    }

    [TestMethod]
    public void TryLoadFromString_SaveToString_Modification_SavesCorrectly()
    {
        var original =
@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>SDK_VB_DoublyIndirectRefToGenerated</RootNamespace>
    <TargetFramework>net6.0</TargetFramework>

    <CodeAnalysisRuleSet>Local_Direct.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>

  <ItemGroup>

  </ItemGroup>

</Project>";

        // using a regular string because git/rider removes whitespace characters it shouldn't (the 4 space characters that go before <CodeAnalysis... in the original string)
        var expected =
            "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n\r\n  <PropertyGroup>\r\n    <OutputType>Exe</OutputType>\r\n    <RootNamespace>SDK_VB_DoublyIndirectRefToGenerated</RootNamespace>\r\n    <TargetFramework>net6.0</TargetFramework>\r\n\r\n    \r\n  </PropertyGroup>\r\n\r\n  <ItemGroup>\r\n\r\n  </ItemGroup>\r\n\r\n</Project>";

        var testSubject = new XmlDocumentHelper();

        testSubject.TryLoadFromString(original, out var document).Should().BeTrue();
        var xmlNode = document.GetElementsByTagName("CodeAnalysisRuleSet")[0];
        xmlNode.ParentNode.RemoveChild(xmlNode);

        testSubject.SaveToString(document).Should().BeEquivalentTo(expected);
    }
}
