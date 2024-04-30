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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots_List;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIdeHotspots;

[TestClass]
public class OpenIssueInIdeHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenHotspotInIdeHandler, IOpenHotspotInIdeHandler>(
            MefTestHelpers.CreateExport<IOpenInIdeHandlerImplementation>(),
            MefTestHelpers.CreateExport<IHotspotDetailsDtoToHotspotConverter>(),
            MefTestHelpers.CreateExport<IOpenInIDEHotspotsStore>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenHotspotInIdeHandler>();
    }
    
    [TestMethod]
    public void Show_CallsBaseHandler()
    {
        const string configScope = "configscope";
        var issue = new HotspotDetailsDto(default, default, default, default, default,
            default, default, default, default);
        var testSubject = CreateTestSubject(out var handler, out var converter, out _);
        
        testSubject.Show(issue, configScope);
        
        handler.Received().ShowIssue(issue, configScope, converter, IssueListIds.HotspotsId, testSubject);
    }

    [TestMethod]
    public void HandleConvertedIssue_AddsToHotspotStore()
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var storeAnalysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var testSubject = CreateTestSubject(out _, out _, out var hotspotsStore);
        hotspotsStore.GetOrAdd(analysisIssueVisualization).Returns(storeAnalysisIssueVisualization);

        testSubject.HandleConvertedIssue(analysisIssueVisualization).Should().BeSameAs(storeAnalysisIssueVisualization);

        hotspotsStore.Received().GetOrAdd(analysisIssueVisualization);
    }

    private OpenHotspotInIdeHandler CreateTestSubject(out IOpenInIdeHandlerImplementation openInIdeHandlerImplementation,
        out IHotspotDetailsDtoToHotspotConverter hotspotOpenInIdeConverter, 
        out IOpenInIDEHotspotsStore openInIdeHotspotsStore)
    {
        openInIdeHandlerImplementation = Substitute.For<IOpenInIdeHandlerImplementation>();
        hotspotOpenInIdeConverter = Substitute.For<IHotspotDetailsDtoToHotspotConverter>();
        openInIdeHotspotsStore = Substitute.For<IOpenInIDEHotspotsStore>();
        return new OpenHotspotInIdeHandler(openInIdeHandlerImplementation, hotspotOpenInIdeConverter, openInIdeHotspotsStore);
    } 
}
