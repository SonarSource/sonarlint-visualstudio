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
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class FilterableIssueAdapterTests
    {
        [TestMethod]
        public void Ctor_NullIssue_Throws()
        {
            Action act = () => new FilterableIssueAdapter(null, "line text", "hash");

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarLintIssue");
        }

        [TestMethod]
        public void Ctor_NullPathAndHash_PathAndHashAreSetToNull()
        {
            var result = (IFilterableIssue)new FilterableIssueAdapter(new DummyAnalysisIssue(), null, null);

            result.WholeLineText.Should().BeNull();
            result.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_ValidIssue_CorrectlyInitialized()
        {
            // Arrange
            var issue = new DummyAnalysisIssue
            {
                FilePath = "path",
                RuleKey = "rule id",
                StartLine = 111
            };

            // Act
            var result = new FilterableIssueAdapter(issue, "line text", "line hash");

            // Assert
            result.SonarLintIssue.Should().BeSameAs(issue);

            var filterableResult = (IFilterableIssue)result;
            filterableResult.FilePath.Should().Be("path");
            filterableResult.RuleId.Should().Be("rule id");
            filterableResult.StartLine.Should().Be(111);

            filterableResult.ProjectGuid.Should().BeNull();
            filterableResult.WholeLineText.Should().Be("line text");
            filterableResult.LineHash.Should().Be("line hash");
        }
    }
}
