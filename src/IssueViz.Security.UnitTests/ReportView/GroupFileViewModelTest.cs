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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class GroupFileViewModelTest
{
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize() => threadHandling = Substitute.For<IThreadHandling>();

    [TestMethod]
    public void Ctor_InitializesPropertiesAsExpected()
    {
        var issues = new ObservableCollection<IIssueViewModel> { Substitute.For<IIssueViewModel>() };
        var filePath = "c:\\myDir\\myFile.cs";

        var testSubject = new GroupFileViewModel(filePath, issues, threadHandling);

        testSubject.Title.Should().Be("myFile.cs");
        testSubject.FilePath.Should().Be(filePath);
        testSubject.AllIssues.Should().BeSameAs(issues);
    }
}
