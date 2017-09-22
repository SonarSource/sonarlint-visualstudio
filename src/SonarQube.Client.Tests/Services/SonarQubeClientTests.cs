/*
 * SonarQube Client
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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services.Tests
{
    [TestClass]
    public class SonarQubeClientTests
    {
        #region Ctor checks
        [TestMethod]
        public void Ctor_WithNullMessageHandler_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(null, TimeSpan.MaxValue);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageHandler");
        }

        [TestMethod]
        public void Ctor_WithZeroTimeout_ThrowsArgumentException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(new HttpClientHandler(), TimeSpan.Zero);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .WithMessage("Doesn't expect a zero or negative timeout.\r\nParameter name: requestTimeout")
                .And.ParamName.Should().Be("requestTimeout");
        }

        [TestMethod]
        public void Ctor_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(new HttpClientHandler(), TimeSpan.MinValue);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .WithMessage("Doesn't expect a zero or negative timeout.\r\nParameter name: requestTimeout")
                .And.ParamName.Should().Be("requestTimeout");
        }
        #endregion

        #region Check called URLs
        [TestMethod]
        public async Task GetComponentsSearchProjectsAsync_CallsTheExpectedUri()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            await Method_CallsTheExpectedUri(
                new Uri("api/components/search_projects?organization=org&p=42&ps=25&asc=true", UriKind.RelativeOrAbsolute),
                @"{""components"":[]}", (c, co, t) => c.GetComponentsSearchProjectsAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetOrganizationsAsync_CallsTheExpectedUri()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            await Method_CallsTheExpectedUri(new Uri("api/organizations/search?p=42&ps=25", UriKind.RelativeOrAbsolute),
                @"{""organizations"":[]}", (c, co, t) => c.GetOrganizationsAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetPluginsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/updatecenter/installed_plugins", UriKind.RelativeOrAbsolute),
                "", (c, co, t) => c.GetPluginsAsync(co, t));
        }
        [TestMethod]
        public async Task GetProjectsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/projects/index", UriKind.RelativeOrAbsolute),
                "", (c, co, t) => c.GetProjectsAsync(co, t));
        }
        [TestMethod]
        public async Task GetPropertiesAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/properties/", UriKind.RelativeOrAbsolute),
                "", (c, co, t) => c.GetPropertiesAsync(co, t));
        }
        [TestMethod]
        public async Task GetQualityProfileChangeLogAsync_CallsTheExpectedUri()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/changelog?profileKey=qp&ps=25", UriKind.RelativeOrAbsolute),
                "", (c, co, t) => c.GetQualityProfileChangeLogAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetQualityProfilesAsync_CallsTheExpectedUri()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/search?projectKey=project", UriKind.RelativeOrAbsolute),
                @"{""profiles"":[]}", (c, co, t) => c.GetQualityProfilesAsync(co, request, t));

            request = new QualityProfileRequest { ProjectKey = null };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/search?defaults=true", UriKind.RelativeOrAbsolute),
                @"{""profiles"":[]}", (c, co, t) => c.GetQualityProfilesAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetRoslynExportProfileAsync_CallsTheExpectedUri()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.CSharp };
            await Method_CallsTheExpectedUri(
                new Uri("api/qualityprofiles/export?name=qp&language=cs&exporterKey=roslyn-cs", UriKind.RelativeOrAbsolute),
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
</RoslynExportProfile>", (c, co, t) => c.GetRoslynExportProfileAsync(co, request, t));

            request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.VbNet };
            await Method_CallsTheExpectedUri(
                new Uri("api/qualityprofiles/export?name=qp&language=vbnet&exporterKey=roslyn-vbnet", UriKind.RelativeOrAbsolute),
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
</RoslynExportProfile>", (c, co, t) => c.GetRoslynExportProfileAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetVersionAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/server/version", UriKind.RelativeOrAbsolute),
                "", (c, co, t) => c.GetVersionAsync(co, t));
        }
        [TestMethod]
        public async Task ValidateCredentialsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/authentication/validate", UriKind.RelativeOrAbsolute),
                @"{""valid"": true}", (c, co, t) => c.ValidateCredentialsAsync(co, t));
        }

        private async Task Method_CallsTheExpectedUri<T>(Uri expectedRelativeUri, string resultContent,
            Func<SonarQubeClient, ConnectionDTO, CancellationToken, Task<Result<T>>> call)
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            var client = new SonarQubeClient(httpHandler.Object, TimeSpan.FromSeconds(10));
            var serverUri = new Uri("http://mysq.com/");
            var connection = new ConnectionDTO { ServerUri = serverUri };

            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(resultContent)
                }))
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    request.Method.Should().Be(HttpMethod.Get);
                    request.RequestUri.Should().Be(new Uri(serverUri, expectedRelativeUri));
                });

            // Act
            await call(client, connection, CancellationToken.None);
        }
        #endregion

        #region Check successful requests
        [TestMethod]
        public async Task GetComponentsSearchProjectsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetComponentsSearchProjectsAsync(co, request, t),
                @"{""components"":[{""organization"":""my - org - key - 1"",""id"":""AU - Tpxb--iU5OvuD2FLy"",""key"":""my_project"",""name"":""My Project 1"",""isFavorite"":true,""tags"":[""finance"",""java""],""visibility"":""public""},{""organization"":""my-org-key-1"",""id"":""AU-TpxcA-iU5OvuD2FLz"",""key"":""another_project"",""name"":""My Project 2"",""isFavorite"":false,""tags"":[],""visibility"":""public""}]}",
                result => result.Length.Should().Be(2));
        }
        [TestMethod]
        public async Task GetOrganizationsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetOrganizationsAsync(co, request, t),
                @"{""organizations"":[{""key"":""foo - company"",""name"":""Foo Company""},{""key"":""bar - company"",""name"":""Bar Company""}]}",
                result => result.Length.Should().Be(2));
        }
        [TestMethod]
        public async Task GetPluginsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetPluginsAsync(co, t),
                @"[{""key"":""findbugs"",""name"":""Findbugs"",""version"":""2.1""},{""key"":""l10nfr"",""name"":""French Pack"",""version"":""1.10""},{""key"":""jira"",""name"":""JIRA"",""version"":""1.2""}]",
                result => result.Length.Should().Be(3));
        }
        [TestMethod]
        public async Task GetProjectsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetProjectsAsync(co, t),
                @"[{""id"":""5035"",""k"":""org.jenkins-ci.plugins:sonar"",""nm"":""Jenkins Sonar Plugin"",""sc"":""PRJ"",""qu"":""TRK""},{""id"":""5146"",""k"":""org.codehaus.sonar-plugins:sonar-ant-task"",""nm"":""Sonar Ant Task"",""sc"":""PRJ"",""qu"":""TRK""},{""id"":""15964"",""k"":""org.codehaus.sonar-plugins:sonar-build-breaker-plugin"",""nm"":""Sonar Build Breaker Plugin"",""sc"":""PRJ"",""qu"":""TRK""}]",
                result => result.Length.Should().Be(3));
        }
        [TestMethod]
        public async Task GetPropertiesAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetPropertiesAsync(co, t),
                @"[{""key"":""sonar.demo.1.text"",""value"":""foo""},{""key"":""sonar.demo.1.boolean"",""value"":""true""},{""key"":""sonar.demo.2.text"",""value"":""bar""}]",
                result => result.Length.Should().Be(3));
        }
        [TestMethod]
        public async Task GetQualityProfileChangeLogAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetQualityProfileChangeLogAsync(co, request, t),
                @"{""events"":[{""date"":""2015-02-23T17:58:39+0100"",""action"":""ACTIVATED"",""authorLogin"":""anakin.skywalker"",""authorName"":""Anakin Skywalker"",""ruleKey"":""squid:S2438"",""ruleName"":""\""Threads\"" should not be used where \""Runnables\"" are expected"",""params"":{""severity"":""CRITICAL""}}]}",
                result => result.Events.Length.Should().Be(1));
        }
        [TestMethod]
        public async Task GetQualityProfilesAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetQualityProfilesAsync(co, request, t),
                @"{""profiles"":[{""key"":""AU-TpxcA-iU5OvuD2FL3"",""name"":""Sonar way"",""language"":""cs"",""languageName"":""C#"",""isInherited"":false,""activeRuleCount"":37,""activeDeprecatedRuleCount"":0,""isDefault"":true,""ruleUpdatedAt"":""2016-12-22T19:10:03+0100"",""lastUsed"":""2016-12-01T19:10:03+0100""}]}",
                result => result.Length.Should().Be(1));
        }
        [TestMethod]
        public async Task GetRoslynExportProfileAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.CSharp };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetRoslynExportProfileAsync(co, request, t),
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
  <Configuration>
    <RuleSet Name=""Rules for SonarQube"" Description=""This rule set was automatically generated from SonarQube."" ToolsVersion=""14.0"">
      <Rules AnalyzerId=""SonarAnalyzer.CSharp"" RuleNamespace=""SonarAnalyzer.CSharp"">
        <Rule Id=""S121"" Action=""Warning"" />
      </Rules>
    </RuleSet>
    <AdditionalFiles>
      <AdditionalFile FileName=""SonarLint.xml"" />
    </AdditionalFiles>
  </Configuration>
  <Deployment>
    <Plugins>
      <Plugin Key=""csharp"" Version=""6.4.0.3322"" StaticResourceName=""SonarAnalyzer-6.4.0.3322.zip"" />
    </Plugins>
    <NuGetPackages>
      <NuGetPackage Id=""SonarAnalyzer.CSharp"" Version=""6.4.0.3322"" />
    </NuGetPackages>
  </Deployment>
</RoslynExportProfile>",
                result => { });
        }
        [TestMethod]
        public async Task GetVersionAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.GetVersionAsync(co, t),
                "6.3.0.1234",
                result => result.Version.Should().Be("6.3.0.1234"));
        }
        [TestMethod]
        public async Task ValidateCredentialsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, co, t) => c.ValidateCredentialsAsync(co, t),
                "{\"valid\": true}",
                value => value.AreValid.Should().BeTrue());
        }

        private async Task Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData<T>(
            Func<SonarQubeClient, ConnectionDTO, CancellationToken, Task<Result<T>>> call, string resultContent,
            Action<T> extraAssertions)
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(resultContent)
                }));
            var client = new SonarQubeClient(httpHandler.Object, TimeSpan.FromSeconds(10));
            var connection = new ConnectionDTO { ServerUri = new Uri("http://mysq.com/") };

            // Act & Assert
            var result = await call(client, connection, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            extraAssertions(result.Value);
        }
        #endregion

        #region Check cancellation
        [TestMethod]
        public void GetComponentsSearchProjectsAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetComponentsSearchProjectsAsync(co, request, t));
        }
        [TestMethod]
        public void GetOrganizationsAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetOrganizationsAsync(co, request, t));
        }
        [TestMethod]
        public void GetPluginsAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetPluginsAsync(co, t));
        }
        [TestMethod]
        public void GetProjectsAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetProjectsAsync(co, t));
        }
        [TestMethod]
        public void GetPropertiesAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetPropertiesAsync(co, t));
        }
        [TestMethod]
        public void GetQualityProfileChangeLogAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetQualityProfileChangeLogAsync(co, request, t));
        }
        [TestMethod]
        public void GetQualityProfilesAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetQualityProfilesAsync(co, request, t));
        }
        [TestMethod]
        public void GetRoslynExportProfileAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.CSharp };
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetRoslynExportProfileAsync(co, request, t));
        }
        [TestMethod]
        public void GetVersionAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.GetVersionAsync(co, t));
        }
        [TestMethod]
        public void ValidateCredentialsAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, co, t) => c.ValidateCredentialsAsync(co, t));
        }

        private void Method_WhenCancellationRequested_ThrowsException<T>(
            Func<SonarQubeClient, ConnectionDTO, CancellationToken, Task<Result<T>>> call)
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("")
                }));
            var client = new SonarQubeClient(httpHandler.Object, TimeSpan.FromSeconds(10));
            var connection = new ConnectionDTO { ServerUri = new Uri("http://mysq.com/") };
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act & Assert
            Func<Task<Result<T>>> funct = async () => await call(client, connection, cancellationToken.Token);

            // Assert
            funct.ShouldThrow<OperationCanceledException>();
        }
        #endregion

        #region Check thrown exception is propagated
        [TestMethod]
        public void GetComponentsSearchProjectsAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetComponentsSearchProjectsAsync(co, request, t));
        }
        [TestMethod]
        public void GetOrganizationsAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetOrganizationsAsync(co, request, t));
        }
        [TestMethod]
        public void GetPluginsAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetPluginsAsync(co, t));
        }
        [TestMethod]
        public void GetProjectsAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetProjectsAsync(co, t));
        }
        [TestMethod]
        public void GetPropertiesAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetPropertiesAsync(co, t));
        }
        [TestMethod]
        public void GetQualityProfileChangeLogAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetQualityProfileChangeLogAsync(co, request, t));
        }
        [TestMethod]
        public void GetQualityProfilesAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetQualityProfilesAsync(co, request, t));
        }
        [TestMethod]
        public void GetRoslynExportProfileAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.CSharp };
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetRoslynExportProfileAsync(co, request, t));
        }
        [TestMethod]
        public void GetVersionAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.GetVersionAsync(co, t));
        }
        [TestMethod]
        public void ValidateCredentialsAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, co, t) => c.ValidateCredentialsAsync(co, t));
        }

        private void Method_WhenExceptionThrown_PropagateIt<T>(
            Func<SonarQubeClient, ConnectionDTO, CancellationToken, Task<Result<T>>> call)
        {
            // Arrange
            var expectedException = new Exception("foo text.");

            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => { throw expectedException; });
            var client = new SonarQubeClient(httpHandler.Object, TimeSpan.FromSeconds(10));
            var connection = new ConnectionDTO { ServerUri = new Uri("http://mysq.com/") };

            // Act & Assert
            Func<Task<Result<T>>> funct = async () => await call(client, connection, CancellationToken.None);

            // Assert
            funct.ShouldThrow<Exception>().And.Message.Should().Be(expectedException.Message);
        }
        #endregion
    }
}
