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
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Api
{
    [TestClass]
    public class StatusResponseTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            Action act = () => new StatusResponse(null, "any");
            act.Should().ThrowExactly<ArgumentNullException>("ideName");

            act = () => new StatusResponse("any", null);
            act.Should().ThrowExactly<ArgumentNullException>("description");
        }

        [TestMethod]
        public void Ctor_ValidArgs_PropertiesSet()
        {
            var testSubject = new StatusResponse("name", "desc");

            testSubject.IdeName.Should().Be("name");
            testSubject.Description.Should().Be("desc");
        }
    }
}
