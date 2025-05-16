using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.ReviewHotspot;

[TestClass]
public class ReviewHotspotsViewModelTest
{
    private ReviewHotspotsViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new ReviewHotspotsViewModel();

    [TestMethod]
    public void Cto_InitializesProperties()
    {
        testSubject.SelectedStatusViewModel.Should().BeNull();
        testSubject.AllowedStatusViewModels.Should().BeEmpty();
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

    [TestMethod]
    public void InitializeStatuses_InitializesAllowedStatusViewModels()
    {
        var transitions = new[] { HotspotStatus.ACKNOWLEDGED, HotspotStatus.SAFE };

        testSubject.InitializeStatuses(transitions);

        testSubject.AllowedStatusViewModels.Should().HaveCount(2);
        testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.HotspotStatus == HotspotStatus.ACKNOWLEDGED);
        testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.HotspotStatus == HotspotStatus.SAFE);
    }

    [TestMethod]
    public void InitializeStatuses_ClearsPreviousStatuses()
    {
        testSubject.InitializeStatuses([HotspotStatus.FIXED]);
        testSubject.InitializeStatuses([HotspotStatus.TO_REVIEW]);

        testSubject.AllowedStatusViewModels.Should().HaveCount(1);
        testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.HotspotStatus == HotspotStatus.TO_REVIEW);
    }

    [TestMethod]
    public void InitializeStatuses_ClearsSelectedStatusViewModel()
    {
        var transitions = new[] { HotspotStatus.SAFE, HotspotStatus.TO_REVIEW };
        testSubject.SelectedStatusViewModel = new StatusViewModel(HotspotStatus.SAFE, "title", "description");

        testSubject.InitializeStatuses(transitions);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }

    [TestMethod]
    public void InitializeStatusesInitializes_ClearsIsChecked()
    {
        var transitions = new[] { HotspotStatus.SAFE, HotspotStatus.TO_REVIEW };
        testSubject.InitializeStatuses(transitions);

        testSubject.AllowedStatusViewModels.ToList().ForEach(vm => vm.IsChecked = true);
        testSubject.InitializeStatuses(transitions);

        testSubject.AllowedStatusViewModels.All(x => !x.IsChecked).Should().BeTrue();
    }
}
