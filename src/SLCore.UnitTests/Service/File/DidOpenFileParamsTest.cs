﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.File;

[TestClass]
public class DidOpenFileParamsTest
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var testSubject = new DidOpenFileParams("some ConfigScope", new FileUri("file:///tmp/junit14012097140227905793/Foo.cs"));

        const string expectedString = """
                                      {
                                        "configurationScopeId": "some ConfigScope",
                                        "fileUri": "file:///tmp/junit14012097140227905793/Foo.cs"
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
