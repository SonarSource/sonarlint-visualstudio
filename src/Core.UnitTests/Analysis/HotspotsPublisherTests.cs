/*
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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis;

[TestClass]
public class HotspotPublisherTests
{
    private IIssueConsumerStorage issueConsumerStorage;
    private IIssueConsumer issueConsumer;
    private HotspotPublisher testSubject;

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<HotspotPublisher, IHotspotPublisher>(MefTestHelpers.CreateExport<IIssueConsumerStorage>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<HotspotPublisher>();

    [TestInitialize]
    public void TestInitialize()
    {
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        issueConsumer = Substitute.For<IIssueConsumer>();
        testSubject = new HotspotPublisher(issueConsumerStorage);
    }

    [TestMethod]
    public void FindingsType_ReturnsCorrectValue() =>
        testSubject.FindingsType.Should().Be(CoreStrings.FindingType_Hotspot);

    [TestMethod]
    public void PublishHotspots_NoConsumerInStorage_DoesNothing()
    {
        issueConsumerStorage.TryGet(default, out _, out _).ReturnsForAnyArgs(false);

        var act = () => testSubject.Publish("file/path", Guid.NewGuid(), Substitute.For<IEnumerable<IAnalysisIssue>>());

        act.Should().NotThrow();
        issueConsumer.DidNotReceiveWithAnyArgs().SetIssues(default, default);
        issueConsumer.DidNotReceiveWithAnyArgs().SetHotspots(default, default);
    }

    [TestMethod]
    public void PublishHotspots_DifferentAnalysisId_DoesNothing()
    {
        issueConsumerStorage.TryGet("file/path", out Arg.Any<Guid>(), out Arg.Any<IIssueConsumer>())
            .Returns(info =>
            {
                info[1] = Guid.NewGuid();
                info[2] = issueConsumer;
                return true;
            });

        testSubject.Publish("file/path", Guid.NewGuid(), Substitute.For<IEnumerable<IAnalysisIssue>>());

        issueConsumer.DidNotReceiveWithAnyArgs().SetIssues(default, default);
        issueConsumer.DidNotReceiveWithAnyArgs().SetHotspots(default, default);
    }

    [TestMethod]
    public void PublishHotspots_MatchingConsumer_PublishesHotspots()
    {
        var analysisId = Guid.NewGuid();
        var analysisIssues = Substitute.For<IEnumerable<IAnalysisIssue>>();
        issueConsumerStorage.TryGet("file/path", out Arg.Any<Guid>(), out Arg.Any<IIssueConsumer>())
            .Returns(info =>
            {
                info[1] = analysisId;
                info[2] = issueConsumer;
                return true;
            });

        testSubject.Publish("file/path", analysisId, analysisIssues);

        issueConsumer.Received().SetHotspots("file/path", analysisIssues);
        issueConsumer.DidNotReceiveWithAnyArgs().SetIssues(default, default);
    }
}
