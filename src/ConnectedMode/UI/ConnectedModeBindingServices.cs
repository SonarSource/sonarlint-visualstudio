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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

internal interface IConnectedModeBindingServices
{
    public ISolutionInfoProvider SolutionInfoProvider { get; }
    public ISharedBindingConfigProvider SharedBindingConfigProvider { get; }
    public ISolutionBindingRepository SolutionBindingRepository { get; }
    public IBindingControllerAdapter BindingControllerAdapter { get; }
}

[Export(typeof(IConnectedModeBindingServices))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ConnectedModeBindingServices(
    ISolutionInfoProvider solutionInfoProvider,
    ISharedBindingConfigProvider sharedBindingConfigProvider,
    ISolutionBindingRepository solutionBindingRepository,
    IBindingControllerAdapter bindingControllerAdapter)
    : IConnectedModeBindingServices
{
    public ISolutionInfoProvider SolutionInfoProvider { get; } = solutionInfoProvider;
    public ISharedBindingConfigProvider SharedBindingConfigProvider { get; } = sharedBindingConfigProvider;
    public ISolutionBindingRepository SolutionBindingRepository { get; } = solutionBindingRepository;
    public IBindingControllerAdapter BindingControllerAdapter { get; } = bindingControllerAdapter;
}
