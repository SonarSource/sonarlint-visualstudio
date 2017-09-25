/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ErrorListInfoBarController_QualityProfileBackgroundProcessorTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableVsProjectSystemHelper projectSystem;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableSolutionBindingSerializer bindingSerializer;

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

            this.bindingSerializer = new ConfigurableSolutionBindingSerializer();
            this.serviceProvider.RegisterService(typeof(ISolutionBindingSerializer), this.bindingSerializer);
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
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoFilteredProjects()
        {
            // Arrange
            var testSubject = this.GetTestSubject();
            this.projectSystem.Projects = new Project[] { new ProjectMock("project.proj") };
            this.projectSystem.FilteredProjects = null;

            // Act
            testSubject.QueueCheckIfUpdateIsRequired(this.AssertIfCalled);

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void QualityProfileBackgroundProcessor_QueueCheckIfUpdateIsRequired_NoSolutionBinding()
        {
            // Arrange
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects();

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
            this.SetFilteredProjects(Language.CSharp, Language.CSharp);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject();
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
        public void QualityProfileBackgroundProcessor_BackgroundTask_DifferentTimestamp_RequiresUpdate()
        {
            // Arrange
            string qpKey = "Profile1";
            var testSubject = this.GetTestSubject();
            this.SetFilteredProjects(Language.CSharp, Language.CSharp);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };

            // Same profile key
            this.bindingSerializer.CurrentBinding.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = DateTime.Now
            };

            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding,
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
            this.SetFilteredProjects(Language.CSharp, Language.CSharp);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            this.bindingSerializer.CurrentBinding.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = "Profile2", // Different profile key
                ProfileTimestamp = null
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, DateTime.Now, qpKey, Language.CSharp);

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
            this.SetFilteredProjects(Language.CSharp, Language.CSharp);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            DateTime sameTimestamp = DateTime.Now;
            this.bindingSerializer.CurrentBinding.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = "csOld", // Different profile key
                ProfileTimestamp = sameTimestamp
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, sameTimestamp, qpKey, Language.CSharp);

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
            this.SetFilteredProjects(Language.CSharp, Language.VBNET);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            // Has only a profile for C#
            this.bindingSerializer.CurrentBinding.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = null
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, DateTime.Now, qpKey, Language.CSharp, Language.VBNET);

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
            this.SetFilteredProjects(Language.CSharp, Language.CSharp);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            DateTime sameDate = DateTime.Now;
            this.bindingSerializer.CurrentBinding.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey, // Same as profile
                ProfileTimestamp = sameDate
            };
            // This profile should not be picked up in practice, no should cause an update to occur
            this.bindingSerializer.CurrentBinding.Profiles[Language.VBNET] = new ApplicableQualityProfile
            {
                ProfileKey = qpKey,
                ProfileTimestamp = null
            };
            this.ConfigureValidSonarQubeServiceWrapper(this.bindingSerializer.CurrentBinding, sameDate, qpKey, Language.CSharp);

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
            this.SetFilteredProjects(Language.VBNET, Language.VBNET);
            this.bindingSerializer.CurrentBinding = new BoundSonarQubeProject
            {
                ServerUri = new Uri("http://server"),
                ProjectKey = "ProjectKey",
                Profiles = new Dictionary<Language, ApplicableQualityProfile>()
            };
            this.bindingSerializer.CurrentBinding.Profiles[Language.VBNET] = new ApplicableQualityProfile
            {
                ProfileKey = "vbnet",
                ProfileTimestamp = DateTime.Now
            };
            var service = new Mock<ISonarQubeService>();
            service.Setup(x => x.GetQualityProfileAsync(this.bindingSerializer.CurrentBinding.ProjectKey, ServerLanguage.VbNet, It.IsAny<CancellationToken>()))
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
                    .Setup(x => x.GetQualityProfileAsync(binding.ProjectKey, language.ToServerLanguage(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QualityProfile(qualityProfileKey, "", language.ToServerLanguage().Key, false, timestamp));
            }
        }

        private void AssertIfCalled(string customMessage)
        {
            FluentAssertions.Execution.Execute.Assertion.FailWith("Not expected to be called");
        }

        private void SetFilteredProjects(params Language[] languages)
        {
           this.projectSystem.FilteredProjects = languages.Select((language, i) =>
           {
               var project = new ProjectMock($"validProject{i}.csproj");
               project.SetProjectKind(language.ProjectType);
               return project;
           });
        }

        #endregion Helpers
    }
}