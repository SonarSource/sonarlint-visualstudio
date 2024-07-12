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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration
{
    [ExcludeFromCodeCoverage] // Wrapper around Visual Studio
    public class UIContextWrapper : IUIContext
    {
        private readonly UIContext wrapped;

        public UIContextWrapper(UIContext uIContext) => wrapped = uIContext;

        public bool IsActive => wrapped.IsActive;

        public void WhenActivated(Action action) => wrapped.WhenActivated(action);
    }

    [ExcludeFromCodeCoverage] // Wrapper around Visual Studio
    [Export(typeof(IKnownUIContexts))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class KnownUIContextsWrapper : IKnownUIContexts
    {
        public event EventHandler<UIContextChangedEventArgs> SolutionBuildingContextChanged
        {
            add
            {
                KnownUIContexts.SolutionBuildingContext.UIContextChanged += value;
            }
            remove
            {
                KnownUIContexts.SolutionBuildingContext.UIContextChanged -= value;
            }
        }

        public event EventHandler<UIContextChangedEventArgs> SolutionExistsAndFullyLoadedContextChanged
        {
            add
            {
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged += value;
            }
            remove
            {
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged -= value;
            }
        }

        public event EventHandler<UIContextChangedEventArgs> CSharpProjectContextChanged
        {
            add
            {
                KnownUIContexts.CSharpProjectContext.UIContextChanged += value;
            }
            remove
            {
                KnownUIContexts.CSharpProjectContext.UIContextChanged -= value;
            }
        }

        public event EventHandler<UIContextChangedEventArgs> VBProjectContextChanged
        {
            add
            {
                KnownUIContexts.VBProjectContext.UIContextChanged += value;
            }
            remove
            {
                KnownUIContexts.VBProjectContext.UIContextChanged -= value;
            }
        }

        public IUIContext SolutionBuildingContext => new UIContextWrapper(KnownUIContexts.SolutionBuildingContext);

        public IUIContext SolutionExistsAndFullyLoadedContext => new UIContextWrapper(KnownUIContexts.SolutionExistsAndFullyLoadedContext);

        public IUIContext SolutionExistsAndNotBuildingAndNotDebuggingContext => new UIContextWrapper(KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext);

        public IUIContext DebuggingContext => new UIContextWrapper(KnownUIContexts.DebuggingContext);
    }
}
