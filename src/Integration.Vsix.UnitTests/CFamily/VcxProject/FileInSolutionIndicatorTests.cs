/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using EnvDTE;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class FileInSolutionIndicatorTests
{
    private FileInSolutionIndicator testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = Substitute.For<IThreadHandling>();
        testSubject = new FileInSolutionIndicator(threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<FileInSolutionIndicator, IFileInSolutionIndicator>(
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<FileInSolutionIndicator>();

    [TestMethod]
    public void Get_FileIsNotInSolution_ReturnsFalse()
    {
        var projectItemMock = CreateMockProjectItem("c:\\foo\\SingleFileISense\\xxx.vcxproj");

        testSubject.IsFileInSolution(projectItemMock.Object)
            .Should().BeFalse();

        projectItemMock.Verify(x => x.ContainingProject);
        threadHandling.Received().ThrowIfNotOnUIThread();
    }

    [TestMethod]
    public void Get_NoConfigurationManager_ReturnsFalse()
    {
        var projectItemMock = CreateMockProjectItem("c:\\foo\\Any\\xxx.vcxproj");
        projectItemMock.SetupGet(x => x.ConfigurationManager).Returns(null as ConfigurationManager);

        testSubject.IsFileInSolution(projectItemMock.Object)
            .Should().BeFalse();

        projectItemMock.Verify(x => x.ContainingProject);
        projectItemMock.Verify(x => x.ConfigurationManager);
        threadHandling.Received().ThrowIfNotOnUIThread();
    }

    [TestMethod]
    public void Get_NoConfiguration_ReturnsFalse()
    {
        var projectItemMock = CreateMockProjectItem("c:\\foo\\Any\\xxx.vcxproj");
        var configurationManagerMock = Mock.Get(projectItemMock.Object.ConfigurationManager);
        configurationManagerMock.SetupGet(x => x.ActiveConfiguration).Returns(null as Configuration);

        testSubject.IsFileInSolution(projectItemMock.Object)
            .Should().BeFalse();

        projectItemMock.Verify(x => x.ContainingProject);
        projectItemMock.Verify(x => x.ConfigurationManager);
        configurationManagerMock.Verify(x => x.ActiveConfiguration);
        threadHandling.Received().ThrowIfNotOnUIThread();
    }

    [TestMethod]
    public void Get_FileInSolution_ReturnsTrue()
    {
        var projectItemMock = CreateMockProjectItem("c:\\foo\\Any\\xxx.vcxproj");

        testSubject.IsFileInSolution(projectItemMock.Object)
            .Should().BeTrue();

        projectItemMock.Verify(x => x.ContainingProject);
        projectItemMock.Verify(x => x.ConfigurationManager);
        threadHandling.Received().ThrowIfNotOnUIThread();
    }

    [TestMethod]
    public void Get_FailureToCheckIfFileIsInSolution_NonCriticalException_ExceptionCaughtAndFalseReturned()
    {
        var projectItemMock = CreateMockProjectItem("c:\\foo\\Any\\xxx.vcxproj");
        projectItemMock.Setup(x => x.ContainingProject).Throws<NotImplementedException>();

        testSubject.IsFileInSolution(projectItemMock.Object)
            .Should().BeFalse();

        projectItemMock.Verify(x => x.ContainingProject);
        threadHandling.Received().ThrowIfNotOnUIThread();
    }

    [TestMethod]
    public void Get_FailureToCheckIfFileIsInSolution_CriticalException_ExceptionThrown()
    {
        var projectItemMock = CreateMockProjectItem("c:\\foo\\Any\\xxx.vcxproj");
        projectItemMock.Setup(x => x.ContainingProject).Throws<DivideByZeroException>();

        Action act = () => testSubject.IsFileInSolution(projectItemMock.Object);

        act.Should().Throw<DivideByZeroException>();
        projectItemMock.Verify(x => x.ContainingProject);
        threadHandling.Received().ThrowIfNotOnUIThread();
    }
}
