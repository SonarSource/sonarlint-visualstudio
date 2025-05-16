using System.ComponentModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.ReviewHotspot;

[TestClass]
public class StatusViewModelTest
{
    [TestMethod]
    [DataRow(HotspotStatus.ToReview, "to review", "description1")]
    [DataRow(HotspotStatus.Acknowledge, "acknowledges", "description\ndescription2")]
    [DataRow(HotspotStatus.Fixed, "fixed", "description3")]
    [DataRow(HotspotStatus.Safe, "safe", "description\n\tdescription4")]
    public void Ctor_InitializesProperties(HotspotStatus status, string title, string description)
    {
        var testSubject = new StatusViewModel(status, title, description);

        testSubject.HotspotStatus.Should().Be(status);
        testSubject.Title.Should().Be(title);
        testSubject.Description.Should().Be(description);
        testSubject.IsChecked.Should().BeFalse();
    }

    [TestMethod]
    public void IsChecked_Set_RaisesEvents()
    {
        var testSubject = new StatusViewModel(default, "title", "description");
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.IsChecked = !testSubject.IsChecked;

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsChecked)));
    }
}
