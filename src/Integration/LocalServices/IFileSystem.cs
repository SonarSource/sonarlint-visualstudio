//-----------------------------------------------------------------------
// <copyright file="IFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration
{
    // Test wrapper over basic file system operations
    internal interface IFileSystem
    {
        bool IsFileExist(string filePath);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);
    }
}
