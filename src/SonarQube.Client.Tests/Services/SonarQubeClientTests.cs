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
            action.ShouldThrow<ArgumentException>().WithMessage("Doesn't expect a zero or negative timeout.\r\nParameter name: requestTimeout")
                .And.ParamName.Should().Be("requestTimeout");
        }

        [TestMethod]
        public void Ctor_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(new HttpClientHandler(), TimeSpan.MinValue);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Doesn't expect a zero or negative timeout.\r\nParameter name: requestTimeout")
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
                (c, co, t) => c.GetComponentsSearchProjectsAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetOrganizationsAsync_CallsTheExpectedUri()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            await Method_CallsTheExpectedUri(new Uri("api/organizations/search?p=42&ps=25", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetOrganizationsAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetPluginsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/updatecenter/installed_plugins", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetPluginsAsync(co, t));
        }
        [TestMethod]
        public async Task GetProjectsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/projects/index", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetProjectsAsync(co, t));
        }
        [TestMethod]
        public async Task GetPropertiesAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/properties/", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetPropertiesAsync(co, t));
        }
        [TestMethod]
        public async Task GetQualityProfileChangeLogAsync_CallsTheExpectedUri()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/changelog?profileKey=qp&ps=25", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetQualityProfileChangeLogAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetQualityProfilesAsync_CallsTheExpectedUri()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/search?projectKey=project", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetQualityProfilesAsync(co, request, t));

            request = new QualityProfileRequest { ProjectKey = null };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/search?defaults=true", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetQualityProfilesAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetRoslynExportProfileAsync_CallsTheExpectedUri()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.CSharp };
            await Method_CallsTheExpectedUri(
                new Uri("api/qualityprofiles/export?name=qp&language=cs&exporterKey=roslyn-cs", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetRoslynExportProfileAsync(co, request, t));

            request = new RoslynExportProfileRequest { QualityProfileName = "qp", Language = ServerLanguage.VbNet };
            await Method_CallsTheExpectedUri(
                new Uri("api/qualityprofiles/export?name=qp&language=vb&exporterKey=roslyn-vb", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetRoslynExportProfileAsync(co, request, t));
        }
        [TestMethod]
        public async Task GetVersionAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/server/version", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.GetVersionAsync(co, t));
        }
        [TestMethod]
        public async Task ValidateCredentialsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/authentication/validate", UriKind.RelativeOrAbsolute),
                (c, co, t) => c.ValidateCredentialsAsync(co, t));
        }

        private async Task Method_CallsTheExpectedUri<T>(Uri expectedRelativeUri,
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
                .Returns(Task<HttpResponseMessage>.Factory.StartNew(() =>
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
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
            extraAssertions(result.Value);
        }
        #endregion

        [TestMethod]
        public void ProcessJsonResponse_WhenTokenIsCanceled_ThrowsOperationCanceledException()
        {
            // Arrange
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            // Act
            Action action = () => SonarQubeClient.ProcessJsonResponse<string>("", cancellationSource.Token);

            // Assert
            action.ShouldThrow<OperationCanceledException>();
        }

        [TestMethod]
        public void GetStringResultAsync_WhenTokenIsCanceled_ThrowsOperationCanceledException()
        {
            // Arrange
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            // Act
            Func<Task> func = async () => await SonarQubeClient.GetStringResultAsync(new HttpResponseMessage(),
                cancellationSource.Token);

            // Assert
            func.ShouldThrow<OperationCanceledException>();
        }

        [TestMethod]
        public void GetStringResultAsync_ThrowsWhenNotSuccessStatusCode()
        {
            // Arrange
            var message = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            // Act
            Func<Task> func = async () => await SonarQubeClient.GetStringResultAsync(message, CancellationToken.None);

            // Assert
            func.ShouldThrow<HttpRequestException>();
        }

        [TestMethod]
        public async Task SafeUseHttpClient_WhenRequestReturnsNotNull_ReturnsSuccessResult()
        {
            // Arrange
            var client = new SonarQubeClient(new HttpClientHandler(), TimeSpan.FromSeconds(10));

            // Act
            var result = await client.SafeUseHttpClient(new ConnectionDTO(), c => Task.FromResult(new object()));

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.ErrorMessage.Should().BeNull();
            result.Exception.Should().BeNull();
            result.StatusCode.Should().BeNull();
        }

        [TestMethod]
        public async Task SafeUseHttpClient_WhenRequestReturnsNull_ReturnsFailingResult()
        {
            // Arrange
            var client = new SonarQubeClient(new HttpClientHandler(), TimeSpan.FromSeconds(10));

            // Act
            var result = await client.SafeUseHttpClient(new ConnectionDTO(), c => Task.FromResult<object>(null));

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Be("Null is not an expected valid result.");
            result.Exception.Should().BeNull();
            result.StatusCode.Should().BeNull();
            result.Value.Should().BeNull();
        }

        [TestMethod]
        public async Task SafeUseHttpClient_WhenRequestThrows_ReturnsFailingResult()
        {
            // Arrange
            var client = new SonarQubeClient(new HttpClientHandler(), TimeSpan.FromSeconds(10));
            var exception = new Exception("some message");

            // Act
            var result = await client.SafeUseHttpClient<object>(new ConnectionDTO(), c => { throw exception; });

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Exception.Should().Be(exception);
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.StatusCode.Should().BeNull();
        }
    }
}
