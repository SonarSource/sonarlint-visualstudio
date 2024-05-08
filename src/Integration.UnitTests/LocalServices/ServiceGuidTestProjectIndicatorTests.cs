/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.IO.Abstractions;
using SonarLint.VisualStudio.Integration.LocalServices.TestProjectIndicators;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ServiceGuidTestProjectIndicatorTests
    {
        private Mock<IFileSystem> fileSystem;
        private ProjectMock project;
        private ServiceGuidTestProjectIndicator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystem = new Mock<IFileSystem>();
            project = new ProjectMock("test.csproj");

            testSubject = new ServiceGuidTestProjectIndicator(fileSystem.Object);
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new ServiceGuidTestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void IsTestProject_ProjectHasNoItemGroups_Null()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasNoServiceIncludes_Null()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasOneServiceInclude_NotTestServiceInclude_Null()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{B4F97281-0DBD-4835-9ED8-7DFB966E87FF}"" />
  </ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasOneServiceInclude_TestServiceInclude_True()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />
  </ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasOneServiceIncludeThatIsCommentedOut_Null()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <!--<Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />-->
</ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasMultipleServiceIncludes_NotTestServiceInclude_Null()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{B4F97281-0DBD-4835-9ED8-7DFB966E87FF}"" />
    <Service Include=""{fd512ce6-bcff-444b-bbc8-1c9eaf5e2c1b}"" />
  </ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasMultipleServiceIncludes_TestServiceInclude_True()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{B4F97281-0DBD-4835-9ED8-7DFB966E87FF}"" />
    <Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />
  </ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasMultipleServiceIncludesInDifferentItemGroups_TestServiceInclude_True()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{B4F97281-0DBD-4835-9ED8-7DFB966E87FF}"" />
  </ItemGroup>
  <ItemGroup>
    <Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />
  </ItemGroup>
</Project>";

            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_ExceptionOccurs_Null()
        {
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Throws<ArgumentException>();

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_CriticalExceptionOccurs_NotSuppressed()
        {
            var critialException = new StackOverflowException("BANG!");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Throws(critialException);

            Action act = () => testSubject.IsTestProject(project);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("BANG!");
        }
    }
}
