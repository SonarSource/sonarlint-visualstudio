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
using Moq;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class StaticXamlStorageTests
    {
        [TestMethod]
        public void EducationPrinciplesHeader_IsLazy()
        {
            var translatorMock = new Mock<IRuleHelpXamlTranslator>();
            var testSubject = new StaticXamlStorage(translatorMock.Object);

            _  = testSubject.EducationPrinciplesHeader;
            _  = testSubject.EducationPrinciplesHeader;

            translatorMock.Verify(x => x.TranslateHtmlToXaml(StaticHtmlSnippets.EducationPrinciplesHeader), Times.Once);
        }

        [TestMethod]
        public void EducationPrinciplesHeader_DoesNotThrow()
        {
            var testSubject = new StaticXamlStorage(new RuleHelpXamlTranslator());

            Action act = () =>
            {
                _ = testSubject.EducationPrinciplesHeader;
            };

            act.Should().NotThrow();
        }

        [TestMethod]
        public void EducationPrinciplesDefenseInDepth_IsLazy()
        {
            var translatorMock = new Mock<IRuleHelpXamlTranslator>();
            var testSubject = new StaticXamlStorage(translatorMock.Object);

            _ = testSubject.EducationPrinciplesDefenseInDepth;
            _ = testSubject.EducationPrinciplesDefenseInDepth;

            translatorMock.Verify(x => x.TranslateHtmlToXaml(StaticHtmlSnippets.EducationPrinciplesDefenseInDepth), Times.Once);
        }

        [TestMethod]
        public void EducationPrinciplesDefenseInDepth_DoesNotThrow()
        {
            var testSubject = new StaticXamlStorage(new RuleHelpXamlTranslator());

            Action act = () =>
            {
                _ = testSubject.EducationPrinciplesDefenseInDepth;
            };

            act.Should().NotThrow();
        }

        [TestMethod]
        public void EducationPrinciplesNeverTrustUserInput_IsLazy()
        {
            var translatorMock = new Mock<IRuleHelpXamlTranslator>();
            var testSubject = new StaticXamlStorage(translatorMock.Object);

            _ = testSubject.EducationPrinciplesNeverTrustUserInput;
            _ = testSubject.EducationPrinciplesNeverTrustUserInput;

            translatorMock.Verify(x => x.TranslateHtmlToXaml(StaticHtmlSnippets.EducationPrinciplesNeverTrustUserInput), Times.Once);
        }

        [TestMethod]
        public void EducationPrinciplesNeverTrustUserInput_DoesNotThrow()
        {
            var testSubject = new StaticXamlStorage(new RuleHelpXamlTranslator());

            Action act = () =>
            {
                _ = testSubject.EducationPrinciplesNeverTrustUserInput;
            };

            act.Should().NotThrow();
        }

        [TestMethod]
        public void HowToFixItFallbackContext_IsLazy()
        {
            var translatorMock = new Mock<IRuleHelpXamlTranslator>();
            var testSubject = new StaticXamlStorage(translatorMock.Object);

            _ = testSubject.HowToFixItFallbackContext;
            _ = testSubject.HowToFixItFallbackContext;

            translatorMock.Verify(x => x.TranslateHtmlToXaml(StaticHtmlSnippets.HowToFixItFallbackContext), Times.Once);
        }

        [TestMethod]
        public void HowToFixItFallbackContext_DoesNotThrow()
        {
            var testSubject = new StaticXamlStorage(new RuleHelpXamlTranslator());

            Action act = () =>
            {
                _ = testSubject.HowToFixItFallbackContext;
            };

            act.Should().NotThrow();
        }
    }
}
