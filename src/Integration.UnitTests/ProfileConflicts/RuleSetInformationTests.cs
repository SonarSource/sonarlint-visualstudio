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

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    [TestClass]
    public class RuleSetInformationTests
    {
        [TestMethod]
        public void RuleSetInformation_ArgChecks()
        {
            string projectFullName = "p";
            string baselineRuleSet = "br";
            string projectRuleSet = "pr";

            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(null, baselineRuleSet, projectRuleSet, null));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(projectFullName, null, projectRuleSet, null));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(projectFullName, baselineRuleSet, null, null));

            new RuleSetInformation(projectFullName, baselineRuleSet, projectRuleSet, null).Should().NotBeNull("Not expecting this to fail, just to make the static analyzer happy");
            new RuleSetInformation(projectFullName, baselineRuleSet, projectRuleSet, new string[0]).Should().NotBeNull("Not expecting this to fail, just to make the static analyzer happy");
            new RuleSetInformation(projectFullName, baselineRuleSet, projectRuleSet, new string[] { "file" }).Should().NotBeNull("Not expecting this to fail, just to make the static analyzer happy");
        }
    }
}