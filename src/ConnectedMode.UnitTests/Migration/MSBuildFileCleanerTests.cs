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

using System.Threading;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MSBuildFileCleanerTests
    {
        private static readonly LegacySettings AnyLegacySettings = new LegacySettings("c:\\any\\root\\folder", "csharpruleset", "csharpXML", "vbruleset", "vbXML");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MSBuildFileCleaner, IFileCleaner>(
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IXmlDocumentHelper>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<MSBuildFileCleaner>();

        [TestMethod]
        public void Clean_EmptyDocument_NoErrors_ReturnsNull()
        {
            const string content = "<Project/>";
            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(content, AnyLegacySettings, CancellationToken.None);

            actual.Should().Be(MSBuildFileCleaner.Unchanged);
        }

        [TestMethod]
        public void Clean_AdditionalFileRefsExist_AreRemoved()
        {
            const string content =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net6.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

	<ItemGroup Label=""Should be removed - CSharp"">
        <!-- simple reference -->
		<AdditionalFiles Include=""..\..\.sonarlint\my_project_key\CSharp\SonarLint.xml"" />

        <!-- with conditions -->
		<AdditionalFiles Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" Include=""..\..\.sonarlint\my_project_key\CSharp\SonarLint.xml"" Link=""SonarLint.xml"" />
	</ItemGroup>

	<ItemGroup Label=""Should be removed - VB"">
        <!-- different cases -->
		<AdditionalFiles Include=""..\..\.sonarlint\my_project_key\vb\SonarLint.xml"" />
		<AdditionalFiles Include=""..\..\.sonarlint\my_project_key\VB\SONARLINT.XML"" />
	</ItemGroup>

	<ItemGroup>
        <!-- should not be removed - shouldn't match -->
		<AdditionalFiles Include=""..\..\.sonarlint\wrong_my_project_key\vb\SonarLint.xml"" />
		<AdditionalFiles Include=""some other path`vb\SonarLint.xml"" />
	</ItemGroup>
</Project>";

            var settings = new LegacySettings("c:\\any\\root\\folder",
                "XXX",
                ".sonarlint\\my_project_key\\CSharp\\SonarLint.xml",
                "XXX",
                ".sonarlint\\my_project_key\\vb\\SonarLint.xml");
        
            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(content, settings, CancellationToken.None);

            const string expected =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net6.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

	<ItemGroup Label=""Should be removed - CSharp"">
        <!-- simple reference -->
		

        <!-- with conditions -->
		
	</ItemGroup>

	<ItemGroup Label=""Should be removed - VB"">
        <!-- different cases -->
		
		
	</ItemGroup>

	<ItemGroup>
        <!-- should not be removed - shouldn't match -->
		<AdditionalFiles Include=""..\..\.sonarlint\wrong_my_project_key\vb\SonarLint.xml"" />
		<AdditionalFiles Include=""some other path`vb\SonarLint.xml"" />
	</ItemGroup>
</Project>";

            actual.Should().Be(expected);
        }

        private static MSBuildFileCleaner CreateTestSubject(ILogger logger = null)
        {
            logger ??= new TestLogger(logToConsole: true);
            return new MSBuildFileCleaner(logger);
        }
    }
}
