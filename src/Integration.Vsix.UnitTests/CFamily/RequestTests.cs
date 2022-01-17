/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class RequestTests
    {
        [TestMethod]
        public void WriteRequestDiagnostics_FileConfigIsSerialized()
        {
            var fileConfig = new FileConfig
            {
                AbsoluteFilePath = "abs", ForcedIncludeFiles = "xxx"
            };
            var expected = JsonConvert.SerializeObject(fileConfig);

            var testSubject = (IRequest)new Request
            {
                FileConfig = fileConfig,

                // Only the FileConfig should be serialized - other properties should be ignored
                File = "any file",
                PchFile = "any pchFile",
                Context = new RequestContext("junk", null, "file", "pchFile", new CFamilyAnalyzerOptions()),
                Flags = 123
            };

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                // Act
                testSubject.WriteRequestDiagnostics(writer);
            }

            var actual = sb.ToString();
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void EnvironmentVariables_ReturnsExpectedValue()
        {
            var testSubject = new Request();

            testSubject.EnvironmentVariables.Should().BeNull();
        }
    }
}
