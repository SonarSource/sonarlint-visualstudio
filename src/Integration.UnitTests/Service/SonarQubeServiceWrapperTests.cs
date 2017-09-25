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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarQubeServiceWrapperTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(
                    MefTestHelpers.CreateExport<ITelemetryLogger>(new ConfigurableTelemetryLogger())));
        }

        #region Tests

        [TestMethod]
        public void SonarQubeServiceWrapper_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SonarQubeServiceWrapper(null));
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects()
        {
            var p1 = new ProjectInformation
            {
                Name = "Project 1",
                Key = "1"
            };
            var p2 = new ProjectInformation
            {
                Name = "Project 2",
                Key = "2"
            };

            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange case 1: first time connect
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler { ResponseText = Serialize(new[] { p1 }) });
                var connectionInfo1 = new ConnectionInformation(new Uri("http://server"));

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo1, CancellationToken.None, out projects).Should().BeTrue("Expected to get the projects");

                // Assert
                AssertEqualProjects(new[] { p1 }, projects);
                this.outputWindowPane.AssertOutputStrings(0);

                // Arrange case 2: second time connect
                var connectionInfo2 = new ConnectionInformation(new Uri("http://server"));

                // Act
                testSubject.RegisterConnectionHandler(new RequestHandler { ResponseText = Serialize(new[] { p1, p2 }) });
                testSubject.TryGetProjects(connectionInfo2, CancellationToken.None, out projects).Should().BeTrue("Expected to get the projects");

                // Assert
                AssertEqualProjects(new[] { p1, p2 }, projects);
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_Organization()
        {
            var p1 = new ComponentResult
            {
                Name = "Project 1",
                Key = "1"
            };
            var p2 = new ComponentResult
            {
                Name = "Project 2",
                Key = "2"
            };

            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                testSubject.RegisterQueryValidator("api/components/search_projects", request =>
                {
                    request.QueryString.HasValue.Should().BeTrue();
                    request.QueryString.Value.Should().Be("asc=true&organization=myorg&ps=500&p=1");
                });

                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.RegisterRequestHandler("api/components/search_projects?asc=true&organization=myorg&ps=500&p=1",
                    new RequestHandler { ResponseText = Serialize(new { paging = new { total = 0 }, components = new[] { p1 } }) });
                var connectionInfo1 = new ConnectionInformation(new Uri("http://server"))
                {
                    Organization = new OrganizationInformation { Key = "myorg", Name = "My org" }
                };

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo1, CancellationToken.None, out projects)
                    .Should().BeTrue("Expected to get the projects");

                // Assert
                projects.Should().Equal(new[] { p1 }, (x, y) => x.Key == y.Key && x.Name == y.Name);
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetNotifications()
        {
            var expected = new NotificationEvent[]
            {
                new NotificationEvent { Category = "QUALITY_GATE",
                    Message =  "Quality Gate of project 'test' is now Red (was Green)",
                    Link = new Uri("http://localhost:9000/dashboard?id=test"),
                    Date = new DateTimeOffset(2017, 1, 1, 9, 55, 1, 0, TimeSpan.FromHours(1)),
                    Project = "test" }
            };

            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                testSubject.RegisterQueryValidator("api/developers/search_events", request =>
                {
                    request.QueryString.HasValue.Should().BeTrue();
                    request.QueryString.Value.Should().Be("projects=test&from=2017-09-14T10%3a00%3a00%2b0200");
                });

                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.RegisterRequestHandler("api/developers/search_events?projects=test&from=2017-01-01T07%3a55%3a01%2b0200",
                    new RequestHandler { ResponseText = Serialize(new { events = expected }) });

                var connectionInfo = new ConnectionInformation(new Uri("http://server"));
                var projectKey = "test";
                var dateLastCheck = new DateTimeOffset(2017, 1, 1, 7, 55, 1, 0, TimeSpan.FromHours(2));

                // Act
                NotificationEvent[] events;

                var isSuccess = testSubject.TryGetNotificationEvents(connectionInfo, CancellationToken.None, projectKey, dateLastCheck,
                    out events);

                // Assert
                isSuccess.Should().BeTrue("Expected to get the notifications");
                AssertEqual(expected, events);
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_InvalidStatusCode()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler { ResponseStatusCode = HttpStatusCode.InternalServerError });
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeFalse();

                // Assert
                projects.Should().BeNull();
                this.outputWindowPane.AssertOutputStrings(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_Timeout()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 1))
            {
                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.DelayRequestInMilliseconds = 1000;
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeFalse();

                // Assert
                this.outputWindowPane.AssertOutputStrings(1);
                projects.Should().BeNull();
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_Cancellation()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 100))
            {
                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.DelayRequestInMilliseconds = 1000;
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));
                using (var tokenSource = new CancellationTokenSource())
                {
                    tokenSource.CancelAfter(20);

                    // Act
                    ProjectInformation[] projects = null;
                    testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeFalse();

                    // Assert
                    this.outputWindowPane.AssertOutputStrings(1);
                    projects.Should().BeNull();
                }
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_Authentication_Anonymous()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler { ResponseText = Serialize(new ProjectInformation[0]) });
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeTrue("Should be get an empty array of projects");

                // Assert
                this.outputWindowPane.AssertOutputStrings(0);
                projects.Should().NotBeNull("Expected projects");
                projects.Should().BeEmpty("Expected an empty array");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_Authentication_Basic_Valid()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                testSubject.BasicAuthUsers.Add("admin", "admin");
                testSubject.RegisterConnectionHandler(new RequestHandler { ResponseText = Serialize(new ProjectInformation[0]) });
                var connectionInfo = new ConnectionInformation(new Uri("http://server"), "admin", "admin".ToSecureString());

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeTrue("Should be get an empty array of projects");

                // Assert
                this.outputWindowPane.AssertOutputStrings(0);
                projects.Should().NotBeNull("Expected projects");
                projects.Should().BeEmpty("Expected an empty array");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_Authentication_Basic_Invalid()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange case 1: Invalid password
                testSubject.BasicAuthUsers.Add("admin", "admin1");
                var connectionInfo = new ConnectionInformation(new Uri("http://server"), "admin", "admin".ToSecureString());

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeFalse();

                // Assert
                this.outputWindowPane.AssertOutputStrings(1);
                projects.Should().BeNull("Not expecting projects");

                // Arrange case 2: Invalid user name
                testSubject.BasicAuthUsers.Clear();
                testSubject.BasicAuthUsers.Add("admin1", "admin");

                // Act
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeFalse();

                // Assert
                this.outputWindowPane.AssertOutputStrings(2);
                projects.Should().BeNull("Not expecting projects");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_ArgChecks()
        {
            // Arrange
            var testSubject = new SonarQubeServiceWrapper(this.serviceProvider);
            ProjectInformation[] projects;

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetProjects(null, CancellationToken.None, out projects));
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProjects_UnhandledExceptions()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler(ctx => { throw new InvalidOperationException(); }));
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                ProjectInformation[] projects = null;
                testSubject.TryGetProjects(connectionInfo, CancellationToken.None, out projects).Should().BeFalse();

                // Assert
                this.outputWindowPane.AssertOutputStrings(1);
                projects.Should().BeNull("Not expecting projects");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProperties()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                ConnectionInformation conn = ConfigureValidConnection(testSubject, new ProjectInformation[0]);

                // Arrange
                var property1 = new ServerProperty { Key = "prop1", Value = "val1" };
                var property2 = new ServerProperty { Key = "prop2", Value = "val2" };

                var expectedProperties = new[] { property1, property2 };

                // Arrange test server
                RequestHandler handler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.PropertiesAPI,
                    ctx => ServiceServerProperties(ctx, expectedProperties)
                );

                // Act
                ServerProperty[] actualProperties;
                testSubject.TryGetProperties(conn, CancellationToken.None, out actualProperties).Should().BeTrue("TryGetProperties failed unexpectedly");

                // Assert
                CollectionAssert.AreEqual(expectedProperties.Select(x => x.Key).ToArray(), actualProperties.Select(x => x.Key).ToArray(), "Unexpected server property keys");
                CollectionAssert.AreEqual(expectedProperties.Select(x => x.Value).ToArray(), actualProperties.Select(x => x.Value).ToArray(), "Unexpected server property values");
                handler.HandlerCalledCount.Should().Be(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetProperties_ArgChecks()
        {
            // Arrange
            var testSubject = new SonarQubeServiceWrapper(this.serviceProvider);
            ServerProperty[] properties;

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetProperties(null, CancellationToken.None, out properties));
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetQualityProfile_ArgChecks()
        {
            // Arrange
            var testSubject = new SonarQubeServiceWrapper(this.serviceProvider);
            QualityProfile profile;
            var validConnection = new ConnectionInformation(new Uri("http://valid"));
            var validProject = new ProjectInformation();
            var validLanguage = Language.CSharp;
            var cppLanguage = new Language("Cpp", "C++", Guid.NewGuid());

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetQualityProfile(null, validProject, validLanguage, CancellationToken.None, out profile));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetQualityProfile(validConnection, null, validLanguage, CancellationToken.None, out profile));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetQualityProfile(validConnection, validProject, null, CancellationToken.None, out profile));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.TryGetQualityProfile(validConnection, validProject, cppLanguage, CancellationToken.None, out profile));

            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetQualityProfile_FullFunctionality()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                Language language = Language.CSharp;
                QualityProfile profile = CreateRandomQualityProfile(language);
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                var changeLog = new QualityProfileChangeLog
                {
                    Page = 1,
                    PageSize = 1,
                    Total = 1,
                    Events = new QualityProfileChangeLogEvent[]
                    {
                        new QualityProfileChangeLogEvent { Date = DateTime.Now }
                    }
                };
                ConnectionInformation conn = ConfigureValidConnection(testSubject, new[] { project });

                // Arrange test server
                RegisterQualityProfileChangeLogValidator(testSubject);

                RequestHandler getProfileHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceQualityProfiles(ctx, new[] { profile })
                );
                RequestHandler changeLogHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileChangeLogUrl(profile),
                    ctx => ServiceChangeLog(ctx, changeLog)
                );

                // Act
                QualityProfile actualProfile;
                testSubject.TryGetQualityProfile(conn, project, language, CancellationToken.None, out actualProfile).Should().BeTrue("TryGetExportProfile failed unexpectedly");

                // Assert
                actualProfile.Should().NotBeNull("Expected a profile to be returned");
                actualProfile.Key.Should().Be(profile.Key);
                actualProfile.Name.Should().Be(profile.Name);
                actualProfile.QualityProfileTimestamp.Should().Be(changeLog.Events[0].Date);

                getProfileHandler.HandlerCalledCount.Should().Be(1);
                changeLogHandler.HandlerCalledCount.Should().Be(1);
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetQualityProfileForCSharp_ReducedFunctionality()
        {
            SonarQubeServiceWrapper_TryGetQualityProfile_ReducedFunctionality(Language.CSharp);
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetQualityProfileForVBNet_ReducedFunctionality()
        {
            SonarQubeServiceWrapper_TryGetQualityProfile_ReducedFunctionality(Language.VBNET);
        }

        private void SonarQubeServiceWrapper_TryGetQualityProfile_ReducedFunctionality(Language language)
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                QualityProfile profile = CreateRandomQualityProfile(language);
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                ConnectionInformation conn = ConfigureValidConnection(testSubject, new[] { project });

                // Arrange test server
                RegisterQualityProfileChangeLogValidator(testSubject);

                RequestHandler getProfileHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceQualityProfiles(ctx, new[] { profile })
                );
                RequestHandler changeLogHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileChangeLogUrl(profile),
                    ctx => ServiceChangeLog(ctx, null, simulateFault: true)
                );

                // Act
                QualityProfile actualProfile;
                testSubject.TryGetQualityProfile(conn, project, language, CancellationToken.None, out actualProfile).Should().BeTrue("TryGetExportProfile failed unexpectedly");

                // Assert
                actualProfile.Should().NotBeNull("Expected a profile to be returned");
                actualProfile.Key.Should().Be(profile.Key);
                actualProfile.Name.Should().Be(profile.Name);
                actualProfile.QualityProfileTimestamp.Should().BeNull();

                getProfileHandler.HandlerCalledCount.Should().Be(1);
                changeLogHandler.HandlerCalledCount.Should().Be(1);
                this.outputWindowPane.AssertOutputStrings(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetExportProfileForCSharp()
        {
            SonarQubeServiceWrapper_TryGetExportProfile(Language.CSharp);
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetExportProfileForVBNet()
        {
            SonarQubeServiceWrapper_TryGetExportProfile(Language.VBNET);
        }

        private void SonarQubeServiceWrapper_TryGetExportProfile(Language language)
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                QualityProfile profile = CreateRandomQualityProfile(language);
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                var expectedExport = RoslynExportProfileHelper.CreateExport(ruleSet: TestRuleSetHelper.CreateTestRuleSet(3));
                var roslynExporter = SonarQubeServiceWrapper.CreateRoslynExporterName(language);
                ConnectionInformation conn = ConfigureValidConnection(testSubject, new[] { project });

                // Arrange test server
                RegisterProfileExportQueryValidator(testSubject);

                RequestHandler getExportHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileExportUrl(profile, language, roslynExporter),
                    ctx => ServiceProfileExport(ctx, expectedExport)
                );

                // Act
                RoslynExportProfile actualExport;
                testSubject.TryGetExportProfile(conn, profile, language, CancellationToken.None, out actualExport).Should().BeTrue("TryGetExportProfile failed unexpectedly");

                // Assert
                actualExport.Should().NotBeNull("Expected a profile export to be returned");
                RoslynExportProfileHelper.AssertAreEqual(expectedExport, actualExport);
                getExportHandler.HandlerCalledCount.Should().Be(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetExportProfile_ArgChecks()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 1))
            {
                RoslynExportProfile actualExport;
                ConnectionInformation connection = new ConnectionInformation(new Uri("http://valid"));
                QualityProfile profile = new QualityProfile();

                // No connection information
                Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetExportProfile(null, profile, Language.CSharp, CancellationToken.None, out actualExport));

                // No project information
                Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetExportProfile(connection, null, Language.CSharp, CancellationToken.None, out actualExport));

                // Invalid language
                var pascalLanguage = new Language("Pascal", "Pascal", Guid.Empty);
                Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.TryGetExportProfile(connection, profile, pascalLanguage, CancellationToken.None, out actualExport));

                // Those are API usage issue which we don't report to the output pane
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public async Task SonarQubeServiceWrapper_DownloadQualityProfile_ProjectWithoutAnalysis_MultipleLanguageProfiles_ReturnsDefault()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                HttpClient httpClient = testSubject.CreateHttpClient();
                var language = Language.CSharp;
                QualityProfile expectedProfile = CreateRandomQualityProfile(language, isDefault: true);
                QualityProfile unexpectedProfile = CreateRandomQualityProfile(language);

                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                ConfigureValidConnection(testSubject, new[] { project });

                // Arrange test server
                RegisterQualityProfileQueryValidator(testSubject);

                RequestHandler forProjectHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceHttpNotFound(ctx)
                );

                RequestHandler forLanguageHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language),
                    ctx => ServiceQualityProfiles(ctx, new[] { expectedProfile, unexpectedProfile })
                );

                // Act
                QualityProfile actualProfile = await SonarQubeServiceWrapper.DownloadQualityProfile(httpClient, project, language, CancellationToken.None);

                // Assert
                actualProfile.Should().NotBeNull("Expected a quality profile");
                actualProfile.Key.Should().Be(expectedProfile.Key, "Unexpected quality profile returned");
                forProjectHandler.HandlerCalledCount.Should().Be(1);
                forLanguageHandler.HandlerCalledCount.Should().Be(1);
            }
        }

        [TestMethod]
        public async Task SonarQubeServiceWrapper_DownloadQualityProfile_ProjectWithAnalysis()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                HttpClient httpClient = testSubject.CreateHttpClient();
                var language = Language.CSharp;
                QualityProfile expectedProfile = CreateRandomQualityProfile(language);
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                ConfigureValidConnection(testSubject, new[] { project });

                // Arrange test server
                RegisterQualityProfileQueryValidator(testSubject);

                RequestHandler handler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceQualityProfiles(ctx, new[] { expectedProfile })
                );

                // Act
                QualityProfile actualProfile = await SonarQubeServiceWrapper.DownloadQualityProfile(httpClient, project, language, CancellationToken.None);

                // Assert
                actualProfile.Should().NotBeNull("Expected a quality profile");
                actualProfile.Key.Should().Be(expectedProfile.Key, "Unexpected quality profile returned");
                handler.HandlerCalledCount.Should().Be(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetPlugins_ArgChecks()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 1))
            {
                ServerPlugin[] plugins;

                // Null connection information
                Exceptions.Expect<ArgumentNullException>(() => testSubject.TryGetPlugins(null, CancellationToken.None, out plugins));

                // Those are API usage issue which we don't report to the output pane
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_TryGetPlugins()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Arrange
                var connectionInfo = new ConnectionInformation(new Uri("http://servername"));
                var plugin1 = new ServerPlugin { Key = "plugin1" };
                var plugin2 = new ServerPlugin { Key = "plugin2" };

                var expectedPlugins = new[] { plugin1, plugin2 };

                // Arrange test server
                RequestHandler handler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.ServerPluginsInstalledAPI,
                    ctx => ServiceServerPlugins(ctx, expectedPlugins)
                );

                // Act
                ServerPlugin[] actualPlugins;
                testSubject.TryGetPlugins(connectionInfo, CancellationToken.None, out actualPlugins).Should().BeTrue("TryGetPlugins failed unexpectedly");

                // Assert
                CollectionAssert.AreEqual(expectedPlugins.Select(x => x.Key).ToArray(), actualPlugins.Select(x => x.Key).ToArray(), "Unexpected server plugins");
                handler.HandlerCalledCount.Should().Be(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_CreateProjectDashboardUrl()
        {
            // Arrange
            var testSubject = new SonarQubeServiceWrapper(this.serviceProvider);

            var serverUrl = new Uri("http://my-sonar-server:5555");
            var connectionInfo = new ConnectionInformation(serverUrl);
            var projectInfo = new ProjectInformation { Key = "p1" };

            Uri expectedUrl = new Uri("http://my-sonar-server:5555/dashboard/index/p1");

            // Act
            var actualUrl = testSubject.CreateProjectDashboardUrl(connectionInfo, projectInfo);

            // Assert
            actualUrl.Should().Be(expectedUrl, "Unexpected project dashboard URL");
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_CreateProjectDashboardUrl_ArgChecks()
        {
            // Arrange
            var testSubject = new SonarQubeServiceWrapper(this.serviceProvider);

            var connectionInfo = new ConnectionInformation(new Uri("http://my-sonar-server:5555"));
            var projectInfo = new ProjectInformation { Key = "p1" };

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.CreateProjectDashboardUrl(null, projectInfo));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.CreateProjectDashboardUrl(connectionInfo, null));
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_AppendQuery_NoQueryParameters_ReturnsBaseUrl()
        {
            // Act + Assert
            SonarQubeServiceWrapper.AppendQueryString("api/foobar", "").Should().Be("api/foobar");
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_AppendQuery_MultipleQueryParameters_ReturnsCorrectQueryString()
        {
            // Act
            var result = SonarQubeServiceWrapper.AppendQueryString(
                urlBase: "api/foobar",
                queryFormat: "?a={0}&b={2}&c={1}",
                args: new[] { "1", "3", "2" });

            // Assert
            result.Should().Be("api/foobar?a=1&b=2&c=3");
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_CreateRequestUrl_HostNameOnlyBaseAddress()
        {
            // Act
            var result = SonarQubeServiceWrapper.CreateRequestUrl(
                client: CreateClientWithAddress("http://hostname/"),
                apiUrl: "foo/bar/baz");

            // Assert
            result.ToString().Should().Be("http://hostname/foo/bar/baz", "Unexpected request URL for base address with host name only");
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_CreateRequestUrl_HostNameAndPathBaseAddress()
        {
            // Act
            var result = SonarQubeServiceWrapper.CreateRequestUrl(
                client: CreateClientWithAddress("http://hostname/and/path/"),
                apiUrl: "foo/bar/baz");

            // Assert
            result.ToString().Should().Be("http://hostname/and/path/foo/bar/baz", "Unexpected request URL for base address with host name and path");
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_CreateRequestUrl_LeadingAndTrailingSlashes()
        {
            using (new AssertIgnoreScope())
            {
                // Test case 1: base => no slash; api => with slash
                // Act
                var result1 = SonarQubeServiceWrapper.CreateRequestUrl(
                    client: CreateClientWithAddress("http://localhost/no/trailing/slash"),
                    apiUrl: "/has/starting/slash");

                // Assert
                result1.ToString().Should().Be("http://localhost/no/trailing/slash/has/starting/slash");

                // Test case 2: base => with slash; api => no slash
                // Act
                var result2 = SonarQubeServiceWrapper.CreateRequestUrl(
                    client: CreateClientWithAddress("http://localhost/with/trailing/slash/"),
                    apiUrl: "no/starting/slash");

                // Assert
                result2.ToString().Should().Be("http://localhost/with/trailing/slash/no/starting/slash");

                // Test case 3: base => no slash; api => no slash
                // Act
                var result3 = SonarQubeServiceWrapper.CreateRequestUrl(
                    client: CreateClientWithAddress("http://localhost/no/trailing/slash"),
                    apiUrl: "no/starting/slash");

                // Assert
                result3.ToString().Should().Be("http://localhost/no/trailing/slash/no/starting/slash");

                // Test case 3: base => with slash; api => with slash
                // Act
                var result4 = SonarQubeServiceWrapper.CreateRequestUrl(
                    client: CreateClientWithAddress("http://localhost/with/trailing/slash/"),
                    apiUrl: "/with/starting/slash");

                // Assert
                result4.ToString().Should().Be("http://localhost/with/trailing/slash/with/starting/slash");
            }
        }

        #endregion Tests

        #region Helpers

        private static QualityProfile CreateRandomQualityProfile(Language language, bool isDefault = false)
        {
            var languageKey = SonarQubeServiceWrapper.GetServerLanguageKey(language);
            return new QualityProfile { Key = Guid.NewGuid().ToString("N"), Language = languageKey, IsDefault = isDefault };
        }

        private static HttpClient CreateClientWithAddress(string baseAddress)
        {
            return new HttpClient { BaseAddress = new Uri(baseAddress) };
        }

        private static ConnectionInformation ConfigureValidConnection(TestableSonarQubeServiceWrapper testSubject, IEnumerable<ProjectInformation> projects)
        {
            var connectionInfo = new ConnectionInformation(new Uri("http://test-server"));

            testSubject.AllowAnonymous = true;
            testSubject.RegisterConnectionHandler(new RequestHandler { ResponseText = Serialize(projects) });

            return connectionInfo;
        }

        private static void RegisterQualityProfileQueryValidator(TestableSonarQubeServiceWrapper testSubject)
        {
            testSubject.RegisterQueryValidator(SonarQubeServiceWrapper.QualityProfileListAPI, request =>
            {
                var queryMap = ParseQuery(request.Uri.Query);
                if (queryMap.Count == 1)
                {
                    queryMap["language"].Should().NotBeNull("Missing query param: language");
                }
                else if (queryMap.Count == 2)
                {
                    queryMap["language"].Should().NotBeNull("Missing query param: language");
                    queryMap["project"].Should().NotBeNull("Missing query param: project");
                }
                else
                {
                    FluentAssertions.Execution.Execute.Assertion.FailWith("Unexpected query params.: {0}", string.Join(", ", queryMap.Keys));
                }
            });
        }

        private static void RegisterProfileExportQueryValidator(TestableSonarQubeServiceWrapper testSubject)
        {
            testSubject.RegisterQueryValidator(SonarQubeServiceWrapper.QualityProfileExportAPI, request =>
            {
                var queryMap = ParseQuery(request.Uri.Query);
                queryMap.Should().HaveCount(3, "Unexpected query params.: {0}", string.Join(", ", queryMap.Keys));
                queryMap["name"].Should().NotBeNull("Missing query param: name");
                queryMap["language"].Should().NotBeNull("Missing query param: language");
                queryMap["format"].Should().NotBeNull("Missing query param: format");
                queryMap["format"].Should().Be(SonarQubeServiceWrapper.RoslynExporterFormat, "Unexpected value for query param: format");
            });
        }

        private static void RegisterQualityProfileChangeLogValidator(TestableSonarQubeServiceWrapper testSubject)
        {
            testSubject.RegisterQueryValidator(SonarQubeServiceWrapper.QualityProfileChangeLogAPI, request =>
            {
                var queryMap = ParseQuery(request.Uri.Query);
                queryMap.Should().HaveCount(2, "Unexpected query params.: {0}", string.Join(", ", queryMap.Keys));
                queryMap["profileKey"].Should().NotBeNull("Missing query param: profileKey");
                queryMap["ps"].Should().Be("1", "Expecting always page size 1");
            });
        }

        private static void SimulateServerFault(IOwinContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        private static void ServiceHttpNotFound(IOwinContext context, bool simulateFault = false)
        {
            if (simulateFault)
            {
                SimulateServerFault(context);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }

        private static void ServiceQualityProfiles(IOwinContext context, IEnumerable<QualityProfile> profilesToReturn, bool simulateFault = false)
        {
            if (simulateFault)
            {
                SimulateServerFault(context);
            }
            else
            {
                context.Response.Write(Serialize(new QualityProfiles { Profiles = profilesToReturn.ToArray() }));
            }
        }

        private static void ServiceServerPlugins(IOwinContext context, IEnumerable<ServerPlugin> serverPlugins, bool simulateFault = false)
        {
            if (simulateFault)
            {
                SimulateServerFault(context);
            }
            else
            {
                context.Response.Write(Serialize(serverPlugins.ToArray()));
            }
        }

        private static void ServiceServerProperties(IOwinContext context, IEnumerable<ServerProperty> serverProperties, bool simulateFault = false)
        {
            if (simulateFault)
            {
                SimulateServerFault(context);
            }
            else
            {
                context.Response.Write(Serialize(serverProperties.ToArray()));
            }
        }

        private static void ServiceChangeLog(IOwinContext context, QualityProfileChangeLog changeLog, bool simulateFault = false)
        {
            if (simulateFault)
            {
                SimulateServerFault(context);
            }
            else
            {
                context.Response.Write(Serialize(changeLog));
            }
        }

        private static void ServiceProfileExport(IOwinContext context, RoslynExportProfile export, bool simulateFault = false)
        {
            if (simulateFault)
            {
                SimulateServerFault(context);
            }
            else
            {
                var serializer = new XmlSerializer(typeof(RoslynExportProfile));
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, export);
                    context.Response.Write(stream.ToArray());
                }
            }
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>();
            // Skip '?' and then split by &
            string[] keyValues = query.Substring(1).Split('&');
            foreach (string pair in keyValues)
            {
                string[] keyValue = pair.Split('=');
                keyValue.Should().HaveCount(2);
                HttpUtility.UrlDecode(HttpUtility.UrlEncode(HttpUtility.UrlDecode(keyValue[1]))).Should().Be(HttpUtility.UrlDecode(keyValue[1]), "{0} was supposed to be encoded", keyValue[0]);
                result[keyValue[0]] = HttpUtility.UrlDecode(keyValue[1]);
            }

            return result;
        }

        private static void AssertEqualProjects(ProjectInformation[] expected, ProjectInformation[] actual)
        {
            actual.Should().HaveCount(expected.Length, "Different array size");
            for (int i = 0; i < expected.Length; i++)
            {
                AssertProjectsEqualNotSame(expected[i], actual[i]);
            }
        }

        private static void AssertProjectsEqualNotSame(ProjectInformation expected, ProjectInformation actual)
        {
            actual.Should().NotBe(expected);
            actual.Key.Should().Be(expected.Key, "Unexpected Key");
            actual.Name.Should().Be(expected.Name, "Unexpected Name");
        }

        private static string Serialize<T>(T[] array)
        {
            return JsonHelper.Serialize(array);
        }

        private static string Serialize<T>(T item)
        {
            return JsonHelper.Serialize(item);
        }

        private class RequestHandler
        {
            internal int HandlerCalledCount { get; private set; }

            public RequestHandler()
            {
            }

            public RequestHandler(Action<IOwinContext> requestProcessor)
            {
                this.RequestProcessor = requestProcessor;
            }

            public string ResponseText { get; set; }

            public Action<IOwinContext> RequestProcessor { get; }

            public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

            public void HandlerRequest(IOwinContext context)
            {
                this.HandlerCalledCount++;
                context.Response.StatusCode = (int)this.ResponseStatusCode;
                if (this.ResponseText != null)
                {
                    context.Response.Write(this.ResponseText);
                }

                // Let the processor to set the final state, if exists.
                this.RequestProcessor?.Invoke(context);
            }
        }

        private void AssertEqual(NotificationEvent x, NotificationEvent y, int itemIndex)
        {
            Assert.AreEqual(x.Category, y.Category, string.Format("Category, item {0}", itemIndex));
            Assert.AreEqual(x.Date, y.Date, string.Format("Date, item {0}", itemIndex));
            Assert.AreEqual(x.Link, y.Link, string.Format("Link, item {0}", itemIndex));
            Assert.AreEqual(x.Message, y.Message, string.Format("Message, item {0}", itemIndex));
        }

        private void AssertEqual(NotificationEvent[] expected, NotificationEvent[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                AssertEqual(expected[i], actual[i], i);
            }
        }

        private class TestableSonarQubeServiceWrapper : SonarQubeServiceWrapper, IDisposable
        {
            private bool isDisposed;
            private TestServer server;
            private readonly Dictionary<string, RequestHandler> uriRequestHandler = new Dictionary<string, RequestHandler>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, Action<IOwinRequest>> uriRequestValidators = new Dictionary<string, Action<IOwinRequest>>(StringComparer.OrdinalIgnoreCase);

            public TestableSonarQubeServiceWrapper(IServiceProvider serviceProvider, int timeoutInMilliseconds = 1000)
                : base(serviceProvider, TimeSpan.FromMilliseconds(GetTimeout(timeoutInMilliseconds)))
            {
                this.server = TestServer.Create(app =>
                {
                    app.UseErrorPage();
                    app.Run(context =>
                    {
                        return this.HandleRequest(context);
                    });
                });
            }

            private static int GetTimeout(int defaultValue)
            {
                if (Debugger.IsAttached)
                {
                    return 5 * 60 * 1000; // 5 minutes
                }
                return defaultValue;
            }

            #region Testing hooks

            public int UnauthorizedStatusCode { get; set; } = (int)HttpStatusCode.Forbidden;

            public bool AllowAnonymous { get; set; }

            public Dictionary<string, string> BasicAuthUsers { get; } = new Dictionary<string, string>();

            public int DelayRequestInMilliseconds { get; set; } = 0;

            public Action<IOwinContext> RequestValidation
            {
                get;
                set;
            }

            public void RegisterRequestHandler(string path, RequestHandler handler)
            {
                this.uriRequestHandler[path] = handler;
            }

            public RequestHandler RegisterRequestHandler(string path, Action<IOwinContext> handler)
            {
                return this.uriRequestHandler[path] = new RequestHandler(handler);
            }

            public void RegisterQueryValidator(string path, Action<IOwinRequest> handler)
            {
                this.uriRequestValidators[path] = handler;
            }

            public void RegisterConnectionHandler(RequestHandler handler)
            {
                this.RegisterRequestHandler(SonarQubeServiceWrapper.ProjectsAPI, handler);
            }

            #endregion Testing hooks

            #region Server mock

            private async Task HandleRequest(IOwinContext context)
            {
                this.RequestValidation?.Invoke(context);

                if (this.DelayRequestInMilliseconds > 0)
                {
                    await Task.Delay(this.DelayRequestInMilliseconds);
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;

                if (!this.IsAuthorized(context.Request))
                {
                    context.Response.StatusCode = this.UnauthorizedStatusCode;
                }

                this.ValidateQueryParams(context);

                this.HandlePathRequest(context);
            }

            private void HandlePathRequest(IOwinContext context)
            {
                string path = context.Request.Uri.PathAndQuery.TrimStart('/');

                RequestHandler handler;
                if (this.uriRequestHandler.TryGetValue(path, out handler))
                {
                    try
                    {
                        handler.HandlerRequest(context);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString(), "Unexpected exception during HandlerRequest");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        throw;
                    }
                }
                else
                {
                    Debug.WriteLine("Handler not found", path);
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }

            private void ValidateQueryParams(IOwinContext context)
            {
                Action<IOwinRequest> validator;
                if (this.uriRequestValidators.TryGetValue(context.Request.Uri.LocalPath, out validator))
                {
                    try
                    {
                        validator?.Invoke(context.Request);
                    }
                    catch (AssertFailedException ex)
                    {
                        Debug.WriteLine(ex.ToString(), "AssertFailedException");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        throw;
                    }
                }
            }

            protected internal override HttpClient CreateHttpClient()
            {
                return this.server?.HttpClient;
            }

            private bool IsAuthorized(IOwinRequest request)
            {
                if (this.AllowAnonymous)
                {
                    return true;
                }

                // Basic
                const string AuthorizationHeader = "Authorization";
                string[] value;
                if (request.Headers.TryGetValue(AuthorizationHeader, out value)
                    && value.Length == 1
                    && IsBasicAuthAuthorized(value[0]))
                {
                    return true;
                }

                return false;
            }

            private bool IsBasicAuthAuthorized(string headerValue)
            {
                const string BasicAuth = "Basic";

                string[] keyValue = headerValue.Split((char[])null/*whitespace*/, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length != 2 || keyValue[0] != BasicAuth)
                {
                    return false;
                }

                return this.BasicAuthUsers
                    .Select(kv => AuthenticationHeaderProvider.GetBasicAuthToken(kv.Key, kv.Value.ToSecureString()))
                    .Any(token => token == keyValue[1]);
            }

            #endregion Server mock

            #region IDisposable Support

            protected void Dispose(bool disposing)
            {
                if (!this.isDisposed)
                {
                    if (disposing)
                    {
                        this.server?.Dispose();
                        this.server = null;
                    }

                    this.isDisposed = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                this.Dispose(true);
            }

            #endregion IDisposable Support
        }

        #endregion Helpers
    }
}