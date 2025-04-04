﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarQube.Client.Requests;
using ILogger = SonarQube.Client.Logging.ILogger;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetViewHotspotUrl_Disconnect : SonarQubeService_TestBase
    {
        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/3142")]
        public async Task GetViewHotspotUrl_DisconnectedInTheMiddle_NoException()
        {
            await ConnectToSonarQube("3.3.0.0", serverUrl: "https://sonarcloud.io");

            var result = service.GetViewHotspotUrl("myProject", "myHotspot");

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new Uri("https://sonarcloud.io/project/security_hotspots?id=myProject&hotspots=myHotspot"));
        }

        protected internal override SonarQubeService CreateTestSubject()
        {
            return new DisconnectingService(httpClientHandlerFactory.Object, UserAgent, logger, languageProvider, requestFactorySelector);
        }

        internal class DisconnectingService : SonarQubeService
        {
            internal DisconnectingService(
                IHttpClientHandlerFactory httpClientHandlerFactory,
                string userAgent,
                ILogger logger,
                ILanguageProvider languageProvider,
                IRequestFactorySelector requestFactorySelector)
                : base(httpClientHandlerFactory, userAgent, logger, languageProvider, requestFactorySelector, null)
            {
            }

            protected override ServerInfo EnsureIsConnected()
            {
                var serverInfo = base.EnsureIsConnected();

                // Simulate disconnecting immediately after calling Ensure
                Disconnect();

                return serverInfo;
            }
        }
    }
}
