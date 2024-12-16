﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using SonarQube.Client.Helpers;
using System.Security;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BindingProcessImplTests
    {
        #region Tests

        [TestMethod]
        public void Ctor_ArgChecks()
        {
            var bindingArgs = CreateBindCommandArgs();
            var qpDownloader = Mock.Of<IQualityProfileDownloader>();
            var sonarQubeService = Mock.Of<ISonarQubeService>();
            var logger = Mock.Of<ILogger>();

            // 1. Null binding args
            Action act = () => new BindingProcessImpl(null, sonarQubeService, qpDownloader, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingArgs");

            // 3. Null SonarQube service
            act = () => new BindingProcessImpl(bindingArgs, null, qpDownloader, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            // 4. Null QP downloader
            act = () => new BindingProcessImpl(bindingArgs, sonarQubeService, null, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("qualityProfileDownloader");

            // 5. Null logger
            act = () => new BindingProcessImpl(bindingArgs, sonarQubeService, qpDownloader, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        private static ServerExclusions CreateSettings()
        {
            return new ServerExclusions
            {
                Inclusions = new string[] { "inclusion1", "inclusion2" },
                Exclusions = new string[] { "exclusion" },
                GlobalExclusions = new string[] { "globalExclusion" }
            };
        }

        [TestMethod]
        public async Task DownloadQualityProfile_CreatesBoundProjectAndCallsQPDownloader()
        {
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            var progress = Mock.Of<IProgress<FixedStepsProgress>>();

            var bindingArgs = CreateBindCommandArgs("the project key", "http://theServer");

            var testSubject = CreateTestSubject(bindingArgs,
                qpDownloader: qpDownloader.Object);

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progress, CancellationToken.None);

            result.Should().BeTrue();

            qpDownloader.Verify(x => x.UpdateAsync(It.IsAny<BoundServerProject>(), progress, It.IsAny<CancellationToken>()),
                Times.Once);

            var actualProject = (BoundServerProject)qpDownloader.Invocations[0].Arguments[0];

            // Check the bound project was correctly constructed from the BindCommandArgs
            actualProject.Should().NotBeNull();
            actualProject.ServerConnection.ServerUri.Should().Be("http://theServer");
            actualProject.ServerProjectKey.Should().Be("the project key");
        }

        [TestMethod]
        public async Task DownloadQualityProfile_HandlesInvalidOperationException()
        {
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            qpDownloader
                .Setup(x =>
                    x.UpdateAsync(It.IsAny<BoundServerProject>(),
                        It.IsAny<IProgress<FixedStepsProgress>>(),
                        It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException());

            var testSubject = CreateTestSubject(
                qpDownloader: qpDownloader.Object);

            // Act
            var result =
                await testSubject.DownloadQualityProfileAsync(Mock.Of<IProgress<FixedStepsProgress>>(), CancellationToken.None);

            result.Should().BeFalse();
            qpDownloader.Verify(x => x.UpdateAsync(It.IsAny<BoundServerProject>(),
                    It.IsAny<IProgress<FixedStepsProgress>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion Tests

        #region Helpers

        private BindingProcessImpl CreateTestSubject(BindCommandArgs bindingArgs = null,
            ISonarQubeService sonarQubeService = null,
            IQualityProfileDownloader qpDownloader = null,
            ILogger logger = null)
        {
            bindingArgs = bindingArgs ?? CreateBindCommandArgs();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            qpDownloader ??= Mock.Of<IQualityProfileDownloader>();
            logger ??= new TestLogger(logToConsole: true);

            return new BindingProcessImpl(bindingArgs,
                sonarQubeService,
                qpDownloader,
                logger);
        }

        private BindCommandArgs CreateBindCommandArgs(string projectKey = "key", string serverUri = "http://any")
        {
            return new BindCommandArgs(new BoundServerProject("any", projectKey, new ServerConnection.SonarQube(new Uri(serverUri))));
        }

        private static void CheckIsExpectedPassword(string expectedRawPassword, SecureString actualPassword)
        {
            // The SecureString extension methods in SonarQube.Client.Helpers.SecureStringHelper throw for
            // nulls
            if (expectedRawPassword == null)
            {
                actualPassword.Should().BeNull();
            }
            else
            {
                actualPassword.ToUnsecureString().Should().Be(expectedRawPassword);
            }
        }
        #endregion Helpers
    }
}
