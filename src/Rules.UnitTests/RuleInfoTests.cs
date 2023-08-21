/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class RuleInfoTests
    {
        [TestMethod]
        public void Ctor_NullCollectionsAreAllowed_SetToEmptyCollections()
        {
            var testSubject = new RuleInfo(
                languageKey: null,
                fullRuleKey: null,
                description: null,
                name: null,
                RuleIssueSeverity.Unknown,
                RuleIssueType.Unknown,
                isActiveByDefault: false,
                tags: null,
                descriptionSections: null,
                educationPrinciples: null,
                htmlNote: null,
                cleanCodeAttribute: null,
                defaultImpacts: null);

            testSubject.Tags.Should().NotBeNull();
            testSubject.DescriptionSections.Should().NotBeNull();
            testSubject.EducationPrinciples.Should().NotBeNull();

            testSubject.Tags.Should().HaveCount(0);
            testSubject.DescriptionSections.Should().HaveCount(0);
            testSubject.EducationPrinciples.Should().HaveCount(0);
        }
    }
}
