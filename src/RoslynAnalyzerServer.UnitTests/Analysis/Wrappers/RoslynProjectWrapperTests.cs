/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Wrappers;

[TestClass]
public class RoslynProjectWrapperTests
{
    [TestMethod]
    [DataRow(@"C:\src\View.cshtml", true)]
    [DataRow(@"C:\src\View.CSHTML", true)]
    [DataRow(@"C:\src\View.vbhtml", true)]
    [DataRow(@"C:\src\Component.razor", true)]
    [DataRow(@"C:\src\Class.cs", false)]
    [DataRow(@"C:\src\Module.vb", false)]
    [DataRow(@"C:\src\View.cshtml.cs", false)]
    public void CanBeFoundThroughGeneratedDocument_OnlyForRazorViews(string filePath, bool expected) =>
        RoslynProjectWrapper.CanBeFoundThroughGeneratedDocument(filePath).Should().Be(expected);
}
