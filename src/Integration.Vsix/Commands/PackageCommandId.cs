//-----------------------------------------------------------------------
// <copyright file="PackageCommandId.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal enum PackageCommandId
    {
        // Buttons
        ManageConnections = 0x100,
        ProjectExcludePropertyToggle = 0x101,
        ProjectTestPropertyAuto = 0x102,
        ProjectTestPropertyTrue = 0x103,
        ProjectTestPropertyFalse = 0x104,

        // Menus
        ProjectSonarLintMenu = 0x850
    }
}
