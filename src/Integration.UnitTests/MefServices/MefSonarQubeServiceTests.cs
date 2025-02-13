/*
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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarQube.Client;
using SonarQube.Client.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class MefSonarQubeServiceTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<MefSonarQubeService, ISonarQubeService> (
            MefTestHelpers.CreateExport<IUserAgentProvider>(Substitute.For<IUserAgentProvider>()),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void Ctor_GetsUserAgent()
    {
        var userAgentProvider = Substitute.For<IUserAgentProvider>();

        _ = new MefSonarQubeService(userAgentProvider, Substitute.For<ILogger>(), Substitute.For<IThreadHandling>());

        _ = userAgentProvider.Received().UserAgent;
    }

    [TestMethod]
    public async Task ExecutesCallsOnBackgroundThread()
    {
        var userAgentProvider = Substitute.For<IUserAgentProvider>();
        userAgentProvider.UserAgent.Returns("any user agent");

        // Limited check - just that the class does use the threadHandling abstraction
        // to try to execute in the background
        var connectionInfo = new ConnectionInformation(new Uri("http://localhost:123"));

        var threadHandling = Substitute.For<IThreadHandling>();
        // Assuming the first call is to IGetVersionRequest, which returns a string
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<string>>>())
            .ThrowsAsync(new IndexOutOfRangeException("this is a test"));

        var testSubject = new MefSonarQubeService(userAgentProvider,Substitute.For<ILogger>(), threadHandling);

        var func = async () => await testSubject.ConnectAsync(connectionInfo, CancellationToken.None);

        (await func.Should().ThrowExactlyAsync<IndexOutOfRangeException>())
            .Which.Message.Should().Be("this is a test");
    }
}
