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
  'Description': '\u003Cp\u003EIn JavaScript, every value can be coerced into a boolean value: either \u003Ccode\u003Etrue\u003C/code\u003E or \u003Ccode\u003Efalse\u003C/code\u003E.\u003C/p\u003E\n\u003Cp\u003EValues that are coerced into \u003Ccode\u003Etrue\u003C/code\u003E are said to be \u003Cem\u003Etruthy\u003C/em\u003E, and those coerced into \u003Ccode\u003Efalse\u003C/code\u003E are said to be\n\u003Cem\u003Efalsy\u003C/em\u003E.\u003C/p\u003E\n\u003Cp\u003EA value\u2019s truthiness matters, and depending on the context, it can be necessary or redundant to cast a value to boolean explicitly.\u003C/p\u003E\n\u003Ch2\u003EWhy is this an issue?\u003C/h2\u003E\n\u003Cp\u003EA boolean cast via double negation (\u003Ccode\u003E!!\u003C/code\u003E) or a \u003Ccode\u003EBoolean\u003C/code\u003E call is redundant when used as a condition, though. The condition\ncan be written without the extra cast and behave exactly the same.\u003C/p\u003E\n\u003Cp\u003EThe reason is that JavaScript uses type coercion and automatically converts values to booleans in a specific situation known as a boolean context.\nThe boolean context can be any conditional expression or statement.\u003C/p\u003E\n\u003Cp\u003EFor example, these \u003Ccode\u003Eif\u003C/code\u003E statements are equivalent:\u003C/p\u003E\n\u003Cpre\u003E\nif (!!foo) {\n    // ...\n}\n\nif (Boolean(foo)) {\n    // ...\n}\n\nif (foo) {\n    // ...\n}\n\u003C/pre\u003E\n\u003Ch3\u003EWhat is the potential impact?\u003C/h3\u003E\n\u003Cp\u003EA redundant boolean cast affects code readability. Not only the condition becomes more verbose but it also misleads the reader who might question\nthe intent behind the extra cast.\u003C/p\u003E\n\u003Cp\u003EThe more concise the condition, the more readable the code.\u003C/p\u003E\n\u003Ch2\u003EHow to fix it\u003C/h2\u003E\n\u003Cp\u003EThe fix for this issue is straightforward. One just needs to remove the extra boolean cast.\u003C/p\u003E\n\u003Ch3\u003ECode examples\u003C/h3\u003E\n\u003Ch4\u003ENoncompliant code example\u003C/h4\u003E\n\u003Cpre data-diff-id=\u00221\u0022 data-diff-type=\u0022noncompliant\u0022\u003E\nif (!!foo) {\n    // ...\n}\n\u003C/pre\u003E\n\u003Ch4\u003ECompliant solution\u003C/h4\u003E\n\u003Cpre data-diff-id=\u00221\u0022 data-diff-type=\u0022compliant\u0022\u003E\nif (foo) {\n    // ...\n}\n\u003C/pre\u003E\n\u003Ch4\u003ENoncompliant code example\u003C/h4\u003E\n\u003Cpre data-diff-id=\u00222\u0022 data-diff-type=\u0022noncompliant\u0022\u003E\nwhile (Boolean(foo)) {\n    // ...\n}\n\u003C/pre\u003E\n\u003Ch4\u003ECompliant solution\u003C/h4\u003E\n\u003Cpre data-diff-id=\u00222\u0022 data-diff-type=\u0022compliant\u0022\u003E\nwhile (foo) {\n    // ...\n}\n\u003C/pre\u003E\n\u003Ch2\u003EResources\u003C/h2\u003E\n\u003Ch3\u003EDocumentation\u003C/h3\u003E\n\u003Cul\u003E\n  \u003Cli\u003E \u003Ca href=\u0022https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Boolean#boolean_coercion\u0022\u003EMDN Boolean coercion\u003C/a\u003E\n  \u003C/li\u003E\n  \u003Cli\u003E \u003Ca href=\u0022https://developer.mozilla.org/en-US/docs/Glossary/Type_coercion\u0022\u003EMDN Type coercion\u003C/a\u003E \u003C/li\u003E\n  \u003Cli\u003E \u003Ca href=\u0022https://developer.mozilla.org/en-US/docs/Glossary/Truthy\u0022\u003EMDN Truthy\u003C/a\u003E \u003C/li\u003E\n  \u003Cli\u003E \u003Ca href=\u0022https://developer.mozilla.org/en-US/docs/Glossary/Falsy\u0022\u003EMDN Falsy\u003C/a\u003E \u003C/li\u003E\n\u003C/ul\u003E\n\u003Ch3\u003EArticles \u0026amp; blog posts\u003C/h3\u003E\n\u003Cul\u003E\n  \u003Cli\u003E \u003Ca href=\u0022https://blog.alexdevero.com/truthy-falsy-values-in-javascript/\u0022\u003EAlex Devero, How Truthy and Falsy Values in JavaScript Work\u003C/a\u003E \u003C/li\u003E\n\u003C/ul\u003E',
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
