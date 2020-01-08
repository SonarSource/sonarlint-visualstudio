/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration
{
    [ExcludeFromCodeCoverage] // Wrapper around Visual Studio
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
    }
}
