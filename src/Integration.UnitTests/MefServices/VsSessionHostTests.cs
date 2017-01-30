/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Windows.Threading;
using Xunit;
using SonarLint.VisualStudio.Integration.UnitTests.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    public class VsSessionHostTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableStateManager stateManager;
        private ConfigurableProgressStepRunner stepRunner;
        private ConfigurableSolutionBindingSerializer solutionBinding;

        public VsSessionHostTests()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.stepRunner = new ConfigurableProgressStepRunner();
            this.solutionBinding = new ConfigurableSolutionBindingSerializer();

            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);

            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            var propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests
        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new VsSessionHost(null, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider),
                new ConfigurableActiveSolutionTracker());

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithNullSQServiceWrapper_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new VsSessionHost(this.serviceProvider, null, new ConfigurableActiveSolutionTracker());

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }


        [Fact]
        public void VsSessionHost_ArgChecksCtor_WithNullActiveSolutionTracker_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new VsSessionHost(this.serviceProvider, new SonarQubeServiceWrapper(this.serviceProvider), null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SetActiveSection_WithNullSectionController_ThrowsArgumentNullException()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);

            // Act
            Action act = () => testSubject.SetActiveSection(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void VsSessionHost_SetActiveSection()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);

            // Case 2: Valid args
            var section1 = ConfigurableSectionController.CreateDefault();
            var section2 = ConfigurableSectionController.CreateDefault();
            bool refresh1Called = false;
            section1.RefreshCommand = new RelayCommand(() => refresh1Called = true);
            bool refresh2Called = false;
            section2.RefreshCommand = new RelayCommand(() => refresh2Called = true);

            // Act (set section1)
            testSubject.SetActiveSection(section1);
            refresh1Called.Should().BeFalse("Refresh should only be called when bound project was found");
            refresh2Called.Should().BeFalse("Refresh should only be called when bound project was found");

            // Assert
            testSubject.ActiveSection.Should().BeSameAs(section1);

            // Act (set section2)
            testSubject.ClearActiveSection();
            testSubject.SetActiveSection(section2);

            // Assert
            testSubject.ActiveSection.Should().BeSameAs(section2);
            refresh1Called.Should().BeFalse("Refresh should only be called when bound project was found");
            refresh2Called.Should().BeFalse("Refresh should only be called when bound project was found");
        }

        [Fact]
        public void VsSessionHost_SetActiveSection_TransferState()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();

            // Act
            testSubject.SetActiveSection(section);

            // Assert
            testSubject.ActiveSection.ViewModel.State
                .Should().BeSameAs(stateManager.ManagedState);
        }

        [Fact]
        public void VsSessionHost_SetActiveSection_ChangeHost()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();

            // Sanity
            this.stepRunner.AssertNoCurrentHost();

            // Act
            testSubject.SetActiveSection(section);

            // Assert
            this.stepRunner.AssertCurrentHost(section.ProgressHost);
        }

        [Fact]
        public void VsSessionHost_ClearActiveSection_ClearState()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Act
            testSubject.ClearActiveSection();

            // Assert
            testSubject.ActiveSection.Should().BeNull();
            section.ViewModel.State.Should().BeNull();
        }

        [Fact]
        public void VsSessionHost_ActiveSectionChangedEvent()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            ISectionController otherSection = ConfigurableSectionController.CreateDefault();
            int changed = 0;
            testSubject.ActiveSectionChanged += (o, e) => changed++;

            // Act (1st set)
            testSubject.SetActiveSection(section);

            // Assert
            changed.Should().Be(1, "ActiveSectionChanged event was expected to fire");

            // Act (clear)
            testSubject.ClearActiveSection();

            // Assert
            changed.Should().Be(2, "ActiveSectionChanged event was expected to fire");

            // Act (2nd set)
            testSubject.SetActiveSection(otherSection);

            // Assert
            changed.Should().Be(3, "ActiveSectionChanged event was expected to fire");

            // Act (clear)
            testSubject.ClearActiveSection();

            // Assert
            changed.Should().Be(4, "ActiveSectionChanged event was expected to fire");

            // Act (clear again)
            testSubject.ClearActiveSection();

            // Assert
            changed.Should().Be(4, "ActiveSectionChanged event was not expected to fire, since already cleared");
        }

        [Fact]
        public void VsSessionHost_SyncCommandFromActiveSectionDuringActiveSectionChanges()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            int syncCalled = 0;
            this.stateManager.SyncCommandFromActiveSectionAction = () => syncCalled++;

            // Case 1: SetActiveSection
            this.stateManager.ExpectActiveSection = true;

            // Act
            testSubject.SetActiveSection(section);

            // Assert
            syncCalled.Should().Be(1, "SyncCommandFromActiveSection wasn't called during section activation");

            // Case 2: ClearActiveSection section
            this.stateManager.ExpectActiveSection = false;

            // Act
            testSubject.ClearActiveSection();

            // Assert
            syncCalled.Should().Be(2, "SyncCommandFromActiveSection wasn't called during section deactivation");
        }

        [Fact]
        public void VsSessionHost_ResetBinding_NoOpenSolutionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            this.CreateTestSubject(tracker);
            // Previous binding information that should be cleared once there's no solution
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.stateManager.BoundProjectKey = boundProject.Key;
            this.stateManager.SetBoundProject(boundProject);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Act
            tracker.SimulateActiveSolutionChanged();

            // Assert
            this.stepRunner.AssertAbortAllCalled(1);
            this.stateManager.AssertNoBoundProject();
            this.stateManager.BoundProjectKey.Should().BeNull( "Expecting the key to be reset to null");
        }

        [Fact]
        public void VsSessionHost_ResetBinding_BoundSolutionWithNoActiveSectionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            this.stateManager.SetBoundProject(boundProject);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged();

            // Assert that nothing has changed (should defer all the work to when the section is connected)
            this.stepRunner.AssertAbortAllCalled(1);
            this.stateManager.AssertBoundProject(boundProject);
            this.stateManager.BoundProjectKey.Should().BeNull( "The key should only be set when there's active section to allow marking it once fetched all the projects");

            // Act (set active section)
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Assert (section has refreshed, no further aborts were required)
            this.stepRunner.AssertAbortAllCalled(1);
            boundProject.Key.Should().Be( this.stateManager.BoundProjectKey, "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.AssertBoundProject(boundProject);
            refreshCalled.Should().BeTrue( "Expected the refresh command to be called");
        }

        [Fact]
        public void VsSessionHost_ResetBinding_BoundSolutionWithActiveSectionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.stateManager.SetBoundProject(boundProject);
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged();

            // Assert
            this.stepRunner.AssertAbortAllCalled(1);
            boundProject.Key.Should().Be( this.stateManager.BoundProjectKey, "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.AssertBoundProject(boundProject);
            refreshCalled.Should().BeTrue( "Expected the refresh command to be called");
        }

        [Fact]
        public void VsSessionHost_ResetBinding_ErrorInReadingSolutionBinding()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.stateManager.SetBoundProject(boundProject);
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            var section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Introduce an error
            this.solutionBinding.ReadSolutionBindingAction = () => { throw new Exception("boom"); };

            // Act (i.e. simulate loading a different solution)
            using (new AssertIgnoreScope()) // Ignore exception assert
            {
                tracker.SimulateActiveSolutionChanged();
            }

            // Assert
            this.stateManager.AssertNoBoundProject();
        }


        [Fact]
        public void VsSessionHost_IServiceProvider_GetService()
        {
            // Arrange
            var testSubject = new VsSessionHost(this.serviceProvider, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider), new ConfigurableActiveSolutionTracker());
            ConfigurableVsShell shell = new ConfigurableVsShell();
            shell.RegisterPropertyGetter((int)__VSSPROPID2.VSSPROPID_InstallRootDir, () => TestHelper.GetDeploymentDirectory());
            this.serviceProvider.RegisterService(typeof(SVsShell), shell);

            // Local services
            // Act + Assert
            foreach (Type serviceType in VsSessionHost.SupportedLocalServices)
            {
                testSubject.GetService(serviceType).Should().NotBeNull();
            }

            testSubject.GetService<ISourceControlledFileSystem>()
                .Should().BeSameAs(testSubject.GetService<IFileSystem>());

            // VS-services
            // Sanity
            testSubject.GetService(typeof(VsSessionHostTests)).Should().BeNull( "Not expecting any service at this point");

            // Arrange
            this.serviceProvider.RegisterService(typeof(VsSessionHostTests), this);

            // Act + Assert
            testSubject.GetService(typeof(VsSessionHostTests))
                .Should().BeSameAs(this, "Unexpected service was returned, expected to use the service provider");
        }
        #endregion

        #region Helpers
        private VsSessionHost CreateTestSubject(ConfigurableActiveSolutionTracker tracker)
        {
            this.stateManager = new ConfigurableStateManager();
            var host = new VsSessionHost(this.serviceProvider,
                stateManager,
                this.stepRunner,
                this.sonarQubeService,
                tracker?? new ConfigurableActiveSolutionTracker(),
                Dispatcher.CurrentDispatcher);

            this.stateManager.Host = host;

            host.ReplaceInternalServiceForTesting<ISolutionBindingSerializer>(this.solutionBinding);

            return host;
        }

        #endregion
    }
}
