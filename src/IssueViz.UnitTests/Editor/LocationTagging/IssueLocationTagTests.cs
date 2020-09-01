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
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class IssueLocationTagTests
    {
        [TestMethod]
        public void Ctor_InvalidArgument_Throws()
        {
            Action act = () => new IssueLocationTag(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("location");
        }

        [TestMethod]
        public void Ctor_ValidArg_SetsProperty()
        {
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();
            var testSubject = new IssueLocationTag(location);
            testSubject.Location.Should().BeSameAs(location);
        }
    }
}
