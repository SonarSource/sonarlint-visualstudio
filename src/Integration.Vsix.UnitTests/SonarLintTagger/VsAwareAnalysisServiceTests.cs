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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class VsAwareAnalysisServiceTests
{
    private const string AnalysisFilePath = "analysis/file/path";
    private readonly ITextSnapshot analysisTextSnapshot = Substitute.For<ITextSnapshot>();
    private IAnalysisService analysisService;
    private VsAwareAnalysisService testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        analysisService = Substitute.For<IAnalysisService>();
        threadHandling = CreateDefaultThreadHandling();

        testSubject = new VsAwareAnalysisService(analysisService, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VsAwareAnalysisService, IVsAwareAnalysisService>(
            MefTestHelpers.CreateExport<IAnalysisService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VsAwareAnalysisService>();

    [TestMethod]
    public void RequestAnalysis_ProvidesAnalysisParametersCorrectly()
    {
        testSubject.RequestAnalysis(new AnalysisSnapshot(AnalysisFilePath, analysisTextSnapshot));

        analysisService.Received(1).ScheduleAnalysis(AnalysisFilePath);
    }

    private static IThreadHandling CreateDefaultThreadHandling()
    {
        var mockThreadHandling = Substitute.For<IThreadHandling>();
        mockThreadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(async info => await info.Arg<Func<Task<int>>>()());
        return mockThreadHandling;
    }
}
