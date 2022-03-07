using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    internal class TestHelper
    {
        internal static SonarQubeIssue CreateIssue(string issueKey)
        {
            return new SonarQubeIssue(issueKey,
                "path",
                "hash",
                "message",
                "moduleKey",
                "ruleId",
                true,
                SonarQubeIssueSeverity.Blocker,
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                new IssueTextRange(0, 1, 2, 3),
                new List<IssueFlow> {
                    new IssueFlow(
                        new List<IssueLocation> {
                            new IssueLocation("filepath",
                                "moduleKey",
                                new IssueTextRange(10, 11, 12, 13), "locationMEssage") }) });
        }
    }
}
