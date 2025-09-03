/*
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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests;

[TestClass]
public class RoslynAnalysisServiceTests
{
    private static readonly List<ActiveRuleDto> DefaultActiveRules = new() { new ActiveRuleDto("sample-rule-id", new Dictionary<string, string> { { "paramKey", "paramValue" } }) };
    private static readonly Dictionary<string, string> DefaultAnalysisProperties = new() { { "sonar.cs.any", "any" } };
    private static readonly Dictionary<Language, RoslynAnalysisConfiguration> DefaultAnalysisConfigurations = new() { { Language.CSharp, new RoslynAnalysisConfiguration() } };
    private static readonly List<RoslynProjectAnalysisRequest> DefaultProjectAnalysisRequests = new() { new RoslynProjectAnalysisRequest(Substitute.For<IRoslynProjectWrapper>(), []) };
    private static readonly List<RoslynIssue> DefaultIssues = new() { new RoslynIssue("sample-rule-id", new RoslynIssueLocation("any", "any", new RoslynIssueTextRange(1, 1, 1, 1))) };
    private static readonly AnalyzerInfoDto DefaultAnalyzerInfoDto = new(false, false);
    private IRoslynSolutionAnalysisCommandProvider analysisCommandProvider = null!;
    private IRoslynAnalysisConfigurationProvider analysisConfigurationProvider = null!;

    private IRoslynAnalysisEngine analysisEngine = null!;
    private RoslynAnalysisService testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        analysisEngine = Substitute.For<IRoslynAnalysisEngine>();
        analysisConfigurationProvider = Substitute.For<IRoslynAnalysisConfigurationProvider>();
        analysisCommandProvider = Substitute.For<IRoslynSolutionAnalysisCommandProvider>();

        testSubject = new RoslynAnalysisService(analysisEngine, analysisConfigurationProvider, analysisCommandProvider);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalysisService, IRoslynAnalysisService>(
            MefTestHelpers.CreateExport<IRoslynAnalysisEngine>(),
            MefTestHelpers.CreateExport<IRoslynAnalysisConfigurationProvider>(),
            MefTestHelpers.CreateExport<IRoslynSolutionAnalysisCommandProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisService>();

    [TestMethod]
    public async Task AnalyzeAsync_PassesCorrectArgumentsToEngine()
    {
        string[] filePaths = [@"C:\file1.cs", @"C:\folder\file2.cs"];
        analysisConfigurationProvider.GetConfigurationAsync(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto).Returns(DefaultAnalysisConfigurations);
        analysisCommandProvider.GetAnalysisCommandsForCurrentSolution(Arg.Is<string[]>(x => x.SequenceEqual(filePaths))).Returns(DefaultProjectAnalysisRequests);
        analysisEngine.AnalyzeAsync(DefaultProjectAnalysisRequests, DefaultAnalysisConfigurations, Arg.Any<CancellationToken>()).Returns(DefaultIssues);
        var analysisRequest = new AnalysisRequest
        {
            FileNames = filePaths.Select(x => new FileUri(x)).ToList(), ActiveRules = DefaultActiveRules, AnalysisProperties = DefaultAnalysisProperties, AnalyzerInfo = DefaultAnalyzerInfoDto
        };

        var issues = await testSubject.AnalyzeAsync(analysisRequest, CancellationToken.None);

        issues.Should().BeSameAs(DefaultIssues);
    }
}
