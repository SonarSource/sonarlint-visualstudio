/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Linq.Expressions;
using System.Windows.Documents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Controls;

namespace SonarLint.VisualStudio.Education.UnitTests.Controls;

[TestClass]
public class RuleHelpUserControlViewModelTests
{
    private RuleHelpUserControlViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new RuleHelpUserControlViewModel();
    }

    [TestMethod]
    public void Ctor_DefaultState_IsShowPlaceholderIsTrue()
    {
        AssertOnlyPropertyIsTrue(vm => vm.IsShowPlaceholder);
    }

    [TestMethod]
    public void Ctor_DefaultState_DocumentIsNull()
    {
        testSubject.Document.Should().BeNull();
    }

    [TestMethod]
    public void Ctor_DefaultState_RuleIdIsNull()
    {
        testSubject.RuleId.Should().BeNull();
    }

    [TestMethod]
    public void ShowRuleDescription_SetsDocument()
    {
        var flowDocument = new FlowDocument();

        testSubject.ShowRuleDescription(flowDocument);

        testSubject.Document.Should().BeSameAs(flowDocument);
        AssertOnlyPropertyIsTrue(vm => vm.IsShowRuleDescription);
    }

    [TestMethod]
    public void ShowRuleDescription_SetsOtherBoolPropertiesToFalse()
    {
        testSubject.ShowCannotShowRuleDescription(new SonarCompositeRuleId("repo", "rule"));
        testSubject.ShowRuleDescriptionInBrowser(new SonarCompositeRuleId("repo", "rule"));

        testSubject.ShowRuleDescription(new FlowDocument());

        AssertOnlyPropertyIsTrue(vm => vm.IsShowRuleDescription);
    }

    [TestMethod]
    public void ShowRuleDescription_RaisesPropertyChanged()
    {
        var propertiesChanged = new List<string>();
        testSubject.PropertyChanged += (_, args) => propertiesChanged.Add(args.PropertyName);

        testSubject.ShowRuleDescription(new FlowDocument());

        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.Document));
        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.IsShowRuleDescription));
        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.IsShowPlaceholder));
    }

    [TestMethod]
    public void ShowCannotShowRuleDescription_SetsRuleId()
    {
        var ruleId = new SonarCompositeRuleId("repo", "rule");

        testSubject.ShowCannotShowRuleDescription(ruleId);

        testSubject.RuleId.Should().BeSameAs(ruleId);
        AssertOnlyPropertyIsTrue(vm => vm.IsShowCannotShowRuleDescription);
    }

    [TestMethod]
    public void ShowCannotShowRuleDescription_SetsOtherBoolPropertiesToFalse()
    {
        testSubject.ShowRuleDescription(new FlowDocument());
        testSubject.ShowRuleDescriptionInBrowser(new SonarCompositeRuleId("repo", "rule"));

        testSubject.ShowCannotShowRuleDescription(new SonarCompositeRuleId("repo", "rule"));

        AssertOnlyPropertyIsTrue(vm => vm.IsShowCannotShowRuleDescription);
    }

    [TestMethod]
    public void ShowCannotShowRuleDescription_RaisesPropertyChanged()
    {
        var propertiesChanged = new List<string>();
        testSubject.PropertyChanged += (_, args) => propertiesChanged.Add(args.PropertyName);

        testSubject.ShowCannotShowRuleDescription(new SonarCompositeRuleId("repo", "rule"));

        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.RuleId));
        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.IsShowCannotShowRuleDescription));
        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.IsShowPlaceholder));
    }

    [TestMethod]
    public void ShowRuleDescriptionInBrowser_SetsRuleId()
    {
        var ruleId = new SonarCompositeRuleId("repo", "rule");

        testSubject.ShowRuleDescriptionInBrowser(ruleId);

        testSubject.RuleId.Should().BeSameAs(ruleId);
        AssertOnlyPropertyIsTrue(vm => vm.IsShowRuleDescriptionInBrowser);
    }

    [TestMethod]
    public void ShowRuleDescriptionInBrowser_SetsOtherBoolPropertiesToFalse()
    {
        testSubject.ShowRuleDescription(new FlowDocument());
        testSubject.ShowCannotShowRuleDescription(new SonarCompositeRuleId("repo", "rule"));

        testSubject.ShowRuleDescriptionInBrowser(new SonarCompositeRuleId("repo", "rule"));

        AssertOnlyPropertyIsTrue(vm => vm.IsShowRuleDescriptionInBrowser);
    }

    [TestMethod]
    public void ShowRuleDescriptionInBrowser_RaisesPropertyChanged()
    {
        var propertiesChanged = new List<string>();
        testSubject.PropertyChanged += (_, args) => propertiesChanged.Add(args.PropertyName);

        testSubject.ShowRuleDescriptionInBrowser(new SonarCompositeRuleId("repo", "rule"));

        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.RuleId));
        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.IsShowRuleDescriptionInBrowser));
        propertiesChanged.Should().Contain(nameof(RuleHelpUserControlViewModel.IsShowPlaceholder));
    }

    private void AssertOnlyPropertyIsTrue(Expression<Func<RuleHelpUserControlViewModel, bool>> expectedTruePropertySelector)
    {
        var expectedPropertyName = ((MemberExpression)expectedTruePropertySelector.Body).Member.Name;

        var allBoolProperties = new (string name, bool value)[]
        {
            (nameof(RuleHelpUserControlViewModel.IsShowPlaceholder), testSubject.IsShowPlaceholder),
            (nameof(RuleHelpUserControlViewModel.IsShowRuleDescription), testSubject.IsShowRuleDescription),
            (nameof(RuleHelpUserControlViewModel.IsShowCannotShowRuleDescription), testSubject.IsShowCannotShowRuleDescription),
            (nameof(RuleHelpUserControlViewModel.IsShowRuleDescriptionInBrowser), testSubject.IsShowRuleDescriptionInBrowser)
        };

        foreach (var (name, value) in allBoolProperties)
        {
            var expectedValue = name == expectedPropertyName;
            value.Should().Be(expectedValue, $"property {name} should be {expectedValue}");
        }
    }
}
