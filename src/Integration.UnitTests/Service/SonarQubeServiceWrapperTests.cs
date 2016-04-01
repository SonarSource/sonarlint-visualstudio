//-----------------------------------------------------------------------
// <copyright file="SonarQubeServiceWrapperTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarQubeServiceWrapperTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsGeneralOutputWindowPane outputWindowPane;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindowPane = new ConfigurableVsGeneralOutputWindowPane());
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(
                    MefTestHelpers.CreateExport<ITelemetryLogger>(new ConfigurableTelemetryLogger())));
        }

        #region Tests

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_Disconnect()
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
                // Setup case 1: first time connect
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler() { ResponseText = Serialize(new[] { p1 }) });
                var connectionInfo1 = new ConnectionInformation(new Uri("http://server"));

                // Act
                ProjectInformation[] projects = testSubject.Connect(connectionInfo1, CancellationToken.None).ToArray();

                // Verify
                AssertEqualProjects(new[] { p1 }, projects);
                this.outputWindowPane.AssertOutputStrings(0);
                Assert.AreSame(connectionInfo1, testSubject.CurrentConnection, "Expected to be connected");

                // Setup case 2: second time connect
                var connectionInfo2 = new ConnectionInformation(new Uri("http://server"));

                // Act
                testSubject.RegisterConnectionHandler(new RequestHandler() { ResponseText = Serialize(new[] { p1, p2 }) });
                projects = testSubject.Connect(connectionInfo2, CancellationToken.None).ToArray();

                // Verify
                AssertEqualProjects(new[] { p1, p2 }, projects);
                this.outputWindowPane.AssertOutputStrings(0);
                Assert.AreSame(connectionInfo2, testSubject.CurrentConnection, "Expected to be connected");
                Assert.IsTrue(connectionInfo1.IsDisposed, "The first connection is expected to be disposed");

                // Disconnect
                // Act
                testSubject.Disconnect();

                // Verify
                Assert.IsNull(testSubject.CurrentConnection, "Disconnected, not expecting any current connection");
                Assert.IsTrue(connectionInfo2.IsDisposed, "The second connection is expected to be disposed");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_InvalidStatusCode()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup 
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler() { ResponseStatusCode = HttpStatusCode.InternalServerError });
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(1);
                Assert.IsNull(testSubject.CurrentConnection, "Invalid request was made");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_Timeout()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 1))
            {
                // Setup 
                testSubject.AllowAnonymous = true;
                testSubject.DelayRequestInMilliseconds = 1000;
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(1);
                Assert.IsNull(testSubject.CurrentConnection, "Request timeout");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_Cancellation()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 100))
            {
                // Setup 
                testSubject.AllowAnonymous = true;
                testSubject.DelayRequestInMilliseconds = 1000;
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));
                using (var tokenSource = new CancellationTokenSource())
                {
                    tokenSource.CancelAfter(20);

                    // Act
                    testSubject.Connect(connectionInfo, tokenSource.Token);

                    // Verify
                    this.outputWindowPane.AssertOutputStrings(1);
                    Assert.IsNull(testSubject.CurrentConnection, "Request cancelled");
                }
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_Authentication_Anonymous()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler() { ResponseText = Serialize(new ProjectInformation[0]) });
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                var projects = testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                Assert.AreSame(connectionInfo, testSubject.CurrentConnection, "Expected to be connected");
                Assert.IsNotNull(projects, "Expected projects");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_Authentication_Basic_Valid()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                testSubject.BasicAuthUsers.Add("admin", "admin");
                testSubject.RegisterConnectionHandler(new RequestHandler() { ResponseText = Serialize(new ProjectInformation[0]) });
                var connectionInfo = new ConnectionInformation(new Uri("http://server"), "admin", "admin".ConvertToSecureString());

                // Act
                var projects = testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(0);
                Assert.AreSame(connectionInfo, testSubject.CurrentConnection, "Expected to be connected");
                Assert.IsNotNull(projects, "Expected projects");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_Connect_Authentication_Basic_Invalid()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup case 1: Invalid password
                testSubject.BasicAuthUsers.Add("admin", "admin1");
                var connectionInfo = new ConnectionInformation(new Uri("http://server"), "admin", "admin".ConvertToSecureString());

                // Act
                var projects = testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(1);
                Assert.IsNull(testSubject.CurrentConnection, "Invalid credentials");
                Assert.IsNull(projects, "Not expecting projects");

                // Setup case 2: Invalid user name
                testSubject.BasicAuthUsers.Clear();
                testSubject.BasicAuthUsers.Add("admin1", "admin");

                // Act
                projects = testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(2);
                Assert.IsNull(testSubject.CurrentConnection, "Invalid credentials");
                Assert.IsNull(projects, "Not expecting projects");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_UnhandledExceptions()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                testSubject.AllowAnonymous = true;
                testSubject.RegisterConnectionHandler(new RequestHandler(ctx => { throw new InvalidOperationException(); }));
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));

                // Act
                var projects = testSubject.Connect(connectionInfo, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(1);
                Assert.IsNull(testSubject.CurrentConnection, "Exception been thrown");
                Assert.IsNull(projects, "Not expecting projects");
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_GetProperties()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                ConnectToServerWithProjects(testSubject, new ProjectInformation[0]);

                // Setup
                var property1 = new ServerProperty { Key = "prop1", Value = "val1" };
                var property2 = new ServerProperty { Key = "prop2", Value = "val2" };

                var expectedProperties = new[] { property1, property2 };

                // Setup test server
                RequestHandler handler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.PropertiesAPI,
                    ctx => ServiceServerProperties(ctx, expectedProperties)
                );

                // Act
                var actualProperties = testSubject.GetProperties(CancellationToken.None).ToArray();

                // Verify
                CollectionAssert.AreEqual(expectedProperties.Select(x => x.Key).ToArray(), actualProperties.Select(x => x.Key).ToArray(), "Unexpected server property keys");
                CollectionAssert.AreEqual(expectedProperties.Select(x => x.Value).ToArray(), actualProperties.Select(x => x.Value).ToArray(), "Unexpected server property values");
                handler.AssertHandlerCalled(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_GetExportProfile()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                string language = SonarQubeServiceWrapper.CSharpLanguage;
                var profile = new QualityProfile { Key = Guid.NewGuid().ToString("N"), Language = language };
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                var expectedExport = RoslynExportProfileHelper.CreateExport(ruleSet: TestRuleSetHelper.CreateTestRuleSet(3));
                ConnectToServerWithProjects(testSubject, new[] { project });

                // Setup test server
                RegisterProfileExportQueryValidator(testSubject);

                RequestHandler getProfileHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceQualityProfiles(ctx, new[] { profile })
                );
                RequestHandler getExportHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileExportUrl(profile, language, SonarQubeServiceWrapper.RoslynExporter),
                    ctx => ServiceProfileExport(ctx, expectedExport)
                );

                // Act
                RoslynExportProfile actualExport = testSubject.GetExportProfile(project, language, CancellationToken.None);

                // Verify
                Assert.IsNotNull(actualExport, "Expected a profile export to be returned");
                RoslynExportProfileHelper.AssertAreEqual(expectedExport, actualExport);
                getExportHandler.AssertHandlerCalled(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_GetExportProfile_Exceptions()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 1))
            {
                // No project information
                Exceptions.Expect<ArgumentNullException>(() => testSubject.GetExportProfile(null, SonarQubeServiceWrapper.CSharpLanguage, CancellationToken.None));

                // Not connected
                Exceptions.Expect<InvalidOperationException>(() => testSubject.GetExportProfile(new ProjectInformation(), SonarQubeServiceWrapper.VBLanguage, CancellationToken.None));

                // Invalid language
                Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.GetExportProfile(new ProjectInformation(), "Pascal", CancellationToken.None));

                // Those are API usage issue which we don't report to the output pane
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_GetExportProfile_ServiceErrors()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup 
                testSubject.AllowAnonymous = true;
                var connectionInfo = new ConnectionInformation(new Uri("http://server"));
                var project = new ProjectInformation() { Key = "proj1" };
                testSubject.RegisterConnectionHandler(new RequestHandler() { ResponseText = Serialize(new[] { project }) });
                string csPath = SonarQubeServiceWrapper.CreateQualityProfileUrl(SonarQubeServiceWrapper.CSharpLanguage, project);
                string vbPath = SonarQubeServiceWrapper.CreateQualityProfileUrl(SonarQubeServiceWrapper.VBLanguage, project);
                testSubject.RegisterRequestHandler(csPath, new RequestHandler() { ResponseStatusCode = HttpStatusCode.BadRequest });
                testSubject.RegisterRequestHandler(vbPath, new RequestHandler() { ResponseStatusCode = HttpStatusCode.BadRequest });
                testSubject.Connect(connectionInfo, CancellationToken.None);

                // Sanity
                this.outputWindowPane.AssertOutputStrings(0);

                // Act
                var rules = testSubject.GetExportProfile(project, SonarQubeServiceWrapper.CSharpLanguage, CancellationToken.None);

                // Verify
                this.outputWindowPane.AssertOutputStrings(1);
                Assert.IsNull(rules, "Request cancelled");
            }
        }

        [TestMethod]
        public async Task SonarQubeServiceWrapper_DownloadQualityProfile_ProjectWithoutAnalysis()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                HttpClient httpClient = testSubject.CreateHttpClient();
                string language = SonarQubeServiceWrapper.CSharpLanguage;
                var expectedProfile = new QualityProfile { Key = Guid.NewGuid().ToString("N"), Language = language };
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                ConnectToServerWithProjects(testSubject, new[] { project });

                // Setup test server
                RegisterQualityProfileQueryValidator(testSubject);

                RequestHandler forProjectHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceHttpNotFound(ctx)
                );

                RequestHandler forLanguageHandler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(SonarQubeServiceWrapper.CSharpLanguage),
                    ctx => ServiceQualityProfiles(ctx, new[] { expectedProfile })
                );

                // Act
                QualityProfile actualProfile = await SonarQubeServiceWrapper.DownloadQualityProfile(httpClient, project, language, CancellationToken.None);

                // Verify
                Assert.IsNotNull(actualProfile, "Expected a quality profile");
                Assert.AreEqual(expectedProfile.Key, actualProfile.Key, "Unexpected quality profile returned");
                forProjectHandler.AssertHandlerCalled(1);
                forLanguageHandler.AssertHandlerCalled(1);
            }
        }

        [TestMethod]
        public async Task SonarQubeServiceWrapper_DownloadQualityProfile_ProjectWithAnalysis()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                HttpClient httpClient = testSubject.CreateHttpClient();
                string language = SonarQubeServiceWrapper.CSharpLanguage;
                var expectedProfile = new QualityProfile { Key = Guid.NewGuid().ToString("N"), Language = language };
                var project = new ProjectInformation { Key = "awesome1", Name = "My Awesome Project" };
                ConnectToServerWithProjects(testSubject, new[] { project });

                // Setup test server
                RegisterQualityProfileQueryValidator(testSubject);

                RequestHandler handler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.CreateQualityProfileUrl(language, project),
                    ctx => ServiceQualityProfiles(ctx, new[] { expectedProfile })
                );

                // Act
                QualityProfile actualProfile = await SonarQubeServiceWrapper.DownloadQualityProfile(httpClient, project, language, CancellationToken.None);

                // Verify
                Assert.IsNotNull(actualProfile, "Expected a quality profile");
                Assert.AreEqual(expectedProfile.Key, actualProfile.Key, "Unexpected quality profile returned");
                handler.AssertHandlerCalled(1);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_GetPlugins_ArgChecks()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider, timeoutInMilliseconds: 1))
            {
                // Null connection information
                Exceptions.Expect<ArgumentNullException>(() => testSubject.GetPlugins(null, CancellationToken.None));

                // Those are API usage issue which we don't report to the output pane
                this.outputWindowPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        public void SonarQubeServiceWrapper_GetPlugins()
        {
            using (var testSubject = new TestableSonarQubeServiceWrapper(this.serviceProvider))
            {
                // Setup
                var connectionInfo = new ConnectionInformation(new Uri("http://servername"));
                var plugin1 = new ServerPlugin { Key = "plugin1" };
                var plugin2 = new ServerPlugin { Key = "plugin2" };

                var expectedPlugins = new[] { plugin1, plugin2 };

                // Setup test server
                RequestHandler handler = testSubject.RegisterRequestHandler(
                    SonarQubeServiceWrapper.ServerPluginsInstalledAPI,
                    ctx => ServiceServerPlugins(ctx, expectedPlugins)
                );

                // Act
                var actualPlugins = testSubject.GetPlugins(connectionInfo, CancellationToken.None).ToArray();

                // Verify
                CollectionAssert.AreEqual(expectedPlugins.Select(x => x.Key).ToArray(), actualPlugins.Select(x => x.Key).ToArray(), "Unexpected server plugins");
                handler.AssertHandlerCalled(1);
            }
        }

        #endregion

        #region Helpers

        private static void ConnectToServerWithProjects(TestableSonarQubeServiceWrapper testSubject, IEnumerable<ProjectInformation> projects)
        {
            var connectionInfo = new ConnectionInformation(new Uri("http://test-server"));

            testSubject.AllowAnonymous = true;
            testSubject.RegisterConnectionHandler(new RequestHandler { ResponseText = Serialize(projects) });
            testSubject.Connect(connectionInfo, CancellationToken.None);
        }

        private static void RegisterQualityProfileQueryValidator(TestableSonarQubeServiceWrapper testSubject)
        {
            testSubject.RegisterQueryValidator(SonarQubeServiceWrapper.QualityProfileListAPI, request =>
            {
                var queryMap = ParseQuery(request.Uri.Query);
                if (queryMap.Count == 1)
                {
                    Assert.IsNotNull(queryMap["language"], "Missing query param: language");
                }
                else if (queryMap.Count == 2)
                {
                    Assert.IsNotNull(queryMap["language"], "Missing query param: language");
                    Assert.IsNotNull(queryMap["project"], "Missing query param: project");
                }
                else
                {
                    Assert.Fail("Unexpected query params.: {0}", string.Join(", ", queryMap.Keys));
                }
            });
        }

        private static void RegisterProfileExportQueryValidator(TestableSonarQubeServiceWrapper testSubject)
        {
            testSubject.RegisterQueryValidator(SonarQubeServiceWrapper.QualityProfileExportAPI, request =>
            {
                var queryMap = ParseQuery(request.Uri.Query);
                Assert.AreEqual(3, queryMap.Count, "Unexpected query params.: {0}", string.Join(", ", queryMap.Keys));
                Assert.IsNotNull(queryMap["name"], "Missing query param: name");
                Assert.IsNotNull(queryMap["language"], "Missing query param: language");
                Assert.IsNotNull(queryMap["format"], "Missing query param: format");
                Assert.AreEqual(SonarQubeServiceWrapper.RoslynExporter, queryMap["format"], "Unexpected value for query param: format");
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
                context.Response.Write(Serialize(profilesToReturn.ToArray()));
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
                Assert.AreEqual(2, keyValue.Length);
                Assert.AreEqual(HttpUtility.UrlDecode(keyValue[1]), HttpUtility.UrlDecode(HttpUtility.UrlEncode(HttpUtility.UrlDecode(keyValue[1]))), "{0} was supposed to be encoded", keyValue[0]);
                result[keyValue[0]] = HttpUtility.UrlDecode(keyValue[1]);
            }

            return result;
        }

        private static void AssertEqualProjects(ProjectInformation[] expected, ProjectInformation[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length, "Different array size");
            for (int i = 0; i < expected.Length; i++)
            {
                AssertProjectsEqualNotSame(expected[i], actual[i]);
            }
        }

        private static void AssertProjectsEqualNotSame(ProjectInformation expected, ProjectInformation actual)
        {
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected.Key, actual.Key, "Unexpected Key");
            Assert.AreEqual(expected.Name, actual.Name, "Unexpected Name");
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
            private int handlerCalled;

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

            public void AssertHandlerCalled(int expectedNumberOfTimes)
            {
                Assert.AreEqual(expectedNumberOfTimes, this.handlerCalled, "Handler was called unexpected number of times");
            }

            public int HandlerCalls
            {
                get { return this.handlerCalled; }
            }

            public void HandlerRequest(IOwinContext context)
            {
                this.handlerCalled++;
                context.Response.StatusCode = (int)this.ResponseStatusCode;
                if (this.ResponseText != null)
                {
                    context.Response.Write(this.ResponseText);
                }

                // Let the processor to set the final state, if exists.
                this.RequestProcessor?.Invoke(context);
            }
        }

        private class TestableSonarQubeServiceWrapper : SonarQubeServiceWrapper, IDisposable
        {
            private bool disposedValue = false;
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

            #endregion

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
                RequestHandler handler;
                if (this.uriRequestHandler.TryGetValue(context.Request.Uri.PathAndQuery, out handler))
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
                    Debug.WriteLine("Handler not found", context.Request.Uri.PathAndQuery);
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

            internal protected override HttpClient CreateHttpClient()
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
                    .Select(kv => AuthenticationHeaderProvider.GetBasicAuthToken(kv.Key, kv.Value.ConvertToSecureString()))
                    .Any(token => token == keyValue[1]);
            }

            #endregion

            #region IDisposable Support

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        this.server?.Dispose();
                        this.server = null;
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }

            #endregion
        }

        #endregion
    }
}
