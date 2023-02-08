﻿/*
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class ShowRuleInBrowserServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ShowRuleInBrowserService, IShowRuleInBrowser>(
                MefTestHelpers.CreateExport<IBrowserService>());
        }

        [TestMethod]
        public void ShowRuleDescription_BrowserOpened()
        {
            var ruleId = new SonarCompositeRuleId("xxx","yyy");
            const string ruleUrl = "some url";

            var helpLinkProvider = new Mock<IRuleHelpLinkProvider>();
            helpLinkProvider.Setup(x => x.GetHelpLink("xxx:yyy")).Returns(ruleUrl);

            var browserService = new Mock<IBrowserService>();

            var testSubject = CreateTestSubject(browserService: browserService.Object, helpLinkProvider: helpLinkProvider.Object);

            testSubject.ShowRuleDescription(ruleId);

            browserService.Verify(x => x.Navigate(ruleUrl), Times.Once());
            browserService.VerifyNoOtherCalls();
        }

        private ShowRuleInBrowserService CreateTestSubject(IBrowserService browserService = null,
            IRuleHelpLinkProvider helpLinkProvider = null)
        {
            browserService ??= Mock.Of<IBrowserService>();
            helpLinkProvider ??= Mock.Of<IRuleHelpLinkProvider>();

            return new ShowRuleInBrowserService(browserService, helpLinkProvider);
        }
    }
}
