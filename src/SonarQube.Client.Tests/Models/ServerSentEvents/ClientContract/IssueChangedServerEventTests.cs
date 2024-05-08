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

using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarQube.Client.Tests.Models.ServerSentEvents.ClientContract
{
    [TestClass]
    public class IssueChangedServerEventTests
    {
        [TestMethod]
        public void Ctor_NullIssuesArray_Throws()
        {
            // The implementation of ToString assumes the issues array is not null,
            // so we'll check that the constructor actually enforces this
            Action act = () => new IssueChangedServerEvent("any", true, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issues");
        }

        [TestMethod]
        public void ToString_EmptyList_ContainsExpectedStrings()
        {
            var testSubject = new IssueChangedServerEvent("projectKey A", true, Array.Empty<BranchAndIssueKey>());

            var actual = testSubject.ToString();

            actual.Should().ContainAll("projectKey A", "True", "0");
        }

        [TestMethod]
        public void ToString_MultipleItemsInList_ContainsExpectedStrings()
        {
            var testSubject = new IssueChangedServerEvent("projectKey B", false,
                new BranchAndIssueKey[]
                {
                    new BranchAndIssueKey("issuekey1", "branch1"),
                    new BranchAndIssueKey("issuekey2", "branch2")
                });

            var actual = testSubject.ToString();

            actual.Should().ContainAll("projectKey B", "False", "2",
                "issuekey1", "branch1",
                "issuekey2", "branch2");
        }
    }
}
