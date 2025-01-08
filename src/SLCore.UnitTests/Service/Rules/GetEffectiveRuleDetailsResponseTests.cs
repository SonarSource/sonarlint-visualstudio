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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Rules;

[TestClass]
public class GetEffectiveRuleDetailsResponseTests
{
    [TestMethod]
    public void DeserializeSLCoreResponse_Standard_Mode_AsExpected()
    {
        const string slcoreResponse = """
                            {
                              "details": {
                                "key": "cpp:S3776",
                                "name": "Cognitive Complexity of functions should not be too high",
                                "severityDetails": {
                                  "severity": "CRITICAL",
                                  "type": "CODE_SMELL"
                                },
                                "language": "CPP",
                                "vulnerabilityProbability": null,
                                "description": {
                                  "introductionHtmlContent": "<p>This rule raises an issue when the code cognitive complexity of a function is above a certain threshold.</p>",
                                  "tabs": [
                                    {
                                      "title": "Why is this an issue?",
                                      "content": {
                                        "htmlContent": "<p>Cognitive Complexity is a measure of how hard it is to understand the control flow of a unit of code. Code with high cognitive complexity is hard\nto read, understand, test, and modify.</p>\n<p>As a rule of thumb, high cognitive complexity is a sign that the code should be refactored into smaller, easier-to-manage pieces.</p>\n<h3>Which syntax in code does impact cognitive complexity score?</h3>\n<p>Here are the core concepts:</p>\n<ul>\n  <li> <strong>Cognitive complexity is incremented each time the code breaks the normal linear reading flow.</strong><br> This concerns, for example,\n  loop structures, conditionals, catches, switches, jumps to labels, and conditions mixing multiple operators. </li>\n  <li> <strong>Each nesting level increases complexity.</strong><br> During code reading, the deeper you go through nested layers, the harder it\n  becomes to keep the context in mind. </li>\n  <li> <strong>Method calls are free</strong><br> A well-picked method name is a summary of multiple lines of code. A reader can first explore a\n  high-level view of what the code is performing then go deeper and deeper by looking at called functions content.<br> <em>Note:</em> This does not\n  apply to recursive calls, those will increment cognitive score. </li>\n</ul>\n<p>The method of computation is fully detailed in the pdf linked in the resources.</p>\n<h3>What is the potential impact?</h3>\n<p>Developers spend more time reading and understanding code than writing it. High cognitive complexity slows down changes and increases the cost of\nmaintenance.</p>"
                                      }
                                    },
                                    {
                                      "title": "How can I fix it?",
                                      "content": {
                                        "htmlContent": "<p>Reducing cognitive complexity can be challenging.<br> Here are a few suggestions:</p>\n<ul>\n  <li> <strong>Extract complex conditions in a new function.</strong><br> Mixed operators in condition will increase complexity. Extracting the\n  condition in a new function with an appropriate name will reduce cognitive load. </li>\n  <li> <strong>Break down large functions.</strong><br> Large functions can be hard to understand and maintain. If a function is doing too many\n  things, consider breaking it down into smaller, more manageable functions. Each function should have a single responsibility. </li>\n  <li> <strong>Avoid deep nesting by returning early.</strong><br> To avoid the nesting of conditions, process exceptional cases first and return\n  early. </li>\n</ul>\n\n<p><strong>Extraction of a complex condition in a new function.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The code is using a complex condition and has a cognitive cost of 3.</p>\n<pre data-diff-id=\"1\" data-diff-type=\"noncompliant\">\nvoid processEligibleUser(User user) {\n  if ((user.isActive() &amp;&amp; user.hasProfile()) // +1 (if) +1 (multiple conditions)\n     || (user.getAge() &gt; 18)) {              // +1 (mixing operators)\n    // process the user\n  }\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>Even if the cognitive complexity of the whole program did not change, it is easier for a reader to understand the code of the\n<code>processEligibleUser</code> function, which now only has a cognitive score of 1.</p>\n<pre data-diff-id=\"1\" data-diff-type=\"compliant\">\nvoid processEligibleUser(User user) {\n  if (isEligibleUser(user)) {  // +1 (if)\n    // process the user\n  }\n}\n\nbool isEligibleUser(User user) {\n  return (user.isActive() &amp;&amp; user.hasProfile()) // +1 (multiple conditions)\n      || (user.getAge() &gt; 18));                 // +1 (mixing operators)\n}\n</pre>\n<p><strong>Break down large functions.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The code is simplified here to illustrate the purpose. Please imagine there is more happening in the process.<br> The overall complexity of\n<code>processUser</code> is 8.</p>\n<pre data-diff-id=\"3\" data-diff-type=\"noncompliant\">\nvoid processUser(User user) {\n  if (user.isActive()) {      // +1 (if)\n    if (user.hasProfile()) {  // +1 (if) +1 (nested)\n      // process active user with profile\n    } else {                  // +1 (else)\n      // process active user without profile\n    }\n  } else {                    // +1 (else)\n    if (user.hasProfile()) {  // +1 (if) +1 (nested)\n      // process inactive user with profile\n    } else {                  // +1 (else)\n      // process inactive user without profile\n    }\n  }\n}\n</pre>\n<p>This function could be refactored into smaller functions: The complexity is spread over multiple functions, and the breaks in flow are no longer\nnested.<br> The <code>processUser</code> now has a complexity score of two.</p>\n<h4>Compliant solution</h4>\n<pre data-diff-id=\"3\" data-diff-type=\"compliant\">\nvoid processUser(User user) {\n  if (user.isActive()) {      // +1 (if)\n    processActiveUser(user);\n  } else {                    // +1 (else)\n    processInactiveUser(user);\n  }\n}\n\nvoid processActiveUser(User user) {\n  if (user.hasProfile()) {    // +1 (if)\n      // process active user with profile\n  } else {                    // +1 (else)\n      // process active user without profile\n  }\n}\n\nvoid processInactiveUser(User user) {\n  if (user.hasProfile()) {    // +1 (if)\n    // process inactive user with profile\n  } else {                    // +1 (else)\n    // process inactive user without profile\n  }\n}\n</pre>\n<p><strong>Avoid deep nesting by returning early.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The below code has a cognitive complexity of 3.</p>\n<pre data-diff-id=\"4\" data-diff-type=\"noncompliant\">\nvoid checkUser(User user) {\n  if (user.isActive()) {     // +1 (if)\n    if (user.hasProfile()) { // +1 (if) +1 (nested)\n      // do something\n    }\n  }\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>Checking for the edge case first flattens the <code>if</code> statements and reduces the cognitive complexity to 2.</p>\n<pre data-diff-id=\"4\" data-diff-type=\"compliant\">\nvoid checkUser(User user) {\n  if (!user.isActive()) {\n    return;\n  }\n  if (!user.hasProfile()) {\n    return;\n  }\n  // do something\n}\n</pre>\n<h3>Pitfalls</h3>\n<p>As this code is complex, ensure that you have unit tests that cover the code before refactoring.</p>"
                                      }
                                    },
                                    {
                                      "title": "More Info",
                                      "content": {
                                        "htmlContent": "<h3>Documentation</h3>\n<ul>\n  <li> Sonar - <a href=\"https://www.sonarsource.com/docs/CognitiveComplexity.pdf\">Cognitive Complexity</a> </li>\n</ul>\n<h3>Articles &amp; blog posts</h3>\n<ul>\n  <li> Sonar Blog - <a href=\"https://www.sonarsource.com/blog/5-clean-code-tips-for-reducing-cognitive-complexity/\">5 Clean Code Tips for Reducing\n  Cognitive Complexity</a> </li>\n</ul>"
                                      }
                                    }
                                  ]
                                },
                                "params": []
                              }
                            }
                            """;
        var ruleDetailsResponse = JsonConvert.DeserializeObject<GetEffectiveRuleDetailsResponse>(slcoreResponse);

        var expectedRuleDetails = new GetEffectiveRuleDetailsResponse(new EffectiveRuleDetailsDto(
            key:"cpp:S3776",
            name:"Cognitive Complexity of functions should not be too high",
            Language.CPP,
            new StandardModeDetails(IssueSeverity.CRITICAL, RuleType.CODE_SMELL),
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(
                new RuleSplitDescriptionDto(
                    "<p>This rule raises an issue when the code cognitive complexity of a function is above a certain threshold.</p>",
                    new List<RuleDescriptionTabDto>
                    {
                        new("Why is this an issue?", Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(new RuleNonContextualSectionDto(""))),
                        new("How can I fix it?", Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(new RuleNonContextualSectionDto(""))),
                        new("More Info", Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(new RuleNonContextualSectionDto("")))
                    })),
            new List<EffectiveRuleParamDto>()));

        ruleDetailsResponse
            .Should()
            .BeEquivalentTo(expectedRuleDetails,
                options => options
                    .ComparingByMembers<GetEffectiveRuleDetailsResponse>()
                    .ComparingByMembers<EffectiveRuleDetailsDto>()
                    .ComparingByMembers<StandardModeDetails>()
                    .WithStrictOrdering()
                    .RespectingDeclaredTypes()
                    .Excluding(info => info.RuntimeType == typeof(string) && info.SelectedMemberPath.EndsWith(".content.Left.htmlContent")));
    }

    [TestMethod]
    public void DeserializeSLCoreResponse_MQR_Mode_AsExpected()
    {
        const string slcoreResponse = """
                            {
                              "details": {
                                "key": "cpp:S3776",
                                "name": "Cognitive Complexity of functions should not be too high",
                                "severityDetails": {
                                  "cleanCodeAttribute": "FOCUSED",
                                  "impacts": [
                                    {
                                      "softwareQuality": "MAINTAINABILITY",
                                      "impactSeverity": "HIGH"
                                    }
                                  ]
                                },
                                "language": "CPP",
                                "vulnerabilityProbability": null,
                                "description": {
                                  "introductionHtmlContent": "<p>This rule raises an issue when the code cognitive complexity of a function is above a certain threshold.</p>",
                                  "tabs": [
                                    {
                                      "title": "Why is this an issue?",
                                      "content": {
                                        "htmlContent": "<p>Cognitive Complexity is a measure of how hard it is to understand the control flow of a unit of code. Code with high cognitive complexity is hard\nto read, understand, test, and modify.</p>\n<p>As a rule of thumb, high cognitive complexity is a sign that the code should be refactored into smaller, easier-to-manage pieces.</p>\n<h3>Which syntax in code does impact cognitive complexity score?</h3>\n<p>Here are the core concepts:</p>\n<ul>\n  <li> <strong>Cognitive complexity is incremented each time the code breaks the normal linear reading flow.</strong><br> This concerns, for example,\n  loop structures, conditionals, catches, switches, jumps to labels, and conditions mixing multiple operators. </li>\n  <li> <strong>Each nesting level increases complexity.</strong><br> During code reading, the deeper you go through nested layers, the harder it\n  becomes to keep the context in mind. </li>\n  <li> <strong>Method calls are free</strong><br> A well-picked method name is a summary of multiple lines of code. A reader can first explore a\n  high-level view of what the code is performing then go deeper and deeper by looking at called functions content.<br> <em>Note:</em> This does not\n  apply to recursive calls, those will increment cognitive score. </li>\n</ul>\n<p>The method of computation is fully detailed in the pdf linked in the resources.</p>\n<h3>What is the potential impact?</h3>\n<p>Developers spend more time reading and understanding code than writing it. High cognitive complexity slows down changes and increases the cost of\nmaintenance.</p>"
                                      }
                                    },
                                    {
                                      "title": "How can I fix it?",
                                      "content": {
                                        "htmlContent": "<p>Reducing cognitive complexity can be challenging.<br> Here are a few suggestions:</p>\n<ul>\n  <li> <strong>Extract complex conditions in a new function.</strong><br> Mixed operators in condition will increase complexity. Extracting the\n  condition in a new function with an appropriate name will reduce cognitive load. </li>\n  <li> <strong>Break down large functions.</strong><br> Large functions can be hard to understand and maintain. If a function is doing too many\n  things, consider breaking it down into smaller, more manageable functions. Each function should have a single responsibility. </li>\n  <li> <strong>Avoid deep nesting by returning early.</strong><br> To avoid the nesting of conditions, process exceptional cases first and return\n  early. </li>\n</ul>\n\n<p><strong>Extraction of a complex condition in a new function.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The code is using a complex condition and has a cognitive cost of 3.</p>\n<pre data-diff-id=\"1\" data-diff-type=\"noncompliant\">\nvoid processEligibleUser(User user) {\n  if ((user.isActive() &amp;&amp; user.hasProfile()) // +1 (if) +1 (multiple conditions)\n     || (user.getAge() &gt; 18)) {              // +1 (mixing operators)\n    // process the user\n  }\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>Even if the cognitive complexity of the whole program did not change, it is easier for a reader to understand the code of the\n<code>processEligibleUser</code> function, which now only has a cognitive score of 1.</p>\n<pre data-diff-id=\"1\" data-diff-type=\"compliant\">\nvoid processEligibleUser(User user) {\n  if (isEligibleUser(user)) {  // +1 (if)\n    // process the user\n  }\n}\n\nbool isEligibleUser(User user) {\n  return (user.isActive() &amp;&amp; user.hasProfile()) // +1 (multiple conditions)\n      || (user.getAge() &gt; 18));                 // +1 (mixing operators)\n}\n</pre>\n<p><strong>Break down large functions.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The code is simplified here to illustrate the purpose. Please imagine there is more happening in the process.<br> The overall complexity of\n<code>processUser</code> is 8.</p>\n<pre data-diff-id=\"3\" data-diff-type=\"noncompliant\">\nvoid processUser(User user) {\n  if (user.isActive()) {      // +1 (if)\n    if (user.hasProfile()) {  // +1 (if) +1 (nested)\n      // process active user with profile\n    } else {                  // +1 (else)\n      // process active user without profile\n    }\n  } else {                    // +1 (else)\n    if (user.hasProfile()) {  // +1 (if) +1 (nested)\n      // process inactive user with profile\n    } else {                  // +1 (else)\n      // process inactive user without profile\n    }\n  }\n}\n</pre>\n<p>This function could be refactored into smaller functions: The complexity is spread over multiple functions, and the breaks in flow are no longer\nnested.<br> The <code>processUser</code> now has a complexity score of two.</p>\n<h4>Compliant solution</h4>\n<pre data-diff-id=\"3\" data-diff-type=\"compliant\">\nvoid processUser(User user) {\n  if (user.isActive()) {      // +1 (if)\n    processActiveUser(user);\n  } else {                    // +1 (else)\n    processInactiveUser(user);\n  }\n}\n\nvoid processActiveUser(User user) {\n  if (user.hasProfile()) {    // +1 (if)\n      // process active user with profile\n  } else {                    // +1 (else)\n      // process active user without profile\n  }\n}\n\nvoid processInactiveUser(User user) {\n  if (user.hasProfile()) {    // +1 (if)\n    // process inactive user with profile\n  } else {                    // +1 (else)\n    // process inactive user without profile\n  }\n}\n</pre>\n<p><strong>Avoid deep nesting by returning early.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The below code has a cognitive complexity of 3.</p>\n<pre data-diff-id=\"4\" data-diff-type=\"noncompliant\">\nvoid checkUser(User user) {\n  if (user.isActive()) {     // +1 (if)\n    if (user.hasProfile()) { // +1 (if) +1 (nested)\n      // do something\n    }\n  }\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>Checking for the edge case first flattens the <code>if</code> statements and reduces the cognitive complexity to 2.</p>\n<pre data-diff-id=\"4\" data-diff-type=\"compliant\">\nvoid checkUser(User user) {\n  if (!user.isActive()) {\n    return;\n  }\n  if (!user.hasProfile()) {\n    return;\n  }\n  // do something\n}\n</pre>\n<h3>Pitfalls</h3>\n<p>As this code is complex, ensure that you have unit tests that cover the code before refactoring.</p>"
                                      }
                                    },
                                    {
                                      "title": "More Info",
                                      "content": {
                                        "htmlContent": "<h3>Documentation</h3>\n<ul>\n  <li> Sonar - <a href=\"https://www.sonarsource.com/docs/CognitiveComplexity.pdf\">Cognitive Complexity</a> </li>\n</ul>\n<h3>Articles &amp; blog posts</h3>\n<ul>\n  <li> Sonar Blog - <a href=\"https://www.sonarsource.com/blog/5-clean-code-tips-for-reducing-cognitive-complexity/\">5 Clean Code Tips for Reducing\n  Cognitive Complexity</a> </li>\n</ul>"
                                      }
                                    }
                                  ]
                                },
                                "params": []
                              }
                            }
                            """;
        var ruleDetailsResponse = JsonConvert.DeserializeObject<GetEffectiveRuleDetailsResponse>(slcoreResponse);

        var expectedRuleDetails = new GetEffectiveRuleDetailsResponse(new EffectiveRuleDetailsDto(
            key:"cpp:S3776",
            name:"Cognitive Complexity of functions should not be too high",
            Language.CPP,
            new MQRModeDetails(CleanCodeAttribute.FOCUSED, [new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.HIGH)]),
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(
                new RuleSplitDescriptionDto(
                    "<p>This rule raises an issue when the code cognitive complexity of a function is above a certain threshold.</p>",
                    new List<RuleDescriptionTabDto>
                    {
                        new("Why is this an issue?", Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(new RuleNonContextualSectionDto(""))),
                        new("How can I fix it?", Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(new RuleNonContextualSectionDto(""))),
                        new("More Info", Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(new RuleNonContextualSectionDto("")))
                    })),
            new List<EffectiveRuleParamDto>()));

        ruleDetailsResponse
            .Should()
            .BeEquivalentTo(expectedRuleDetails,
                options => options
                    .ComparingByMembers<GetEffectiveRuleDetailsResponse>()
                    .ComparingByMembers<EffectiveRuleDetailsDto>()
                    .ComparingByMembers<MQRModeDetails>()
                    .WithStrictOrdering()
                    .RespectingDeclaredTypes()
                    .Excluding(info => info.RuntimeType == typeof(string) && info.SelectedMemberPath.EndsWith(".content.Left.htmlContent")));
    }
}
