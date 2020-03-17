/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DaemonIssueAdapterTests
    {
        [TestMethod]
        public void Ctor_NullIssue_Throws()
        {
            Action act = () => new DaemonIssueAdapter(null, "line text", "hash");

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarLintIssue");
        }

        [TestMethod]
        public void Ctor_NullPathAndHash_NoException()
        {
            var result = new DaemonIssueAdapter(new Sonarlint.Issue(), null, null);

            result.WholeLineText.Should().BeNull();
            result.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_ValidIssue()
        {
            // Arrange
            var issue = new Sonarlint.Issue
            {
                FilePath = "path",
                RuleKey = "rule id",
                StartLine = 111
            };


            // Act
            var result = new DaemonIssueAdapter(issue, "line text", "line hash");

            // Assert
            result.SonarLintIssue.Should().BeSameAs(issue);

            result.FilePath.Should().Be("path");
            result.RuleId.Should().Be("rule id");
            result.StartLine.Should().Be(111);

            result.ProjectGuid.Should().BeNull();
            result.WholeLineText.Should().Be("line text");
            result.LineHash.Should().Be("line hash");
        }
    }
}
