/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Files;

[TestClass]
public class ListFilesResponseTests
{
    [TestMethod]
    public void SerializeCorrectly()
    {
        var file1 = new ClientFileDto(new("C:\\Code\\Solution\\Project\\file1.cs"), "Project\\file1.cs", "configScope", false, "UTF-8", "C:\\Code\\Solution\\Project\\file1.cs", "some content");
        var file2 = new ClientFileDto(new("C:\\Code\\Solution\\Project\\file 2.cs"), "Project\\file 2.cs", "configScope", false, "UTF-8", "C:\\Code\\Solution\\Project\\file 2.cs", null);

        var response = new ListFilesResponse(new[] { file1, file2 });

        var responseString = JsonConvert.SerializeObject(response, Formatting.Indented);

        var expectedString = """
                             {
                               "files": [
                                 {
                                   "uri": "file:///C:/Code/Solution/Project/file1.cs",
                                   "ideRelativePath": "Project\\file1.cs",
                                   "configScopeId": "configScope",
                                   "isTest": false,
                                   "charset": "UTF-8",
                                   "fsPath": "C:\\Code\\Solution\\Project\\file1.cs",
                                   "content": "some content",
                                   "isUserDefined": true
                                 },
                                 {
                                   "uri": "file:///C:/Code/Solution/Project/file%202.cs",
                                   "ideRelativePath": "Project\\file 2.cs",
                                   "configScopeId": "configScope",
                                   "isTest": false,
                                   "charset": "UTF-8",
                                   "fsPath": "C:\\Code\\Solution\\Project\\file 2.cs",
                                   "content": null,
                                   "isUserDefined": true
                                 }
                               ]
                             }
                             """;

        responseString.Should().Be(expectedString);
    }
}
