/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintVulnerabilitiesUpdateTests
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void Ctor_ConfigScope_NullOrEmpty_Throws(string configScope)
    {
        var act = () => new TaintVulnerabilitiesUpdate(configScope, [], [], []);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("configurationScope");
    }

    [TestMethod]
    public void Ctor_NullAdded_Throws()
    {
        var act = () => new TaintVulnerabilitiesUpdate("some config scope", null, [], []);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("added");
    }

    [TestMethod]
    public void Ctor_NullUpdated_Throws()
    {
        var act = () => new TaintVulnerabilitiesUpdate("some config scope", [], null, []);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("updated");
    }

    [TestMethod]
    public void Ctor_NullClosed_Throws()
    {
        var act = () => new TaintVulnerabilitiesUpdate("some config scope", [], [], null);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("closed");
    }

    [TestMethod]
    public void Ctor_SetsCorrectValues()
    {
        const string configScope = "some config scope";
        var added = new List<IAnalysisIssueVisualization>();
        var updated = new List<IAnalysisIssueVisualization>();
        var closed = new List<Guid>();

        var testSubject = new TaintVulnerabilitiesUpdate(configScope, added, updated, closed);

        testSubject.ConfigurationScope.Should().BeSameAs(configScope);
        testSubject.Added.Should().BeSameAs(added);
        testSubject.Updated.Should().BeSameAs(updated);
        testSubject.Closed.Should().BeSameAs(closed);
    }
}
