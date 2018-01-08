/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectRuleSetConflictTests
    {
        [TestMethod]
        public void ProjectRuleSetConflict_ArgChecks()
        {
            var info = new RuleSetInformation("projectFullName", "baselineRuleSet", "projectRuleSet", new string[0]);
            var conflict = new RuleConflictInfo();

            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetConflict(null, info));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetConflict(conflict, null));

            new ProjectRuleSetConflict(conflict, info).Should().NotBeNull("Not expecting this to fail, just to make the static analyzer happy");
        }
    }
}