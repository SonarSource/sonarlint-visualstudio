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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Issue;

[TestClass]
public class AddIssueCommentParamsTests
{
    [TestMethod]
    public void Serialized_AsExpected()
    {
        const string expected = """
                                {
                                  "configurationScopeId": "CONFIG_SCOPE_ID",
                                  "issueKey": "ISSUE_KEY",
                                  "text": "COMMENT_TEXT"
                                }
                                """;

        var addIssueCommentParams = new AddIssueCommentParams("CONFIG_SCOPE_ID", "ISSUE_KEY", "COMMENT_TEXT");

        JsonConvert.SerializeObject(addIssueCommentParams, Formatting.Indented).Should().Be(expected);
    }
}
