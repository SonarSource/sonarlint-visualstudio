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
    public class RuleInfoJsonDeserializerTests
    {
        [TestMethod]
        public void Deserialize()
        {
            const string data = @"{
  'FullRuleKey': 'javascript:S6509',
  'Name': 'Extra boolean casts should be removed',
  'Severity': 3,
  'IssueType': 0,
  'IsActiveByDefault': true,
  'LanguageKey': 'js',
  'Description': 'description',
  'Tags': [],
  'DescriptionSections': [
    {
      'Key': 'introduction',
      'HtmlContent': 'content1',
      'Context': null
    },
    {
      'Key': 'root_cause',
      'HtmlContent': 'content2',
      'Context': 
        {
            'Key': 'context key',
            'DisplayName': 'context display name'
        }
    }
  ],
  'EducationPrinciples': [ 'aaa', 'bbb' ],
  'HtmlNote': null
}";

            var actual = LocalRuleMetadataProvider.RuleInfoJsonDeserializer.Deserialize(data);

            actual.Should().NotBeNull();
            actual.DescriptionSections.Should().HaveCount(2);
            actual.DescriptionSections[0].Key.Should().Be("introduction");
            actual.DescriptionSections[0].HtmlContent.Should().Be("content1");
            actual.DescriptionSections[0].Context.Should().BeNull();

            actual.DescriptionSections[1].Key.Should().Be("root_cause");
            actual.DescriptionSections[1].HtmlContent.Should().Be("content2");
            actual.DescriptionSections[1].Context.Should().NotBeNull();
            actual.DescriptionSections[1].Context.Key.Should().Be("context key");
            actual.DescriptionSections[1].Context.DisplayName.Should().Be("context display name");

            actual.EducationPrinciples.Should().HaveCount(2);
            actual.EducationPrinciples[0].Should().Be("aaa");
            actual.EducationPrinciples[1].Should().Be("bbb");
        }
    }
}
