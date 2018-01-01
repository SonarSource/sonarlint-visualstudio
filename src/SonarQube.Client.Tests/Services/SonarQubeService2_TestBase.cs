using System;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SonarQube.Client.Api.Requests;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarQube.Client.Tests.Services
{
    public class SonarQubeService2_TestBase
    {
        protected Mock<HttpMessageHandler> messageHandler;
        protected SonarQubeService2 service;
        private RequestFactory requestFactory;

        private static readonly Uri BasePath = new Uri("http://localhost");

        [TestInitialize]
        public void TestInitialize()
        {
            messageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            requestFactory = new RequestFactory();
            DefaultConfiguration.Configure(requestFactory);

            service = new SonarQubeService2(messageHandler.Object, requestFactory);
        }

        protected async Task ConnectToSonarQube(string version = "5.6.0.0")
        {
            SetupRequest("api/server/version", version);
            SetupRequest("api/authentication/validate", "{ \"valid\": true}");

            await service.ConnectAsync(
                new ConnectionInformation(BasePath, "valeri", new SecureString()),
                CancellationToken.None);
        }

        protected void SetupRequest(string relativePath, string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(BasePath, relativePath)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(response)
                }));
        }
    }
}
