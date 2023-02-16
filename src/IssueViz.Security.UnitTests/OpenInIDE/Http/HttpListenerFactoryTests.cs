﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin.Host.HttpListener;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Http
{
    [TestClass]
    public class HttpListenerFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<HttpListenerFactory, IHttpListenerFactory>(
                MefTestHelpers.CreateExport<IOwinPipelineProcessor>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        [TestCategory("Integration")]
        [DataRow(10000, 10003, 10000)]
        [DataRow(10000, 10003, 10001)]
        [DataRow(10000, 10003, 10002)]
        [DataRow(20000, 20003, 20003)]
        [DataRow(20000, 20005, 20006)] // no free ports in the range
        public void Create_FirstAvailablePortIsUsed(int startPort, int endPort, int firstFreePort)
        {
            IHttpListenerFactory testSubject = new HttpListenerFactory(Mock.Of<IOwinPipelineProcessor>(), new TestLogger(logToConsole: true));

            bool isFreePortInRange = firstFreePort >= startPort && firstFreePort <= endPort;

            var allPorts = CreateRange(startPort, endPort);

            // If any of the ports required by the test are not available then bail out.
            if(!AreAllPortsAvailable(allPorts))
            {
                Assert.Inconclusive("Test setup error: some test ports are in use.");
            }

            // The unavailable ports are all at the start of the range.
            var unavailablePorts = CreateRange(startPort, firstFreePort - 1);
            var remainingFreePorts = CreateRange(firstFreePort + 1, endPort);

            using(new PortGrabber(unavailablePorts))
            using (var actual = testSubject.Create(startPort, endPort))
            {
                if (isFreePortInRange)
                {
                    var expectedPort = GetPrefix(firstFreePort);
                    actual.Should().NotBeNull();
                    actual.Should().BeAssignableTo<OwinHttpListener>();

                    var owinListener = (OwinHttpListener)actual;
                    var httpListener = owinListener.Listener;

                    httpListener.IsListening.Should().BeTrue();
                    httpListener.Prefixes.Count.Should().Be(1);
                    httpListener.Prefixes.First().Should().Be(expectedPort);
                    IsPortAvailable(firstFreePort).Should().BeFalse();
                }
                else
                {
                    actual.Should().BeNull();
                }

                AreAllPortsAvailable(remainingFreePorts).Should().BeTrue();
            }

            // The port used by the test subject should have been released on disposal
            IsPortAvailable(firstFreePort).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ListenerPipeline_ExceptionInHandler_Returns500()
        {
            const int port = 10000;
            // If any of the ports required by the test are not available then bail out.
            if (!AreAllPortsAvailable(port))
            {
                Assert.Inconclusive("Test setup error: some test ports are in use.");
            }

            // Create a listener with an inner handler that will always throw
            var handlerExecuted = false;
            var innerProcessorMock = new Mock<IOwinPipelineProcessor>();
            innerProcessorMock.Setup(x => x.ProcessRequest(It.IsAny<IDictionary<string, object>>()))
                .Callback(() =>
                {
                    handlerExecuted = true;
                    throw new InvalidOperationException("exception thrown by test code");
                });

            IHttpListenerFactory factory = new HttpListenerFactory(innerProcessorMock.Object , new TestLogger(logToConsole: true));
            using var listener = factory.Create(port, port);

            // Send a request to the listener that should be passed to the pipeline
            using var httpClient = new HttpClient();
            var address = GetPrefix(port);
            var request = new HttpRequestMessage(HttpMethod.Get, address);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead)
                .ConfigureAwait(false);

            response.StatusCode.Should().Be(500);
            handlerExecuted.Should().BeTrue();
        }

        private static int[] CreateRange (int start, int end)
        {
            if (start > end)
            {
                return Array.Empty<int>();
            }
            return Enumerable.Range(start, end - start + 1).ToArray();
        }

        private static bool AreAllPortsAvailable(params int[] ports) =>
            ports.All(IsPortAvailable);

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using (var listener = new HttpListener())
                {
                    listener.Prefixes.Add(GetPrefix(port));
                    listener.Start();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static string GetPrefix(int port) => $"http://localhost:{port}/sonarlint/api/";

        /// <summary>
        /// Helper class to listen on the specified port(s) until the
        /// object is disposed. Throws if any of the ports are not free.
        /// </summary>
        private sealed class PortGrabber : IDisposable
        {
            private readonly HttpListener testListener;

            public PortGrabber(params int[] portsToGrab)
            {
                Console.WriteLine($"Grabbing ports: {string.Join(",", portsToGrab) ?? "{none}"}");

                if (portsToGrab.Length == 0)
                {
                    return;
                }

                testListener = new HttpListener();
                foreach(var port in portsToGrab)
                {
                    testListener.Prefixes.Add(GetPrefix(port));
                }

                testListener.Start();
                Console.WriteLine("Ports successfully grabbed");
            }

            public void Dispose()
            {
                ((IDisposable)testListener)?.Dispose();
            }
        }
    }
}
