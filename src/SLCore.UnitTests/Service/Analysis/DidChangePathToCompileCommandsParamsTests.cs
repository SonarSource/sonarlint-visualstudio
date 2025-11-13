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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Analysis;

[TestClass]
public class DidChangePathToCompileCommandsParamsTests
{
    [TestMethod]
    public void Serialize_WithPathToCompileCommands_AsExpected()
    {
        const string expected = """{"configurationScopeId":"scope1","pathToCompileCommands":"C:/some/path/compile_commands.json"}""";
        var testSubject = new DidChangePathToCompileCommandsParams("scope1", "C:/some/path/compile_commands.json");

        var serialized = JsonConvert.SerializeObject(testSubject);

        serialized.Should().Be(expected);
    }

    [TestMethod]
    public void Serialize_WithNullPathToCompileCommands_AsExpected()
    {
        const string expected = """{"configurationScopeId":"scope2","pathToCompileCommands":null}""";
        var testSubject = new DidChangePathToCompileCommandsParams("scope2", null);

        var serialized = JsonConvert.SerializeObject(testSubject);

        serialized.Should().Be(expected);
    }
}

