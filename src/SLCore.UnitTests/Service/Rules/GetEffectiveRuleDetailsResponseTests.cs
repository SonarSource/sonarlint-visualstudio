/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Collections.Generic;
using FluentAssertions.Equivalency;
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
    public void DeserializeSlcoreResponse_AsExpected()
    {
        const string slcoreResponse = """
                            {
                              "details": {
                                "description": {
                                  "introductionHtmlContent": "<p>This rule raises an issue when the code cognitive complexity of a function is above a certain threshold.</p>",
                                  "tabs": [
                                    {
                                      "title": "Why is this an issue?",
                                      "content": {
                                        "htmlContent": "<p>Cognitive Complexity is a measure of how hard it is to understand the control flow of a unit of code. Code with high cognitive complexity is hard\nto read, understand, test, and modify.</p>\n<p>As a rule of thumb, high cognitive complexity is a sign that the code should be refactored into smaller, easier-to-manage pieces.</p>\n<h3>Which syntax in code does impact cognitive complexity score?</h3>\n<p>Here are the core concepts:</p>\n<ul>\n  <li> <strong>Cognitive complexity is incremented each time the code breaks the normal linear reading flow.</strong><br> This concerns, for example:\n  Loop structures, Conditionals, Catches, Switches, Jumps to label and mixed operators in condition. </li>\n  <li> <strong>Each nesting level adds a malus to the breaking call.</strong><br> During code reading, the deeper you go through nested layers, the\n  harder it becomes to keep the context in mind. </li>\n  <li> <strong>Method calls are free</strong><br> A well-picked method name is a summary of multiple lines of code. A reader can first explore a\n  high-level view of what the code is performing then go deeper and deeper by looking at called functions content.<br> <em>Note:</em> This does not\n  apply to recursive calls, those will increment cognitive score. </li>\n</ul>\n<p>The method of computation is fully detailed in the pdf linked in the resources.</p>\n<h3>What is the potential impact?</h3>\n<p>Developers spend more time reading and understanding code than writing it. High cognitive complexity slows down changes and increases the cost of\nmaintenance.</p>"
                                      }
                                    },
                                    {
                                      "title": "How can I fix it?",
                                      "content": {
                                        "htmlContent": "<p>Reducing cognitive complexity can be challenging.<br> Here are a few suggestions:</p>\n<ul>\n  <li> <strong>Extract complex conditions in a new function.</strong><br> Mixed operators in condition will increase complexity. Extracting the\n  condition in a new function with an appropriate name will reduce cognitive load. </li>\n  <li> <strong>Break down large functions.</strong><br> Large functions can be hard to understand and maintain. If a function is doing too many\n  things, consider breaking it down into smaller, more manageable functions. Each function should have a single responsibility. </li>\n  <li> <strong>Avoid deep nesting by returning early.</strong><br> To avoid the nesting of conditions, process exceptional cases first and return\n  early. </li>\n  <li> <strong>Use null-safe operations (if available in the language).</strong><br> When available the <code>.?</code> or <code>??</code> operator\n  replaces multiple tests and simplifies the flow. </li>\n</ul>\n\n<p><strong>Extraction of a complex condition in a new function.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The code is using a complex condition and has a cognitive cost of 3.</p>\n<pre data-diff-id=\"1\" data-diff-type=\"noncompliant\">\nfunction calculateFinalPrice(user, cart) {\n  let total = calculateTotal(cart);\n  if (user.hasMembership                       // +1 (if)\n    &amp;&amp; user.orders &gt; 10                        // +1 (more than one condition)\n    &amp;&amp; user.accountActive\n    &amp;&amp; !user.hasDiscount\n    || user.orders === 1) {                    // +1 (change of operator in condition)\n      total = applyDiscount(user, total);\n  }\n  return total;\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>Even if the cognitive complexity of the whole program did not change, it is easier for a reader to understand the code of the\n<code>calculateFinalPrice</code> function, which now only has a cognitive cost of 1.</p>\n<pre data-diff-id=\"1\" data-diff-type=\"compliant\">\nfunction calculateFinalPrice(user, cart) {\n  let total = calculateTotal(cart);\n  if (isEligibleForDiscount(user)) {       // +1 (if)\n    total = applyDiscount(user, total);\n  }\n  return total;\n}\n\nfunction isEligibleForDiscount(user) {\n  return user.hasMembership\n    &amp;&amp; user.orders &gt; 10                     // +1 (more than one condition)\n    &amp;&amp; user.accountActive\n    &amp;&amp; !user.hasDiscount\n    || user.orders === 1                    // +1 (change of operator in condition)\n}\n</pre>\n<p><strong>Break down large functions.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>For example, consider a function that calculates the total price of a shopping cart, including sales tax and shipping.<br> <em>Note:</em> The code\nis simplified here, to illustrate the purpose. Please imagine there is more happening in the <code>for</code> loops.</p>\n<pre data-diff-id=\"3\" data-diff-type=\"noncompliant\">\nfunction calculateTotal(cart) {\n  let total = 0;\n  for (let i = 0; i &lt; cart.length; i++) {       // +1 (for)\n    total += cart[i].price;\n  }\n\n  // calculateSalesTax\n  for (let i = 0; i &lt; cart.length; i++) {       // +1 (for)\n    total += 0.2 * cart[i].price;\n  }\n\n  //calculateShipping\n  total += 5 * cart.length;\n\n  return total;\n}\n</pre>\n<p>This function could be refactored into smaller functions: The complexity is spread over multiple functions and the complex\n<code>calculateTotal</code> has now a complexity score of zero.</p>\n<h4>Compliant solution</h4>\n<pre data-diff-id=\"3\" data-diff-type=\"compliant\">\nfunction calculateTotal(cart) {\n  let total = calculateSubtotal(cart);\n  total += calculateSalesTax(cart);\n  total += calculateShipping(cart);\n  return total;\n}\n\nfunction calculateSubtotal(cart) {\n  let subTotal = 0;\n  for (const item of cart) {        // +1 (for)\n    subTotal += item.price;\n  }\n  return subTotal;\n}\n\nfunction calculateSalesTax(cart) {\n  let salesTax = 0;\n  for (const item of cart) {        // +1 (for)\n    salesTax += 0.2 * item.price;\n  }\n  return salesTax;\n}\n\nfunction calculateShipping(cart) {\n  return 5 * cart.length;\n}\n</pre>\n<p><strong>Avoid deep nesting by returning early.</strong></p>\n<h4>Noncompliant code example</h4>\n<p>The below code has a cognitive complexity of 6.</p>\n<pre data-diff-id=\"4\" data-diff-type=\"noncompliant\">\nfunction calculateDiscount(price, user) {\n  if (isEligibleForDiscount(user)) {  // +1 ( if )\n    if (user?.hasMembership) {        // +2 ( nested if )\n      return price * 0.9;\n  } else if (user?.orders === 1 ) {   // +1 ( else )\n          return price * 0.95;\n    } else {                          // +1 ( else )\n      return price;\n    }\n  } else {                            // +1 ( else )\n    return price;\n  }\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>Checking for the edge case first flattens the <code>if</code> statements and reduces the cognitive complexity to 3.</p>\n<pre data-diff-id=\"4\" data-diff-type=\"compliant\">\nfunction calculateDiscount(price, user) {\n    if (!isEligibleForDiscount(user)) {  // +1 ( if )\n      return price;\n    }\n    if (user?.hasMembership) {           // +1 ( if )\n      return price * 0.9;\n    }\n    if (user?.orders === 1) {            // +1 ( if )\n      return price * 0.95;\n    }\n    return price;\n}\n</pre>\n<p><strong>Use the optional chaining operator to access data.</strong></p>\n<p>In the below code, the cognitive complexity is increased due to the multiple checks required to access the manufacturer’s name. This can be\nsimplified using the optional chaining operator.</p>\n<h4>Noncompliant code example</h4>\n<pre data-diff-id=\"2\" data-diff-type=\"noncompliant\">\nlet manufacturerName = null;\n\nif (product &amp;&amp; product.details &amp;&amp; product.details.manufacturer) { // +1 (if) +1 (multiple condition)\n    manufacturerName = product.details.manufacturer.name;\n}\nif (manufacturerName) { // +1 (if)\n  console.log(manufacturerName);\n} else {\n  console.log('Manufacturer name not found');\n}\n</pre>\n<h4>Compliant solution</h4>\n<p>The optional chaining operator will return <code>undefined</code> if any reference in the chain is <code>undefined</code> or <code>null</code>,\navoiding multiple checks:</p>\n<pre data-diff-id=\"2\" data-diff-type=\"compliant\">\nlet manufacturerName = product?.details?.manufacturer?.name;\n\nif (manufacturerName) { // +1 (if)\n  console.log(manufacturerName);\n} else {\n  console.log('Manufacturer name not found');\n}\n</pre>\n<h3>Pitfalls</h3>\n<p>As this code is complex, ensure that you have unit tests that cover the code before refactoring.</p>"
                                      }
                                    },
                                    {
                                      "title": "More Info",
                                      "content": {
                                        "htmlContent": "<h3>Documentation</h3>\n<ul>\n  <li> Sonar - <a href=\"https://www.sonarsource.com/docs/CognitiveComplexity.pdf\">Cognitive Complexity</a> </li>\n</ul>\n<h3>Articles &amp; blog posts</h3>\n<ul>\n  <li> Sonar Blog - <a href=\"https://www.sonarsource.com/blog/5-clean-code-tips-for-reducing-cognitive-complexity/\">5 Clean Code Tips for Reducing\n  Cognitive Complexity</a> </li>\n</ul>BLABLA<br/><br/>bla bla<br/><br/><br/>dsfsfsd"
                                      }
                                    }
                                  ]
                                },
                                "params": [],
                                "key": "javascript:S3776",
                                "name": "Cognitive Complexity of functions should not be too high",
                                "severity": "CRITICAL",
                                "type": "CODE_SMELL",
                                "cleanCodeAttribute": "FOCUSED",
                                "cleanCodeAttributeCategory": "ADAPTABLE",
                                "defaultImpacts": [
                                  {
                                    "softwareQuality": "MAINTAINABILITY",
                                    "impactSeverity": "HIGH"
                                  }
                                ],
                                "language": "JS"
                              }
                            }
                            """;
        var ruleDetailsResponse = JsonConvert.DeserializeObject<GetEffectiveRuleDetailsResponse>(slcoreResponse);

        var expectedRuleDetails = new GetEffectiveRuleDetailsResponse(new EffectiveRuleDetailsDto(
            key:"javascript:S3776",
            name:"Cognitive Complexity of functions should not be too high", 
            severity:IssueSeverity.CRITICAL,
            type:RuleType.CODE_SMELL,
            cleanCodeAttribute:CleanCodeAttribute.FOCUSED,
            cleanCodeAttributeCategory:CleanCodeAttributeCategory.ADAPTABLE,
            defaultImpacts:new List<ImpactDto>
            {
                new(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.HIGH)
            },
            Language.JS,
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
                    .WithStrictOrdering()
                    .RespectingDeclaredTypes()
                    .Excluding((IMemberInfo info) => info.RuntimeType == typeof(string) && info.SelectedMemberPath.EndsWith(".content.Left.htmlContent")));
    }
}
