//-----------------------------------------------------------------------
// <copyright file="IHost.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IHost : IServiceProvider
    {
        /// <summary>
        /// The UI thread dispatcher. Not null.
        /// </summary>
        Dispatcher UIDIspatcher { get; }

        /// <summary>
        /// <see cref="ISonarQubeServiceWrapper"/>. Not null.
        /// </summary>
        ISonarQubeServiceWrapper SonarQubeService { get; }

        /// <summary>
        /// The visual state manager. Not null.
        /// </summary>
        IStateManager VisualStateManager { get; }

        /// <summary>
        /// The currently active section. Null when no active section.
        /// </summary>
        IConnectSection ActiveSection { get; }

        /// <summary>
        /// Sets the <see cref="ActiveSection"/> with the specified <paramref name="section"/>
        /// </summary>
        /// <param name="section">Required</param>
        void SetActiveSection(IConnectSection section);

        /// <summary>
        /// Clears the <see cref="ActiveSection"/>
        /// </summary>
        void ClearActiveSection();
    }
}
