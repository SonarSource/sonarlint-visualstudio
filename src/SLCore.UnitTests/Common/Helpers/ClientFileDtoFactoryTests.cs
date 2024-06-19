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

using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class ClientFileDtoFactoryTests
{
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ClientFileDtoFactory>();
    }
    
    [DataTestMethod]
    [DataRow("C:\\Code\\Project\\File1.js", "C:\\", "file:///C:/Code/Project/File1.js", "C:\\Code\\Project\\File1.js", "Code\\Project\\File1.js")]
    [DataRow("C:\\привет\\project\\file1.js", "C:\\привет\\", "file:///C:/привет/project/file1.js", "C:\\привет\\project\\file1.js", "project\\file1.js")] // supports localized
    [DataRow("\\\\servername\\work\\project\\file1.js", "\\\\servername\\work\\", "file://servername/work/project/file1.js", "\\\\servername\\work\\project\\file1.js", "project\\file1.js")] // supports UNC
    [DataRow("C:\\Code\\My Project\\My Favorite File2.js", "C:\\", "file:///C:/Code/My%20Project/My%20Favorite%20File2.js", "C:\\Code\\My Project\\My Favorite File2.js", "Code\\My Project\\My Favorite File2.js")] // supports whitespaces
    public void Create_ConstructsValidDto(string filePath, string rootPath, string expectedUri, string expectedFsPath, string expectedIdeRelativePath)
    {
        var testSubject = new ClientFileDtoFactory();

        var result = testSubject.Create(filePath, "CONFIG_SCOPE_ID", rootPath);
        
        ValidateDto(result, expectedUri, expectedFsPath, expectedIdeRelativePath);
    }

    private static void ValidateDto(ClientFileDto dto, string uri, string fsPath, string ideRelativePath)
    {
        dto.uri.ToString().Should().Be(uri);
        dto.uri.LocalPath.Should().Be(dto.fsPath);
        dto.fsPath.Should().Be(fsPath);
        dto.ideRelativePath.Should().Be(ideRelativePath);
        dto.configScopeId.Should().Be("CONFIG_SCOPE_ID");
        dto.isTest.Should().BeNull();
        dto.charset.Should().Be("utf-8");
        dto.content.Should().BeNull();
        
    }
}
