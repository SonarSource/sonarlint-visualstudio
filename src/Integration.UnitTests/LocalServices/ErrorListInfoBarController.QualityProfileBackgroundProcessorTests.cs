//-----------------------------------------------------------------------
// <copyright file="ErrorListInfoBarController.QualityProfileBackgroundProcessorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Threading;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ErrorListInfoBarController_QualityProfileBackgroundProcessorTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableVsProjectSystemHelper projectSystem;
        private ConfigurableVsGeneralOutputWindowPane outputWindow;
        private ConfigurableSolutionBindingSerializer bindingSerializer;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            this.outputWindow = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindow);

            this.bindingSerializer = new ConfigurableSolutionBindingSerializer();
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBindingSerializer), this.bindingSerializer);
        }

        #region Tests
        [TestMethod]
        public void QualityProfileBackgroundProcessor_ArgChecks()
        {
            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => 
                new ErrorListInfoBarController.QualityProfileBackgroundProcessor(null));
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_LifeCycle()
        {
            // Setup
            var testSubject = this.GetTestSubject();

            // Verify
            Assert.IsNotNull(testSubject.TokenSource);
            Assert.AreNotEqual(CancellationToken.None, testSubject.TokenSource.Token);

            // Act
            testSubject.Dispose();

            // Verify
            Exceptions.Expect<ObjectDisposedException>(() => testSubject.TokenSource.Cancel());
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_ArgChecks()
        {
            // Setup
            var testSubject = this.GetTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.QueueCheckIfUpdateIsRequired(null));
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoFilteredProjects()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.projectSystem.Projects = new Project[] { new ProjectMock("project.proj") };
            this.projectSystem.FilteredProjects = null;

            // Act
            testSubject.QueueCheckIfUpdateIsRequired(this.AssertIfCalled);

            // Verify
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoSolutionBinding()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();

            // Act
            testSubject.QueueCheckIfUpdateIsRequired(this.AssertIfCalled);

            // Verify
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoProfiles_RequiresUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();
            this.bindingSerializer.CurrentBinding = new Persistence.BoundSonarQubeProject();
            int called = 0;

            // Act
            testSubject.QueueCheckIfUpdateIsRequired((customMessage) =>
            {
                Assert.AreEqual(Strings.SonarLintInfoBarOldBindingFile, customMessage);
                called++;
            });

            // Verify
            Assert.AreEqual(1, called, "Expected the update action to be called");
            this.outputWindow.AssertOutputStrings(Strings.SonarLintProfileCheckNoProfiles);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_DifferentTimestamp_RequiresUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new System.Collections.Generic.Dictionary<LanguageGroup, ApplicableQualityProfile>()
            };
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.CSharp.ToString(), // Same profile key
                ProfileTimestamp = DateTime.Now
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, DateTime.Now.AddMinutes(-1), LanguageGroup.CSharp);

            // Act + Verify
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckProfileUpdated);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_NoTimestampDifferentProfile_RequiresUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new System.Collections.Generic.Dictionary<LanguageGroup, ApplicableQualityProfile>()
            };
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.CSharp.ToString() + "Old", // Different profile key
                ProfileTimestamp = null
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, null, LanguageGroup.CSharp);

            // Act + Verify
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckDifferentProfile);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_SameTimestampDifferentProfile_RequiresUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new System.Collections.Generic.Dictionary<LanguageGroup, ApplicableQualityProfile>()
            };
            DateTime sameTimestamp = DateTime.Now;
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.CSharp.ToString() + "Old", // Different profile key
                ProfileTimestamp = sameTimestamp
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, sameTimestamp, LanguageGroup.CSharp);

            // Act + Verify
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckDifferentProfile);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_SolutionRequiresMoreProfiles_RequiresUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(secondProjectLangauge: LanguageGroup.VB);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new System.Collections.Generic.Dictionary<LanguageGroup, ApplicableQualityProfile>()
            };
            // Has only a profile for C#
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.CSharp.ToString(),
                ProfileTimestamp = null
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, null, LanguageGroup.CSharp, LanguageGroup.VB);

            // Act + Verify
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckSolutionRequiresMoreProfiles);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_HasNotNeededProfile_DoesNotRequireUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new System.Collections.Generic.Dictionary<LanguageGroup, ApplicableQualityProfile>()
            };
            DateTime sameDate = DateTime.Now;
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.CSharp.ToString(), // Same as profile
                ProfileTimestamp = sameDate
            };
            // This profile should not be picked up in practice, no should cause an update to occur
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.VB] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.VB.ToString(), 
                ProfileTimestamp = null
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, sameDate, LanguageGroup.CSharp);

            // Act + Verify
            VerifyBackgroundExecution(false, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckQualityProfileIsUpToDate);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_ServiceErrors_DoesNotRequireUpdate()
        {
            // Setup
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(firstProjectLangauge: LanguageGroup.VB, secondProjectLangauge: LanguageGroup.VB);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new System.Collections.Generic.Dictionary<LanguageGroup, ApplicableQualityProfile>()
            };
            this.bindingSerializer.CurrentBinding.Profiles[LanguageGroup.VB] = new ApplicableQualityProfile
            {
                ProfileKey = LanguageGroup.VB.ToString(),
                ProfileTimestamp = DateTime.Now
            };
            this.ConfigureSonarQubeServiceWrapperWithServiceError();

            // Act + Verify
            VerifyBackgroundExecution(false, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckFailed);
        }
        #endregion

        #region Helpers

        private void VerifyBackgroundExecution(bool updateRequired, ErrorListInfoBarController.QualityProfileBackgroundProcessor testSubject, params string[] expectedOutput)
        {
            // Act
            int called = 0;
            testSubject.QueueCheckIfUpdateIsRequired((customMessage) =>
            {
                Assert.IsNull(customMessage, "Not expecting any message customizations");
                called++;
            });

            // Verify
            Assert.AreEqual(0, called, "Not expected to be immediate");
            Assert.IsNotNull(testSubject.BackgroundTask, "Expected to start processing in the background");

            // Run the background task
            Assert.IsTrue(testSubject.BackgroundTask.Wait(TimeSpan.FromSeconds(2)), "Timeout waiting for the background task");
            Assert.AreEqual(0, called, "The UI thread (this one) should be blocked");

            // Run the UI async action
            DispatcherHelper.DispatchFrame(DispatcherPriority.Normal); // Allow the BeginInvoke to run

            if (updateRequired)
            {
                Assert.AreEqual(1, called, "Expected to call the update action");
            }
            else
            {
                Assert.AreEqual(0, called, "Not expected to call the update action");
            }

            this.outputWindow.AssertOutputStrings(expectedOutput);
        }

        private ErrorListInfoBarController.QualityProfileBackgroundProcessor GetTestSubject()
        {
            return new ErrorListInfoBarController.QualityProfileBackgroundProcessor(this.host);
        }

        private void ConfigureSonarQubeServiceWrapperWithServiceError()
        {
            var sqService = new ConfigurableSonarQubeServiceWrapper();
            this.host.SonarQubeService = sqService;
            sqService.AllowConnections = false;
        }

        private void ConfigureValidSonarQubeServiceWrapper(BoundSonarQubeProject binding, DateTime? timestamp, params LanguageGroup[] expectedLanguageProfiles)
        {
            var sqService = new ConfigurableSonarQubeServiceWrapper();
            this.host.SonarQubeService = sqService;

            sqService.AllowConnections = true;
            sqService.ExpectedConnection = binding.CreateConnectionInformation();
            sqService.ExpectedProjectKey = binding.ProjectKey;

            foreach (LanguageGroup group in expectedLanguageProfiles)
            {
                sqService.ReturnProfile[LanguageGroupHelper.GetLanguage(group).ServerKey] = new Integration.Service.QualityProfile
                {
                    Key = group.ToString(),
                    Language = group.ToString(),
                    QualityProfileTimestamp = timestamp
                };
            }
        }

        private void AssertIfCalled(string customMessage)
        {
            Assert.Fail("Not expected to be called");
        }


        private void SetFilteredProjects(LanguageGroup firstProjectLangauge = LanguageGroup.CSharp, LanguageGroup secondProjectLangauge = LanguageGroup.CSharp)
        {
            var project1 = new ProjectMock("validProject1.csproj");
            SetProjectLanguage(project1, firstProjectLangauge);
            var project2 = new ProjectMock("validProject2.csproj");
            SetProjectLanguage(project2, secondProjectLangauge);
            this.projectSystem.FilteredProjects = new[] { project1, project2 };
        }

        private static void SetProjectLanguage(ProjectMock project, LanguageGroup lang)
        {
            switch (lang)
            {
                case LanguageGroup.CSharp:
                    project.SetCSProjectKind();
                    break;
                case LanguageGroup.VB:
                    project.SetVBProjectKind();
                    break;
                default:
                    break;
            }
        }
        #endregion

    }
}
