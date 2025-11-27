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

using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core.Initialization;

namespace SonarLint.VisualStudio.Core;

public interface IFocusOnNewCodeService : IRequireInitialization
{
    FocusOnNewCodeStatus Current { get; }
    event EventHandler<NewCodeStatusChangedEventArgs> Changed;
}

public interface IFocusOnNewCodeServiceUpdater : IFocusOnNewCodeService
{
    void Set(bool isEnabled);
}

[ExcludeFromCodeCoverage]
public class NewCodeStatusChangedEventArgs(FocusOnNewCodeStatus newStatus) : EventArgs
{
    public FocusOnNewCodeStatus NewStatus { get; } = newStatus;
}

[ExcludeFromCodeCoverage]
public class FocusOnNewCodeStatus(bool isEnabled)
{
    public bool IsEnabled { get; } = isEnabled;
}
