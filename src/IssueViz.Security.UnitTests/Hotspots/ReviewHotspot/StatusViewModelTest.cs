using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.ReviewHotspot;

[TestClass]
public class StatusViewModelTest
{
    [TestMethod]
    [DataRow(HotspotStatus.TO_REVIEW, "to review", "description1")]
    [DataRow(HotspotStatus.ACKNOWLEDGED, "acknowledges", "description\ndescription2")]
    [DataRow(HotspotStatus.FIXED, "fixed", "description3")]
    [DataRow(HotspotStatus.SAFE, "safe", "description\n\tdescription4")]
    public void Ctor_InitializesProperties(HotspotStatus transition, string title, string description)
    {
        var testSubject = new StatusViewModel(transition, title, description);

        testSubject.HotspotStatus.Should().Be(transition);
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
