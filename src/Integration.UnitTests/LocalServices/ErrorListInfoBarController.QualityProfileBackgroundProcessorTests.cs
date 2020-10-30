/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;
using SonarQube.Client;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ErrorListInfoBarController_QualityProfileBackgroundProcessorTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableVsProjectSystemHelper projectSystem;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableConfigurationProvider configProvider;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.configProvider = new ConfigurableConfigurationProvider {FolderPathToReturn = "c:\\test"};
            this.serviceProvider.RegisterService(typeof(IConfigurationProviderService), this.configProvider);
        }

        #region Tests

        [TestMethod]
        public void QualityProfileBackgroundProcessor_ArgChecks()
        {
            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() =>
                new ErrorListInfoBarController.QualityProfileBackgroundProcessor(null));
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_LifeCycle()
        {
            // Arrange
            var testSubject = this.GetTestSubject();

            // Assert
            testSubject.TokenSource.Should().NotBeNull();
            testSubject.TokenSource.Token.Should().NotBe(CancellationToken.None);

            // Act
            testSubject.Dispose();

            // Assert
            Exceptions.Expect<ObjectDisposedException>(() => testSubject.TokenSource.Cancel());
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_ArgChecks()
        {
            // Arrange
            var testSubject = this.GetTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.QueueCheckIfUpdateIsRequired(null));
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoFilteredProjects_NoOp()
        {
            // Arrange
            var testSubject = this.GetTestSubject();
            SetBinding(new BoundSonarQubeProject(), SonarLintMode.LegacyConnected);
            this.SetFilteredProjects(); // no filtered projects

            // Act
            testSubject.QueueCheckIfUpdateIsRequired(this.AssertIfCalled);

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_StandaloneMode_NoOp()
        {
            // Arrange
            var testSubject = this.GetTestSubject();
            SetBinding(null, SonarLintMode.Standalone);
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind);

            // Act
            testSubject.QueueCheckIfUpdateIsRequired(this.AssertIfCalled);

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoProfiles_RequiresUpdate()
        {
            // Arrange
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.CSharpProjectKind);
            SetBinding(new BoundSonarQubeProject(), SonarLintMode.LegacyConnected);
            int called = 0;

            // Act
            testSubject.QueueCheckIfUpdateIsRequired((customMessage) =>
            {
                customMessage.Should().Be(Strings.SonarLintInfoBarOldBindingFile);
                called++;
            });

            // Assert
            called.Should().Be(1, "Expected the update action to be called");
            this.outputWindowPane.AssertOutputStrings(Strings.SonarLintProfileCheckNoProfiles);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_DifferentTimestamp_RequiresUpdate_Connected()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.CSharpProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            this.configProvider.ModeToReturn = SonarLintMode.Connected;

            // Same profile key
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = DateTime.Now
            };

            this.ConfigureValidSonarQubeServiceWrapper(this.configProvider.ProjectToReturn,
                DateTime.Now.AddMinutes(-1),
                qpKey,
                Language.CSharp);

            // Act + Assert
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckProfileUpdated);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_DifferentTimestamp_RequiresUpdate_Legacy()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.CSharpProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;

            // Same profile key
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = DateTime.Now
            };

            this.ConfigureValidSonarQubeServiceWrapper(this.configProvider.ProjectToReturn,
                DateTime.Now.AddMinutes(-1),
                qpKey,
                Language.CSharp);

            // Act + Assert
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckProfileUpdated);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_NoTimestampDifferentProfile_RequiresUpdate()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.CSharpProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = "Profile2", // Different profile key
                ProfileTimestamp = null
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            this.ConfigureValidSonarQubeServiceWrapper(this.configProvider.ProjectToReturn, DateTime.Now, qpKey, Language.CSharp);

            // Act + Assert
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckDifferentProfile);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_SameTimestampDifferentProfile_RequiresUpdate()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.CSharpProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            DateTime sameTimestamp = DateTime.Now;
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = "csOld", // Different profile key
                ProfileTimestamp = sameTimestamp
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            this.ConfigureValidSonarQubeServiceWrapper(this.configProvider.ProjectToReturn, sameTimestamp, qpKey, Language.CSharp);

            // Act + Assert
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckDifferentProfile);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_SolutionRequiresMoreProfiles_RequiresUpdate()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.VbProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            // Has only a profile for C#
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = null
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            this.ConfigureValidSonarQubeServiceWrapper(this.configProvider.ProjectToReturn, DateTime.Now, qpKey, Language.CSharp, Language.VBNET);

            // Act + Assert
            VerifyBackgroundExecution(true, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckSolutionRequiresMoreProfiles);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_HasNotNeededProfile_DoesNotRequireUpdate()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.CSharpProjectKind, ProjectSystemHelper.CSharpProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            DateTime sameDate = DateTime.Now;
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey, // Same as profile
                ProfileTimestamp = sameDate
            };
            // This profile should not be picked up in practice, no should cause an update to occur
            this.configProvider.ProjectToReturn.Profiles[Language.VBNET] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = null
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            this.ConfigureValidSonarQubeServiceWrapper(this.configProvider.ProjectToReturn, sameDate, qpKey, Language.CSharp);

            // Act + Assert
            VerifyBackgroundExecution(false, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckQualityProfileIsUpToDate);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_BackgroundTask_ServiceErrors_DoesNotRequireUpdate()
        {
            // Arrange
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(ProjectSystemHelper.VbProjectKind, ProjectSystemHelper.VbProjectKind);
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            this.configProvider.ProjectToReturn.Profiles[Language.VBNET] = new ApplicableQualityProfile
            {
                ProfileKey = "vbnet",
                ProfileTimestamp = DateTime.Now
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            var service = new Mock<ISonarQubeService>();
            service.Setup(x => x.GetQualityProfileAsync(this.configProvider.ProjectToReturn.ProjectKey, null, SonarQubeLanguage.VbNet, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => { throw new Exception(); });
            this.host.SonarQubeService = service.Object;

            // Act + Assert
            VerifyBackgroundExecution(false, testSubject,
                Strings.SonarLintProfileCheck,
                Strings.SonarLintProfileCheckFailed);
        }

        #endregion Tests

        #region Helpers

        private void VerifyBackgroundExecution(bool updateRequired, ErrorListInfoBarController.QualityProfileBackgroundProcessor testSubject, params string[] expectedOutput)
        {
            // Act
            int called = 0;
            testSubject.QueueCheckIfUpdateIsRequired((customMessage) =>
            {
                customMessage.Should().BeNull("Not expecting any message customizations");
                called++;
            });

            // Assert
            called.Should().Be(0, "Not expected to be immediate");
            testSubject.BackgroundTask.Should().NotBeNull("Expected to start processing in the background");

            // Run the background task
            testSubject.BackgroundTask.Wait(TimeSpan.FromSeconds(60)).Should().BeTrue("Timeout waiting for the background task");
            called.Should().Be(0, "The UI thread (this one) should be blocked");

            // Run the UI async action
            DispatcherHelper.DispatchFrame(DispatcherPriority.Normal); // Allow the BeginInvoke to run

            if (updateRequired)
            {
                called.Should().Be(1, "Expected to call the update action");
            }
            else
            {
                called.Should().Be(0, "Not expected to call the update action");
            }

            this.outputWindowPane.AssertOutputStrings(expectedOutput);
        }

        private ErrorListInfoBarController.QualityProfileBackgroundProcessor GetTestSubject()
        {
            return new ErrorListInfoBarController.QualityProfileBackgroundProcessor(this.host);
        }

        private void ConfigureValidSonarQubeServiceWrapper(BoundSonarQubeProject binding, DateTime timestamp,
            string qualityProfileKey, params Language[] expectedLanguageProfiles)
        {
            var sqService = new Mock<ISonarQubeService>();

            this.host.SonarQubeService = sqService.Object;

            foreach (Language language in expectedLanguageProfiles)
            {
                sqService
                    .Setup(x => x.GetQualityProfileAsync(binding.ProjectKey, null, language.ServerLanguage, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new SonarQubeQualityProfile(qualityProfileKey, "", language.ServerLanguage.Key, false, timestamp));
            }
        }

        private void AssertIfCalled(string customMessage)
        {
            FluentAssertions.Execution.Execute.Assertion.FailWith("Not expected to be called");
        }

        private void SetFilteredProjects(params string[] projectKinds)
        {
           this.projectSystem.FilteredProjects = projectKinds.Select((projectKind, i) =>
           {
               var project = new ProjectMock($"validProject{i}.csproj");
               project.SetProjectKind(new Guid(projectKind));
               return project;
           });
        }

        private void SetBinding(BoundSonarQubeProject boundProject, SonarLintMode mode)
        {
            this.configProvider.ProjectToReturn = boundProject;
            this.configProvider.ModeToReturn = mode;
        }

        #endregion Helpers
    }
}
