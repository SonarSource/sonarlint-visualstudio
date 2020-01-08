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
using System.Collections.Generic;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    [TestClass]
    public class RuleConflictInfoTests
    {
        [TestMethod]
        public void RuleConflictInfo_Ctor_ArgChecks()
        {
            // Arrange
            IEnumerable<RuleReference> ruleRefs = null;
            IDictionary<RuleReference, RuleAction> rulesMap = null;

            IEnumerable<RuleReference> ruleRefsNull = new RuleReference[0];
            IDictionary<RuleReference, RuleAction> rulesMapNull = new Dictionary<RuleReference, RuleAction>();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => new RuleConflictInfo(ruleRefsNull, rulesMap));
            Exceptions.Expect<ArgumentNullException>(() => new RuleConflictInfo(ruleRefs, rulesMapNull));
        }
    }
}