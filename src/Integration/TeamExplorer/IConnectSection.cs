//-----------------------------------------------------------------------
// <copyright file="IConnectSection.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    // Test only interface
    internal interface IConnectSection
    {
        DependencyObject View { get; }

        ConnectSectionViewModel ViewModel { get; }
    }
}
