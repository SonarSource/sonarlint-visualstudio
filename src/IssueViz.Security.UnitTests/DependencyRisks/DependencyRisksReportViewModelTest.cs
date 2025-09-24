using System.Windows;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class DependencyRisksReportViewModelTest
{
    private IDependencyRisksStore dependencyRisksStore;
    private IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler;
    private IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler;
    private IMessageBox messageBox;
    private DependencyRisksReportViewModel testSubject;

    [TestInitialize]
    public void Initialize()
    {
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        showDependencyRiskInBrowserHandler = Substitute.For<IShowDependencyRiskInBrowserHandler>();
        changeDependencyRiskStatusHandler = Substitute.For<IChangeDependencyRiskStatusHandler>();
        messageBox = Substitute.For<IMessageBox>();

        testSubject = new DependencyRisksReportViewModel(dependencyRisksStore, showDependencyRiskInBrowserHandler, changeDependencyRiskStatusHandler, messageBox);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DependencyRisksReportViewModel, IDependencyRisksReportViewModel>(
            MefTestHelpers.CreateExport<IDependencyRisksStore>(),
            MefTestHelpers.CreateExport<IShowDependencyRiskInBrowserHandler>(),
            MefTestHelpers.CreateExport<IChangeDependencyRiskStatusHandler>(),
            MefTestHelpers.CreateExport<IMessageBox>()
        );

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<DependencyRisksReportViewModel>();

    [TestMethod]
    public void Constructor_SubscribesToIssuesChanged() => dependencyRisksStore.Received().DependencyRisksChanged += Arg.Any<EventHandler>();

    [TestMethod]
    public void Dispose_UnsubscribesFromIssuesChanged()
    {
        testSubject.Dispose();

        dependencyRisksStore.Received().DependencyRisksChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void ShowDependencyRiskInBrowser_CallsHandler()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);

        testSubject.ShowDependencyRiskInBrowser(dependencyRisk);

        showDependencyRiskInBrowserHandler.Received(1).ShowInBrowser(riskId);
    }

    [TestMethod]
    public async Task ChangeDependencyRiskStatusAsync_CallsHandler_Success()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);
        var transition = DependencyRiskTransition.Accept;
        var comment = "test comment";
        changeDependencyRiskStatusHandler.ChangeStatusAsync(riskId, transition, comment).Returns(true);

        await testSubject.ChangeDependencyRiskStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.Received(1).ChangeStatusAsync(riskId, transition, comment);
        messageBox.DidNotReceiveWithAnyArgs().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task ChangeDependencyRiskStatusAsync_CallsHandler_Failure_ShowsMessageBox()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);
        const DependencyRiskTransition transition = DependencyRiskTransition.Accept;
        const string comment = "test comment";
        changeDependencyRiskStatusHandler.ChangeStatusAsync(riskId, transition, comment).Returns(false);

        await testSubject.ChangeDependencyRiskStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.Received(1).ChangeStatusAsync(riskId, transition, comment);
        messageBox.Received(1).Show(Resources.DependencyRiskStatusChangeFailedTitle, Resources.DependencyRiskStatusChangeError, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task ChangeDependencyRiskStatusAsync_NullTransition_DoesNotCallHandler_ShowsMessageBox()
    {
        var dependencyRisk = CreateDependencyRisk();
        DependencyRiskTransition? transition = null;
        const string comment = "test comment";

        await testSubject.ChangeDependencyRiskStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.DidNotReceiveWithAnyArgs().ChangeStatusAsync(Arg.Any<Guid>(), Arg.Any<DependencyRiskTransition>(), Arg.Any<string>());
        messageBox.Received(1).Show(Resources.DependencyRiskStatusChangeFailedTitle, Resources.DependencyRiskNullTransitionError, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    public void GetDependencyRisksGroup_ReturnsGroup_WhenNotFixedRisksExist()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk(isFixed: true);
        var risk3 = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([risk1, risk2, risk3]);

        var group = testSubject.GetDependencyRisksGroup();

        group.Should().NotBeNull();
        group.AllIssues.Should().HaveCount(2);
        group.AllIssues.Should().AllBeOfType<DependencyRiskViewModel>();
        group.AllIssues.Cast<DependencyRiskViewModel>().Select(vm => vm.DependencyRisk).Should().Contain([risk1, risk3]);
    }

    [TestMethod]
    public void GetDependencyRisksGroup_ReturnsNull_WhenAllRisksAreFixed()
    {
        var fixedRisk = CreateDependencyRisk(isFixed: true);
        var fixedRisk2 = CreateDependencyRisk(isFixed: true);
        dependencyRisksStore.GetAll().Returns([fixedRisk, fixedRisk2]);

        var group = testSubject.GetDependencyRisksGroup();

        group.Should().BeNull();
    }

    [TestMethod]
    public void GetDependencyRisksGroup_ReturnsNull_WhenNoRisks()
    {
        dependencyRisksStore.GetAll().Returns([]);

        var group = testSubject.GetDependencyRisksGroup();

        group.Should().BeNull();
    }

    [TestMethod]
    public void DependencyRisksChanged_RaisedOnStoreIssuesChanged()
    {
        var raised = false;
        testSubject.DependencyRisksChanged += (_, _) => raised = true;

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>(null, null);

        raised.Should().BeTrue();
    }

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, bool isFixed = false)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Transitions.Returns([]);
        risk.Status.Returns(isFixed ? DependencyRiskStatus.Fixed : DependencyRiskStatus.Open);
        return risk;
    }
}
