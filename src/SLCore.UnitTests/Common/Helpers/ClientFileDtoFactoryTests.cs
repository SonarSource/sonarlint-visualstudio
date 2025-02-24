/*
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class ClientFileDtoFactoryTests
{
    private TestLogger testLogger;
    private ClientFileDtoFactory testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testLogger = new TestLogger();
        testSubject = new ClientFileDtoFactory(testLogger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ClientFileDtoFactory, IClientFileDtoFactory>(
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ClientFileDtoFactory>();

    [DataTestMethod]
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\", @"user\projectA\directoryA\file.cs")]
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\user\projectA\", @"directoryA\file.cs")]
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\user\projectA\directoryA\", @"file.cs")]
    [DataRow(@"C:\user\projectA\directoryA\file.cs", @"C:\user\projectB\directoryA\", @"..\..\projectA\directoryA\file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\", @"projectA\directoryA\file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\projectA\", @"directoryA\file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\projectA\directoryA\", @"file.cs")]
    [DataRow(@"\\servername\user\projectA\directoryA\file.cs", @"\\servername\user\projectB\directoryA\", @"..\..\projectA\directoryA\file.cs")]
    public void Create_CalculatesCorrectRelativePath(string filePath, string rootPath, string expectedRelativePath)
    {
        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", rootPath, new SourceFile(filePath));

        result.ideRelativePath.Should().BeEquivalentTo(expectedRelativePath);
    }

    [TestMethod]
    public void Create_ConstructsValidDto()
    {
        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", @"C:\", new SourceFile(@"C:\Code\Project\File1.js"));

        ValidateDto(result, @"C:\Code\Project\File1.js", @"Code\Project\File1.js");
    }

    [TestMethod]
    public void Create_WithContent_ConstructsValidDto()
    {
        const string content = "somecontent";

        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", @"C:\", new SourceFile(@"C:\Code\Project\File1.js", content: content));

        ValidateDto(result, @"C:\Code\Project\File1.js", @"Code\Project\File1.js", expectedContent: content);
    }

    [TestMethod]
    public void Create_WithLocalizedPath_ConstructsValidDto()
    {
        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", @"C:\", new SourceFile(@"C:\привет\project\file1.js"));

        ValidateDto(result,  @"C:\привет\project\file1.js", @"привет\project\file1.js");
    }

    [TestMethod]
    public void Create_WithUNCPath_ConstructsValidDto()
    {
        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", @"\\servername\work\", new SourceFile(@"\\servername\work\project\file1.js"));

        ValidateDto(result, @"\\servername\work\project\file1.js", @"project\file1.js");
    }

    [TestMethod]
    public void Create_WithWhitespacesPath_ConstructsValidDto()
    {
        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", @"C:\", new SourceFile(@"C:\Code\My Project\My Favorite File2.js"));

        ValidateDto(result, @"C:\Code\My Project\My Favorite File2.js", @"Code\My Project\My Favorite File2.js");
    }

    [TestMethod]
    public void Create_WithPathAboveRoot_ConstructsValidDto()
    {
        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", @"C:\Code\OtherProject\", new SourceFile(@"C:\Code\Project\File1.js"));

        ValidateDto(result, @"C:\Code\Project\File1.js", @"..\Project\File1.js");
    }

    [TestMethod]
    public void Create_WithNonRelativezablePath_ReturnsNullAndLogs()
    {
        const string filePath = @"C:\folder\project\file1.js";
        const string rootPath = @"D:\";

        var result = testSubject.CreateOrNull("CONFIG_SCOPE_ID", rootPath, new SourceFile(filePath));

        result.Should().BeNull();
        testLogger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ClientFile_NotRelative_Skipped, filePath, rootPath));
    }

    private static void ValidateDto(ClientFileDto actual, string expectedFsPath, string expectedIdeRelativePath, string expectedContent = null)
    {
        actual.Should().BeEquivalentTo(new ClientFileDto(
            new FileUri(expectedFsPath),
            expectedIdeRelativePath,
            "CONFIG_SCOPE_ID",
            null,
            "utf-8",
            expectedFsPath,
            expectedContent));
        actual.isUserDefined.Should().BeTrue();
    }
}
