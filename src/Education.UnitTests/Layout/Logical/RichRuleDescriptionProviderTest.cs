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
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical;

[TestClass]
public class RichRuleDescriptionProviderTest
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<RichRuleDescriptionProvider, IRichRuleDescriptionProvider>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RichRuleDescriptionProvider>();
    }

    [TestMethod]
    public void Get_ReturnsCorrectStructure()
    {
        var richRuleDescriptionProvider = new RichRuleDescriptionProvider();
        var ruleInfoMock = new Mock<IRuleInfo>();
        ruleInfoMock.SetupGet(x => x.RichRuleDescriptionDto).Returns(
            new RuleSplitDescriptionDto("intro",
                new List<RuleDescriptionTabDto>
                {
                    new("tab 1",
                        Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(
                            new RuleNonContextualSectionDto("content"))),
                    new("tab 2",
                        Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateRight(
                            new RuleContextualSectionWithDefaultContextKeyDto("context2",
                                new List<RuleContextualSectionDto>
                                {
                                    new("context1content", "context1", "Context 1"),
                                    new("context2content", "context2", "Context 2")
                                }))),
                    new("tab 3",
                        Either<RuleNonContextualSectionDto, RuleContextualSectionWithDefaultContextKeyDto>.CreateLeft(
                            new RuleNonContextualSectionDto("content"))),
                }));
        
        
        richRuleDescriptionProvider.GetRichRuleDescriptionModel(ruleInfoMock.Object).Should().BeEquivalentTo(
            new RichRuleDescription("intro", new List<IRuleDescriptionTab>
            {
                new NonContextualRuleDescriptionTab("tab 1", "content"),
                new ContextualRuleDescriptionTab("tab 2", "context2", new List<ContextualRuleDescriptionTab.ContextContentTab>
                {
                    new("Context 1", "context1", "context1content"),
                    new("Context 2", "context2", "context2content")
                }),
                new NonContextualRuleDescriptionTab("tab 3", "content"),
            }));
    }
}
