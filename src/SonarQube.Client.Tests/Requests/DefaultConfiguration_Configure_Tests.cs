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

using SonarQube.Client.Api;
using SonarQube.Client.Requests;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Requests;

[TestClass]
public class DefaultConfiguration_Configure_Tests
{
    [TestMethod]
    public void ConfigureSonarQube_Writes_Debug_Messages()
    {
        var logger = new TestLogger();

        var expected = new[]
        {
            "Registered SonarQube.Client.Api.V2_10.GetVersionRequest for 2.1",
            "Registered SonarQube.Client.Api.V3_30.ValidateCredentialsRequest for 3.3",
            "Registered SonarQube.Client.Api.V6_60.GetNotificationsRequest for 6.6",
            "Registered SonarQube.Client.Api.V6_60.GetProjectBranchesRequest for 6.6",
        };

        DefaultConfiguration.ConfigureSonarQube(new RequestFactory(logger));

        DumpDebugMessages(logger);

        logger.DebugMessages.Should().ContainInOrder(expected);
        logger.DebugMessages.Count.Should().Be(expected.Length);
    }

    [TestMethod]
    public void ConfigureSonarCloud_Writes_Debug_Messages()
    {
        var logger = new TestLogger();

        var expected = new[]
        {
            "Registered SonarQube.Client.Api.V2_10.GetVersionRequest",
            "Registered SonarQube.Client.Api.V3_30.ValidateCredentialsRequest",
            "Registered SonarQube.Client.Api.V6_60.GetNotificationsRequest",
            "Registered SonarQube.Client.Api.V6_60.GetProjectBranchesRequest",
        };

        DefaultConfiguration.ConfigureSonarCloud(new UnversionedRequestFactory(logger));

        DumpDebugMessages(logger);

        logger.DebugMessages.Should().ContainInOrder(expected);
        logger.DebugMessages.Count.Should().Be(expected.Length);
    }

    [TestMethod]
    public void ConfigureSonarQube_CheckAllRequestsImplemented()
    {
        var testSubject = DefaultConfiguration.ConfigureSonarQube(new RequestFactory(new TestLogger()));
        var serverInfo = new ServerInfo(null /* latest */, ServerType.SonarQube);

        testSubject.Create<IGetNotificationsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetVersionRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IValidateCredentialsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetProjectBranchesRequest>(serverInfo).Should().NotBeNull();
    }

    [TestMethod]
    public void ConfigureSonarCloud_CheckAllRequestsImplemented()
    {
        var testSubject = DefaultConfiguration.ConfigureSonarCloud(new UnversionedRequestFactory(new TestLogger()));
        var serverInfo = new ServerInfo(null /* latest */, ServerType.SonarQube);

        testSubject.Create<IGetNotificationsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetVersionRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IValidateCredentialsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetProjectBranchesRequest>(serverInfo).Should().NotBeNull();
    }

    private static void DumpDebugMessages(TestLogger logger)
    {
        Debug.WriteLine("Actual registered requests:");
        foreach (var message in logger.DebugMessages)
        {
            Debug.WriteLine(message);
        }
    }
}
