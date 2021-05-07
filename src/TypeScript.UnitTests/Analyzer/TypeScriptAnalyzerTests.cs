///*
// * SonarLint for Visual Studio
// * Copyright (C) 2016-2021 SonarSource SA
// * mailto:info AT sonarsource DOT com
// *
// * This program is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 3 of the License, or (at your option) any later version.
// *
// * This program is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public License
// * along with this program; if not, write to the Free Software Foundation,
// * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// */

//using FluentAssertions;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using SonarLint.VisualStudio.Core.Analysis;
//using SonarLint.VisualStudio.Integration.UnitTests;
//using SonarLint.VisualStudio.TypeScript.Analyzer;

//namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
//{
//    [TestClass]
//    public class TypeScriptAnalyzerTests
//    {
//        [TestMethod]
//        public void MefCtor_CheckIsExported()
//        {
//            MefTestHelpers.CheckTypeCanBeImported<TypeScriptAnalyzer, IAnalyzer>(null, null);
//        }

//        [TestMethod]
//        public void IsAnalysisSupported_NotTypeScript_False()
//        {
//            var testSubject = CreateTestSubject();

//            var languages = new[] { AnalysisLanguage.CFamily, AnalysisLanguage.Javascript };
//            var result = testSubject.IsAnalysisSupported(languages);

//            result.Should().BeFalse();
//        }

//        [TestMethod]
//        public void IsAnalysisSupported_HasTypeScript_True()
//        {
//            var testSubject = CreateTestSubject();

//            var languages = new[] { AnalysisLanguage.CFamily, AnalysisLanguage.TypeScript };
//            var result = testSubject.IsAnalysisSupported(languages);

//            result.Should().BeTrue();
//        }

//        private TypeScriptAnalyzer CreateTestSubject()
//        {
//            return new TypeScriptAnalyzer();
//        }
//    }
//}
