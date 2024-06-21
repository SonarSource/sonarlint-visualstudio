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
using SonarLint.VisualStudio.SLCore.Common.Models;
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
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\", @"user\projectA\directoryA\file.cs")]
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\user\projectA\", @"directoryA\file.cs")]
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\user\projectA\directoryA\", @"file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\", @"projectA\directoryA\file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\projectA\", @"directoryA\file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\projectA\directoryA\", @"file.cs")]
    public void Create_CalculatesCorrectRelativePath(string filePath, string rootPath, string expectedRelativePath)
    {
        var testSubject = new ClientFileDtoFactory();

        var result = testSubject.Create(filePath, "CONFIG_SCOPE_ID", rootPath);

        result.ideRelativePath.Should().BeEquivalentTo(expectedRelativePath);
    }
    
    [TestMethod]
    public void Create_ConstructsValidDto()
    {
        var testSubject = new ClientFileDtoFactory();

        var result = testSubject.Create(@"C:\Code\Project\File1.js", "CONFIG_SCOPE_ID", @"C:\");
        
        ValidateDto(result, @"C:\Code\Project\File1.js", @"Code\Project\File1.js");
    }
    
    [TestMethod]
    public void Create_WithLocalizedPath_ConstructsValidDto()
    {
        var testSubject = new ClientFileDtoFactory();

        var result = testSubject.Create(@"C:\привет\project\file1.js", "CONFIG_SCOPE_ID", @"C:\");
        
        ValidateDto(result,  @"C:\привет\project\file1.js", @"привет\project\file1.js");
    }
    
    [TestMethod]
    public void Create_WithUNCPath_ConstructsValidDto()
    {
        var testSubject = new ClientFileDtoFactory();

        var result = testSubject.Create(@"\\servername\work\project\file1.js", "CONFIG_SCOPE_ID", @"\\servername\work\");
        
        ValidateDto(result, @"\\servername\work\project\file1.js", @"project\file1.js");
    }
    
    [TestMethod]
    public void Create_WithWhitespacesPath_ConstructsValidDto()
    {
        var testSubject = new ClientFileDtoFactory();

        var result = testSubject.Create(@"C:\Code\My Project\My Favorite File2.js", "CONFIG_SCOPE_ID", @"C:\");
        
        ValidateDto(result, @"C:\Code\My Project\My Favorite File2.js", @"Code\My Project\My Favorite File2.js");
    }

    private static void ValidateDto(ClientFileDto actual, string expectedFsPath, string expectedIdeRelativePath)
    {
        actual.Should().BeEquivalentTo(new ClientFileDto(
            new FileUri(expectedFsPath),
            expectedIdeRelativePath,
            "CONFIG_SCOPE_ID", 
            null, 
            "utf-8", 
            expectedFsPath));
    }
}
