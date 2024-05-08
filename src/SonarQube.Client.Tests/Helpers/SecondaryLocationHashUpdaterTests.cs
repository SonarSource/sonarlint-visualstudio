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

using System.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Helpers.Tests
{
    [TestClass]
    public class SecondaryLocationHashUpdaterTests
    {
        private const string PrimaryIssueHash = "the primary issue hash should not be affected";

        [TestMethod]
        public async Task Populate_NoIssues_NoOp()
        {
            var serviceMock = new Mock<ISonarQubeService>();

            var testSubject = new SecondaryLocationHashUpdater();
            await testSubject.UpdateHashesAsync(Array.Empty<SonarQubeIssue>(), serviceMock.Object, CancellationToken.None);

            serviceMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task Populate_NoSecondaryLocations_NoOp()
        {
            var serviceMock = new Mock<ISonarQubeService>();
            var issues = new[]
            {
                CreateIssue("project1:key1", AddFlow()),
                CreateIssue("project2:key2" /* no flows */)
            };

            var testSubject = new SecondaryLocationHashUpdater();
            await testSubject.UpdateHashesAsync(issues, serviceMock.Object, CancellationToken.None);

            serviceMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task Populate_HasSecondaryLocations_UniqueModulesFetched()
        {
            var issues = new[]
            {
                CreateIssue("primary_only_should_not_be_fetched_1",
                    AddFlow(
                        CreateLocation("duplicate"),
                        CreateLocation("duplicate"),
                        CreateLocation("unique1"),
                        CreateLocation("unique2")
                        )),
                CreateIssue("unique2",
                    AddFlow(
                        CreateLocation("unique3")
                        )),
                CreateIssue("primary_only_should_not_be_fetched_2",
                    AddFlow(
                        CreateLocation("unique4")
                        ))
            };

            // Only expecting the unique set of secondary locations to be requested
            var serviceMock = new Mock<ISonarQubeService>();
            AddSourceFile(serviceMock, "duplicate");
            AddSourceFile(serviceMock, "unique1");
            AddSourceFile(serviceMock, "unique2");
            AddSourceFile(serviceMock, "unique3");
            AddSourceFile(serviceMock, "unique4");

            var testSubject = new SecondaryLocationHashUpdater();
            await testSubject.UpdateHashesAsync(issues, serviceMock.Object, CancellationToken.None);

            serviceMock.VerifyAll();
            serviceMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Populate_HasSecondaryLocations_ExpectedHashesSet()
        {
            const string line1Contents = "line one contents";
            const string line2Contents = " line two XXX ";
            const string line3Contents = "  LINE THREE!\"£$% ";

            var file1Contents = $"{line1Contents}\n{line2Contents}\nfoo foo foo";

            var file2Contents = $"111\n222\n{line3Contents}";

            var calcMock = new Mock<IChecksumCalculator>();
            SetupHash(calcMock, line1Contents, "expected line 1 hash");
            SetupHash(calcMock, line2Contents, "expected line 2 hash");
            SetupHash(calcMock, line3Contents, "expected line 3 hash");

            var issues = new[]
            {
                CreateIssue("primary_only_should_not_be_fetched_1",
                    AddFlow(
                        CreateLocation("file1", startLine: 1),
                        CreateLocation("file1", startLine: 2)
                        )),
                CreateIssue("primary_only_should_not_be_fetched_2",
                    AddFlow(
                        CreateLocation("file2", startLine: 4), // beyond the end of the file -> should be ignored
                        CreateLocation("file2", startLine: 3)
                        ))
            };

            var serviceMock = new Mock<ISonarQubeService>();
            AddSourceFile(serviceMock, "file1", file1Contents);
            AddSourceFile(serviceMock, "file2", file2Contents);

            var testSubject = new SecondaryLocationHashUpdater(calcMock.Object);

            // Act
            await testSubject.UpdateHashesAsync(issues, serviceMock.Object, CancellationToken.None);

            issues[0].Hash.Should().Be(PrimaryIssueHash);
            issues[1].Hash.Should().Be(PrimaryIssueHash);

            issues[0].Flows[0].Locations[0].Hash.Should().Be("expected line 1 hash");
            issues[0].Flows[0].Locations[1].Hash.Should().Be("expected line 2 hash");

            issues[1].Flows[0].Locations[0].Hash.Should().Be(null);
            issues[1].Flows[0].Locations[1].Hash.Should().Be("expected line 3 hash");
        }

        private static SonarQubeIssue CreateIssue(string moduleKey, params IssueFlow[] flows) =>
            new SonarQubeIssue("any", "any", PrimaryIssueHash, "any", moduleKey, "any", true,
                SonarQubeIssueSeverity.Blocker, DateTimeOffset.Now, DateTimeOffset.Now, null, flows.ToList());

        private static IssueFlow AddFlow(params IssueLocation[] locations) =>
            new IssueFlow(locations.ToList());

        private static IssueLocation CreateLocation(string moduleKey, int startLine = 1) =>
            new IssueLocation("any", moduleKey,
                new IssueTextRange(startLine, int.MaxValue, int.MaxValue, int.MaxValue),
                "any");

        private static void AddSourceFile(Mock<ISonarQubeService> serviceMock, string moduleKey, string data = "") =>
            serviceMock.Setup(x => x.GetSourceCodeAsync(moduleKey, It.IsAny<CancellationToken>())).Returns(Task.FromResult(data));

        private static void SetupHash(Mock<IChecksumCalculator> calcMock, string input, string hashToReturn) =>
            calcMock.Setup(x => x.Calculate(input)).Returns(hashToReturn);
    }
}
