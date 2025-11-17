/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
public class IssuePublisherTests
{
    private IIssueConsumerStorage issueConsumerStorage;
    private IIssueConsumer issueConsumer;
    private IssuePublisher testSubject;

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<IssuePublisher, IIssuePublisher>(MefTestHelpers.CreateExport<IIssueConsumerStorage>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<IssuePublisher>();

    [TestInitialize]
    public void TestInitialize()
    {
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        issueConsumer = Substitute.For<IIssueConsumer>();
        testSubject = new IssuePublisher(issueConsumerStorage);
    }

    [TestMethod]
    public void FindingsType_ReturnsCorrectValue() => testSubject.FindingsType.Should().Be(CoreStrings.FindingType_Issue);

    [TestMethod]
    public void PublishIssues_NoConsumerInStorage_DoesNothing()
    {
        issueConsumerStorage.TryGet(default, out _).ReturnsForAnyArgs(false);

        var act = () => testSubject.Publish("file/path", Substitute.For<IEnumerable<IAnalysisIssue>>());

        act.Should().NotThrow();
        issueConsumer.DidNotReceiveWithAnyArgs().SetIssues(default, default);
        issueConsumer.DidNotReceiveWithAnyArgs().SetHotspots(default, default);
    }

    [TestMethod]
    public void PublishIssues_MatchingConsumer_PublishesIssues()
    {
        var analysisIssues = Substitute.For<IEnumerable<IAnalysisIssue>>();
        issueConsumerStorage.TryGet("file/path", out Arg.Any<IIssueConsumer>())
            .Returns(info =>
            {
                info[1] = issueConsumer;
                return true;
            });

        testSubject.Publish("file/path", analysisIssues);

        issueConsumer.Received().SetIssues("file/path", analysisIssues);
        issueConsumer.DidNotReceiveWithAnyArgs().SetHotspots(default, default);
    }
}
