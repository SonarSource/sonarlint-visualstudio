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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Suppression;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class IssuesFilterTests
    {
        private Mock<ISuppressedIssueMatcher> issueMatcher;
        private IssuesFilter testSubject;

        private readonly IList<IFilterableIssue> matchedIssues = new List<IFilterableIssue>();

        private readonly IFilterableIssue Issue1 = CreateIssue();
        private readonly IFilterableIssue Issue2 = CreateIssue();
        private readonly IFilterableIssue Issue3 = CreateIssue();
        private readonly IFilterableIssue Issue4 = CreateIssue();
        private readonly IFilterableIssue Issue5 = CreateIssue();

        [TestInitialize]
        public void TestInitialize()
        {
            issueMatcher = new Mock<ISuppressedIssueMatcher>();
            issueMatcher.Setup(x => x.SuppressionExists(It.IsAny<IFilterableIssue>()))
                .Returns((IFilterableIssue i) => matchedIssues.Contains(i));

            testSubject = new IssuesFilter(issueMatcher.Object);
        }

        [TestMethod]
        public void Ctor_NullSonarQubeIssuesProvider_ThrowsArgumentNullException()
        {
            Action act = () => new IssuesFilter((SuppressedIssueMatcher)null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issueMatcher");
        }

        [TestMethod]
        public void Filter_NullIssues_ThrowsArgumentNullException()
        {
            Action act = () => testSubject.Filter(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issues");
        }

        [TestMethod]
        public void Filter_NoMatchingIssues_ReturnsOriginalIssues()
        {
            // Arrange
            var inputIssues = new[] { Issue1, Issue2, Issue3 };
            SetMatchableIssues(Issue4, Issue5);

            // Act
            var result = testSubject.Filter(inputIssues);

            // Assert
            result.Should().BeEquivalentTo(inputIssues);
        }

        [TestMethod]
        public void Filter_SomeIssuesMatch_ReturnsUnmatchedIssues()
        {
            // Arrange
            var inputIssues = new[] { Issue1, Issue2, Issue3, Issue4 };
            SetMatchableIssues(Issue1, Issue2, Issue5);

            // Act
            var result = testSubject.Filter(inputIssues);

            // Assert
            result.Should().BeEquivalentTo(Issue3, Issue4);
        }

        [TestMethod]
        public void Filter_AllIssuesMatch_ReturnsEmptyList()
        {
            // Arrange
            var inputIssues = new[] { Issue1, Issue2, Issue3, Issue4 };
            SetMatchableIssues(inputIssues);

            // Act
            var result = testSubject.Filter(inputIssues);

            // Assert
            result.Should().BeEmpty();
        }

        private static IFilterableIssue CreateIssue()
            => new Mock<IFilterableIssue>().Object;

        private void SetMatchableIssues(params IFilterableIssue[] issues)
        {
            matchedIssues.AddRange(issues);
        }
    }
}
