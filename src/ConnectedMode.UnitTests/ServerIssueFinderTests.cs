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

using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ServerIssueFinderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ServerIssueFinder, IServerIssueFinder>(
            MefTestHelpers.CreateExport<IProjectRootCalculator>(),
            MefTestHelpers.CreateExport<IIssueMatcher>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IStatefulServerBranchProvider>(),
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ServerIssueFinder>();
    }
    
    [TestMethod]
    public void FindServerIssueAsync_UIThread_Throws()
    {
        var serverIssueFinder = CreateTestSubject(out _, out _, out _, out _, out _, out var threadHandlingMock);
        var exception = new Exception();
        threadHandlingMock.Setup(x => x.ThrowIfOnUIThread()).Throws(exception);

        Func<Task> act = async () => await serverIssueFinder.FindServerIssueAsync(Mock.Of<IFilterableIssue>(), CancellationToken.None);

        act.Should().Throw<Exception>().Which.Should().BeSameAs(exception);
    }
    
    [TestMethod]
    public async Task FindServerIssueAsync_StandaloneMode_ReturnsNull()
    {
        var serverIssueFinder = CreateTestSubject(out _, out _, out var activeSolutionBoundTrackerMock, out _, out _, out _);
        SetUpStandalone(activeSolutionBoundTrackerMock);

        var result = await serverIssueFinder.FindServerIssueAsync(Mock.Of<IFilterableIssue>(), CancellationToken.None);

        result.Should().BeNull();
    }
    
    [TestMethod]
    public async Task FindServerIssueAsync_RootCantBeCalculated_ReturnsNull()
    {
        const string filePath = @"c:\a\b\c";
        
        var serverIssueFinder = CreateTestSubject(out var projectRootCalculatorMock, out _, out var activeSolutionBoundTrackerMock, out _, out _, out _);
        SetUpBinding(activeSolutionBoundTrackerMock, "project");
        projectRootCalculatorMock.Setup(x => x.CalculateBasedOnLocalPathAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var result = await serverIssueFinder.FindServerIssueAsync(CreateIssue("rule", filePath), CancellationToken.None);

        result.Should().BeNull();
    }
    
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task FindServerIssueAsync_ReturnsMatchingServerIssueIfMatched(bool isMatch)
    {
        const string ruleId = "rule";
        const string filePath = @"c:\a\b\c";
        const string root = @"c:\a\";
        const string projectKey = "project";
        const string branch = "branch123";
        var localIssue = CreateIssue(ruleId, filePath);
        var serverIssues = new[] { CreateServerIssue(), CreateServerIssue() };
        
        var serverIssueFinder = CreateTestSubject(out var projectRootCalculatorMock, 
            out var issueMatcherMock, 
            out var activeSolutionBoundTrackerMock, 
            out var statefulServerBranchProviderMock, 
            out var sonarQubeServiceMock, 
            out _);
        SetUpBinding(activeSolutionBoundTrackerMock, projectKey);
        projectRootCalculatorMock
            .Setup(x => x.CalculateBasedOnLocalPathAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
        statefulServerBranchProviderMock
            .Setup(x => x.GetServerBranchNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);
        sonarQubeServiceMock
            .Setup(x => x.GetIssuesForComponentAsync(projectKey, branch, ComponentKeyGenerator.GetComponentKey(filePath, root, projectKey), ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverIssues);
        issueMatcherMock
            .Setup(x => x.GetFirstLikelyMatchFromSameFileOrNull(localIssue, serverIssues))
            .Returns(isMatch ? serverIssues[1] : null);

        var result = await serverIssueFinder.FindServerIssueAsync(localIssue, CancellationToken.None);
        
        if (isMatch)
        {
            result.Should().BeSameAs(serverIssues[1]);
        }
        else
        {
            result.Should().BeNull();
        }
    }

    private SonarQubeIssue CreateServerIssue()
    {
        return new SonarQubeIssue("test", "test", "test", "test", "test", "test", false, SonarQubeIssueSeverity.Info,
            DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null);
    }
    
    private IFilterableIssue CreateIssue(string ruleId, string filePath, int? startLine = null, string lineHash = null) =>
        new TestFilterableIssue
        {
            RuleId = ruleId,
            FilePath = filePath,
            StartLine = startLine,
            LineHash = lineHash
        };

    private void SetUpStandalone(Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock) =>
        SetUpBinding(activeSolutionBoundTrackerMock, null);
    
    private void SetUpBinding(Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock, string projectKey)
    {
        activeSolutionBoundTrackerMock.SetupGet(x => x.CurrentConfiguration)
            .Returns(projectKey == null
                ? BindingConfiguration.Standalone
                : new BindingConfiguration(new BoundSonarQubeProject { ProjectKey = projectKey }, SonarLintMode.Connected, default));
    }
    
    private ServerIssueFinder CreateTestSubject(out Mock<IProjectRootCalculator> projectRootCalculatorMock,
        out Mock<IIssueMatcher> issueMatcherMock,
        out Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock,
        out Mock<IStatefulServerBranchProvider> statefulServerBranchProviderMock,
        out Mock<ISonarQubeService> sonarQubeServiceMock,
        out Mock<IThreadHandling> threadHandlingMock)
    {
        return new ServerIssueFinder((projectRootCalculatorMock = new Mock<IProjectRootCalculator>(MockBehavior.Strict)).Object,
            (issueMatcherMock = new Mock<IIssueMatcher>(MockBehavior.Strict)).Object,
            (activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>(MockBehavior.Strict)).Object,
            (statefulServerBranchProviderMock = new Mock<IStatefulServerBranchProvider>(MockBehavior.Strict)).Object,
            (sonarQubeServiceMock = new Mock<ISonarQubeService>(MockBehavior.Strict)).Object,
            (threadHandlingMock = new Mock<IThreadHandling>()).Object); // not strict since does not affect logic
    }
}
