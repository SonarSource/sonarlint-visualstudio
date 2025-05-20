using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList.ViewModels;

[TestClass]
public class LocationFilterViewModelTest
{
    [TestMethod]
    [DataRow(LocationFilter.CurrentDocument, "current")]
    [DataRow(LocationFilter.OpenDocuments, "open")]
    public void Ctor_InitializesProperties(LocationFilter locationFilter, string name)
    {
        var testSubject = new LocationFilterViewModel(locationFilter, name);

        testSubject.LocationFilter.Should().Be(locationFilter);
        testSubject.DisplayName.Should().Be(name);
    }
}
