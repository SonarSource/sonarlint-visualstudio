/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssuesSnapshotTests
    {
        private IssuesSnapshot snapshot;
        private Issue issue;

        [TestInitialize]
        public void SetUp()
        {
            var path = "foo.js";
            issue = new Issue()
            {
                FilePath = path,
                Message = "This is dangerous",
                RuleKey = "javascript:123",
                Severity = Issue.Types.Severity.Blocker,
            };

            var marker = new IssueMarker(issue, new SnapshotSpan());

            snapshot = new IssuesSnapshot("MyProject", path, 1, new List<IssueMarker>() { marker });
        }

        [TestMethod]
        public void Path()
        {
            GetValue(StandardTableKeyNames.DocumentName).Should().Be(issue.FilePath);
        }

        [TestMethod]
        public void Message()
        {
            GetValue(StandardTableKeyNames.Text).Should().Be(issue.Message);
        }

        [TestMethod]
        public void ErrorCode()
        {
            GetValue(StandardTableKeyNames.ErrorCode).Should().Be(issue.RuleKey);
        }

        [TestMethod]
        public void Severity()
        {
            GetValue(StandardTableKeyNames.ErrorSeverity).Should().NotBeNull();
        }

        private object GetValue(string columnName)
        {
            object content;
            snapshot.TryGetValue(0, columnName, out content).Should().BeTrue();
            return content;
        }
    }
}
