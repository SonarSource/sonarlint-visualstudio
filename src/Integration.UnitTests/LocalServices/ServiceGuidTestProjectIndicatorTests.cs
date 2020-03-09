using System;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
        public void IsTestProject_ProjectHasOneServiceInclude_NonTestServiceInclude_Null()
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
        public void IsTestProject_ProjectHasOneServiceInclude_HasTestServiceInclude_True()
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
        public void IsTestProject_ProjectHasMultipleTestServiceIncludes_NoTestServiceIncludes_Null()
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
        public void IsTestProject_ProjectHasMultipleTestServiceIncludes_HasTestServiceIncludes_True()
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
        public void IsTestProject_ProjectHasMultipleTestServiceIncludesInDifferentItemGroups_HasTestServiceIncludes_True()
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
    }
}
