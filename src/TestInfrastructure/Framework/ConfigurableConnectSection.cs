//-----------------------------------------------------------------------
// <copyright file="ConfigurableConnectSection.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.TeamExplorer;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableConnectSection : IConnectSection
    {
        public DependencyObject View
        {
            get;
            set;
        }

        public ConnectSectionViewModel ViewModel
        {
            get;
            set;
        }
    }
}
