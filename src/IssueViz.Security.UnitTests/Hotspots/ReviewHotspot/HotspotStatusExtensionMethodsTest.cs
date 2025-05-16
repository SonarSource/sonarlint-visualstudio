using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.ReviewHotspot;

using CoreHotspotStatus = Core.Analysis.HotspotStatus;
using SlCoreHotspotStatus = SLCore.Common.Models.HotspotStatus;

[TestClass]
public class HotspotStatusExtensionMethodsTest
{
    [TestMethod]
    [DataRow(SlCoreHotspotStatus.TO_REVIEW, CoreHotspotStatus.ToReview)]
    [DataRow(SlCoreHotspotStatus.ACKNOWLEDGED, CoreHotspotStatus.Acknowledged)]
    [DataRow(SlCoreHotspotStatus.FIXED, CoreHotspotStatus.Fixed)]
    [DataRow(SlCoreHotspotStatus.SAFE, CoreHotspotStatus.Safe)]
    public void ToSlCoreHotspotStatus_ConvertsCorrectly(SlCoreHotspotStatus slCoreStatus, CoreHotspotStatus coreStatus)
    {
        var result = coreStatus.ToSlCoreHotspotStatus();

        result.Should().Be(slCoreStatus);
    }

    [TestMethod]
    public void ToSlCoreHotspotStatus_EnumValueStringsAreSame()
    {
        var coreStatusList = Enum.GetValues(typeof(CoreHotspotStatus)).Cast<CoreHotspotStatus>();
        foreach (var coreStatus in coreStatusList)
        {
            var slCoreStatus = coreStatus.ToSlCoreHotspotStatus().ToString();
            slCoreStatus.Replace("_", string.Empty).ToLower().Should().Be(coreStatus.ToString().ToLower());
        }
    }
}
