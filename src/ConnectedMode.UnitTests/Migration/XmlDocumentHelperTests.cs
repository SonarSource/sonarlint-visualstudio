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

using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration;

[TestClass]
public class XmlDocumentHelperTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<XmlDocumentHelper, IXmlDocumentHelper>();

    [TestMethod]
    public void MefCtor_CheckTypeIsShared() =>
        MefTestHelpers.CheckIsSingletonMefComponent<XmlDocumentHelper>();
    
    [TestMethod]
    public void LoadFromString_SaveToString_NoModification_ReturnsEquivalentString()
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

        testSubject.SaveToString(testSubject.LoadFromString(input)).Should().BeEquivalentTo(input);
    }
    
    [TestMethod]
    public void LoadFromString_SaveToString_NoModification_XmlHeaderPreserved()
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

        testSubject.SaveToString(testSubject.LoadFromString(input)).Should().BeEquivalentTo(input);
    }
    
    [TestMethod]
    public void LoadFromString_SaveToString_Modification_SavesCorrectly()
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
        
        var expected =
@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>SDK_VB_DoublyIndirectRefToGenerated</RootNamespace>
    <TargetFramework>net6.0</TargetFramework>

    
  </PropertyGroup>

  <ItemGroup>
    
  </ItemGroup>

</Project>";

        var testSubject = new XmlDocumentHelper();

        var document = testSubject.LoadFromString(input);
        var xmlNode = document.GetElementsByTagName("CodeAnalysisRuleSet")[0];
        xmlNode.ParentNode.RemoveChild(xmlNode);

        testSubject.SaveToString(document).Should().BeEquivalentTo(expected);
    }
}
