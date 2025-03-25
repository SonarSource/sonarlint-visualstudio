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

namespace SonarLint.VisualStudio.ConnectedMode.UI;

/// <summary>
/// Indicates binding parameters that were computed automatically as opposed to manual UI interaction
/// </summary>
public abstract record BindingRequest
{
    internal abstract string TypeName { get; }

    private BindingRequest() { }

    /// <summary>
    /// Indicates binding parameters set in binding ui
    /// </summary>
    public record Manual : BindingRequest
    {
        internal override string TypeName => ConnectedMode.Resources.BindingType_Manual;
    }

    public abstract record AutomaticBindingRequest : BindingRequest;

    /// <summary>
    /// Indicates binding parameters derived from shared binding
    /// </summary>
    public record Shared : AutomaticBindingRequest
    {
        internal override string TypeName => ConnectedMode.Resources.BindingType_Shared;
    }

    /// <summary>
    /// Indicates binding parameters received from SLCore during assistBinding operation
    /// </summary>
    public record Assisted(
        string ServerConnectionId,
        string ServerProjectKey,
        bool IsFromSharedBinding) : AutomaticBindingRequest
    {
        internal override string TypeName => IsFromSharedBinding ? ConnectedMode.Resources.BindingType_SuggestedShared : ConnectedMode.Resources.BindingType_Suggested;
    }
}
