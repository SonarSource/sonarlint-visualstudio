using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots_List;
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
            MefTestHelpers.CreateExport<IOpenInIdeHandler>(),
            MefTestHelpers.CreateExport<IHotspotOpenInIdeConverter>(),
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
        var testSubject = CreateTestSubject(out _, out _, out var hotspotsStore);

        testSubject.HandleConvertedIssue(analysisIssueVisualization);

        hotspotsStore.Received().GetOrAdd(analysisIssueVisualization);
    }

    private OpenHotspotInIdeHandler CreateTestSubject(out IOpenInIdeHandler openInIdeHandler,
        out IHotspotOpenInIdeConverter hotspotOpenInIdeConverter, 
        out IOpenInIDEHotspotsStore openInIdeHotspotsStore)
    {
        openInIdeHandler = Substitute.For<IOpenInIdeHandler>();
        hotspotOpenInIdeConverter = Substitute.For<IHotspotOpenInIdeConverter>();
        openInIdeHotspotsStore = Substitute.For<IOpenInIDEHotspotsStore>();
        return new OpenHotspotInIdeHandler(openInIdeHandler, hotspotOpenInIdeConverter, openInIdeHotspotsStore);
    } 
}
