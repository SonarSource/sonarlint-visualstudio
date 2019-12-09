/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class LanguageHelperTests
    {
        [TestMethod]
        public void ToServerLanguage_NullOrKnown_ShouldBeNull()
        {
            // 1. Null
            LanguageHelper.ToServerLanguage(null).Should().BeNull();

            // 2. Unknown languages
            CheckMapping(new Language("foo", "bar"), null);
        }

        [TestMethod]
        public void ToServerLanguage_Known_ShouldBeCorrectlyMapped()
        {
            CheckMapping(Language.C, SonarQubeLanguage.C);
            CheckMapping(Language.Cpp, SonarQubeLanguage.Cpp);
            CheckMapping(Language.CSharp, SonarQubeLanguage.CSharp);
            CheckMapping(Language.VBNET, SonarQubeLanguage.VbNet);

            CheckMapping(new Language("VB", "Any name at all - isn't used in the mapping"), SonarQubeLanguage.VbNet);
        }

        private static void CheckMapping(Language language, SonarQubeLanguage expectedServerLanguage)
        {
            LanguageHelper.ToServerLanguage(language).Should().Be(expectedServerLanguage);
        }
    }
}
