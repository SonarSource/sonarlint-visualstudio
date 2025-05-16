using System.ComponentModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.ReviewHotspot;

[TestClass]
public class ReviewHotspotsViewModelTest
{
    private ReviewHotspotsViewModel testSubject;
    private HotspotStatus[] allowedStatuses;
    private readonly HotspotStatus currentStatus = HotspotStatus.Safe;

    [TestInitialize]
    public void TestInitialize()
    {
        allowedStatuses = [HotspotStatus.Acknowledge, HotspotStatus.Safe];

        testSubject = new ReviewHotspotsViewModel(currentStatus, allowedStatuses);
    }

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        testSubject.AllowedStatusViewModels.Should().HaveCount(allowedStatuses.Length);
        foreach (var allowedStatus in allowedStatuses)
        {
            testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.HotspotStatus == allowedStatus);
        }

        testSubject.SelectedStatusViewModel.HotspotStatus.Should().Be(currentStatus);
        testSubject.SelectedStatusViewModel.IsChecked.Should().BeTrue();
    }

    [TestMethod]
    public void Ctor_CurrentStatusNotInListOfAllowedStatuses_SetsSelectionToNull()
    {
        testSubject = new ReviewHotspotsViewModel(HotspotStatus.ToReview, allowedStatuses);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsNull_ReturnsFalse()
    {
        testSubject.SelectedStatusViewModel = null;

        testSubject.IsSubmitButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsSet_ReturnsTrue()
    {
        testSubject.SelectedStatusViewModel = new StatusViewModel(default, "title", "description");

        testSubject.IsSubmitButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void SelectedStatusViewModel_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedStatusViewModel = new StatusViewModel(default, "title", "description");

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedStatusViewModel)));
        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSubmitButtonEnabled)));
    }
}
