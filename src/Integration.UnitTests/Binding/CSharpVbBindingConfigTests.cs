/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.Binding;
using RuleSet = Microsoft.VisualStudio.CodeAnalysis.RuleSets.RuleSet;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class CSharpVbBindingConfigTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            var ruleSet = new FilePathAndContent<RuleSet>("dummy", new RuleSet("dummy"));
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>("dummy", new SonarLintConfiguration());

            Action act = () => new CSharpVBBindingConfig(null, additionalFile);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ruleset");

             act = () => new CSharpVBBindingConfig(ruleSet, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("additionalFile");

            act = () => new CSharpVBBindingConfig(ruleSet, additionalFile, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void GetSolutionLevelFilePaths_ReturnFilePaths()
        {
            var ruleSet = new FilePathAndContent<RuleSet>("ruleset dummy", new RuleSet("dummy"));
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>("additional file dummy", new SonarLintConfiguration());

            var testSubject = new CSharpVBBindingConfig(ruleSet, additionalFile);
            testSubject.SolutionLevelFilePaths.Count().Should().Be(2);
            testSubject.SolutionLevelFilePaths.First().Should().Be(ruleSet.Path);
            testSubject.SolutionLevelFilePaths.Last().Should().Be(additionalFile.Path);
        }

        [TestMethod]
        public void Save_FilesSaved()
        {
            // We can't mock the RuleSet class so we're testing Save by actually
            // writing to disk.
            // Arrange
            var testDir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.CreateDirectory(testDir);

            var rulesetFullPath = Path.Combine(testDir, "savedRuleSet.txt");
            var additionalFileFullPath = Path.Combine(testDir, "additionalFile.txt");

            var ruleSet = new FilePathAndContent<RuleSet>(rulesetFullPath, new RuleSet("dummy"));
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>(additionalFileFullPath, new SonarLintConfiguration());

            var testSubject = new CSharpVBBindingConfig(ruleSet, additionalFile);

            // Act
            testSubject.Save();

            // Assert
            File.Exists(rulesetFullPath).Should().BeTrue();
            File.Exists(additionalFileFullPath).Should().BeTrue();

            var savedAdditionalFile = File.ReadAllText(additionalFileFullPath);
            savedAdditionalFile.Should().Be(@"<?xml version=""1.0"" encoding=""utf-8""?>
<AnalysisInput xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Settings />
  <Rules />
</AnalysisInput>");
        }
    }
}
