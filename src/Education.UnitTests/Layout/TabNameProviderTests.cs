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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout
{
    [TestClass]
    public class TabNameProviderTests
    {
        [TestMethod]
        public void GetTabButtonName_ReturnsCorrectName()
        {
            TabNameProvider.GetTabButtonName("myg_roup", "veryfirsttab").Should().Be("myg_roup__veryfirsttab__Button");
        }

        [DataTestMethod]
        [DataRow(null, "xaxaxa")]
        [DataRow( "xaxaxa", null)]
        public void GetTabButtonName_NullArguments_Throws(string tabGroup, string tabName)
        {
            Action act = () =>  TabNameProvider.GetTabButtonName(tabGroup, tabName);

            act.Should().Throw<ArgumentNullException>();
        }

        [DataTestMethod]
        [DataRow("aB", "xax__axa")]
        [DataRow("xax__axa", "aB")]
        public void GetTabButtonName_ArgumentContainsSeparator_Throws(string tabGroup, string tabName)
        {
            Action act = () => TabNameProvider.GetTabButtonName(tabGroup, tabName);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void GetTabSectionName_ReturnsCorrectName()
        {
            TabNameProvider.GetTabSectionName("myg_roup", "veryfirsttab").Should().Be("myg_roup__veryfirsttab__Section");
        }

        [DataTestMethod]
        [DataRow(null, "xaxaxa")]
        [DataRow("xaxaxa", null)]
        public void GetTabSectionName_NullArguments_Throws(string tabGroup, string tabName)
        {
            Action act = () => TabNameProvider.GetTabSectionName(tabGroup, tabName);

            act.Should().Throw<ArgumentNullException>();
        }

        [DataTestMethod]
        [DataRow("aB", "xax__axa")]
        [DataRow("xax__axa", "aB")]
        public void GetTabSectionName_ArgumentContainsSeparator_Throws(string tabGroup, string tabName)
        {
            Action act = () => TabNameProvider.GetTabSectionName(tabGroup, tabName);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void GetTabIdentifier_SectionName_ReturnsCorrectTabGroupAndTabName()
        {
            var (tabGroup, tabName) = TabNameProvider.GetTabIdentifier("Myg_roup__myTab__Section");

            tabGroup.Should().Be("Myg_roup");
            tabName.Should().Be("myTab");
        }

        [TestMethod]
        public void GetTabIdentifier_ButtonName_ReturnsCorrectTabGroupAndTabName()
        {
            var (tabGroup, tabName) = TabNameProvider.GetTabIdentifier("Myg_roup__myTab__Button");

            tabGroup.Should().Be("Myg_roup");
            tabName.Should().Be("myTab");
        }

        [DataTestMethod]
        [DataRow("a__b__c__")]
        [DataRow("a__b__c__d")]
        [DataRow("a__b__c__d__e")]
        [DataRow("a__b_c")]
        public void GetTabIdentifier_IncorrectFormat_Throws(string name)
        {
            Action act = () => TabNameProvider.GetTabIdentifier(name);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void GetTabIdentifier_NullArgument_Throws()
        {
            Action act = () => TabNameProvider.GetTabIdentifier(null);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
