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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ProjectRootCalculatorTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ProjectRootCalculator, IProjectRootCalculator>(
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IConfigurationProvider>(),
            MefTestHelpers.CreateExport<IStatefulServerBranchProvider>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ProjectRootCalculator>();
    }

    [TestMethod]
    public async Task CalculateBasedOnLocalPathAsync_StandaloneMode_ReturnsNull()
    {
        var testSubject = CreateTestSubject(out _, out var configurationProviderMock, out _);
        configurationProviderMock.Setup(x => x.GetConfiguration()).Returns(BindingConfiguration.Standalone);

        var result = await testSubject.CalculateBasedOnLocalPathAsync(@"c:\somepath", CancellationToken.None);

        result.Should().BeNull();
    }
    
    [TestMethod]
    public async Task CalculateBasedOnLocalPathAsync_ConnectedMode_ReturnsCorrectRoot()
    {
        const string projectKey = "projectKey";
        const string branch = "branch";
        
        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, out var configurationProviderMock, out var branchProviderMock);
        configurationProviderMock
            .Setup(x => x.GetConfiguration())
            .Returns(BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(){ProjectKey = projectKey},
                SonarLintMode.Connected,
                "somedir"));
        branchProviderMock
            .Setup(x => x.GetServerBranchNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);
        sonarQubeServiceMock
            .Setup(x => x.SearchFilesByNameAsync(projectKey, branch, "file.cs", CancellationToken.None))
            .ReturnsAsync(new []{@"dir\file.cs"});

        var result = await testSubject.CalculateBasedOnLocalPathAsync(@"c:\root\dir\file.cs", CancellationToken.None);

        result.Should().Be(@"c:\root"); // more extensive testing of the marching algorithm is done in PathHelper tests
    }

    private ProjectRootCalculator CreateTestSubject(out Mock<ISonarQubeService> sonarQubeServiceMock, 
        out Mock<IConfigurationProvider> configurationProviderMock,
        out Mock<IStatefulServerBranchProvider> statefulServerBranchProviderMock)
    {
        return new ProjectRootCalculator((sonarQubeServiceMock = new Mock<ISonarQubeService>(MockBehavior.Strict)).Object,
            (configurationProviderMock = new Mock<IConfigurationProvider>(MockBehavior.Strict)).Object,
            (statefulServerBranchProviderMock = new Mock<IStatefulServerBranchProvider>(MockBehavior.Strict)).Object);
    }
}
