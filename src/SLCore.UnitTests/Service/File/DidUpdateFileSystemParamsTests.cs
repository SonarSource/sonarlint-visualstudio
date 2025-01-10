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
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.File;

[TestClass]
public class DidUpdateFileSystemParamsTests
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var removedFiles = new List<FileUri>
        {
            new("file:///tmp/junit14012097140227905793/Foo.cs"),
            new("file:///tmp/junit14012097140227905793/Bar.cs")
        };

        var addedFiles = new List<ClientFileDto>
        {
            new(new FileUri("file:///c:/Users/test/project1/Baz.cs"), "Baz.cs", "CONFIG_SCOPE_ID", false,
                "utf8", "C:\\Users\\test\\project1", "CONTENT")
        };

        var changedFiles = new List<ClientFileDto>
        {
            new(new FileUri("file:///c:/Users/test/project2/ABOBA.cs"), "ABOBA.cs", "CONFIG_SCOPE_ID", true,
                "utf16", "C:\\Users\\test\\project2", "CONTENT2")
        };

        var testSubject = new DidUpdateFileSystemParams(removedFiles, addedFiles, changedFiles);

        const string expectedString = """
                                      {
                                        "removedFiles": [
                                          "file:///tmp/junit14012097140227905793/Foo.cs",
                                          "file:///tmp/junit14012097140227905793/Bar.cs"
                                        ],
                                        "addedFiles": [
                                          {
                                            "uri": "file:///c:/Users/test/project1/Baz.cs",
                                            "ideRelativePath": "Baz.cs",
                                            "configScopeId": "CONFIG_SCOPE_ID",
                                            "isTest": false,
                                            "charset": "utf8",
                                            "fsPath": "C:\\Users\\test\\project1",
                                            "content": "CONTENT",
                                            "isUserDefined": true
                                          }
                                        ],
                                        "changedFiles": [
                                          {
                                            "uri": "file:///c:/Users/test/project2/ABOBA.cs",
                                            "ideRelativePath": "ABOBA.cs",
                                            "configScopeId": "CONFIG_SCOPE_ID",
                                            "isTest": true,
                                            "charset": "utf16",
                                            "fsPath": "C:\\Users\\test\\project2",
                                            "content": "CONTENT2",
                                            "isUserDefined": true
                                          }
                                        ]
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
