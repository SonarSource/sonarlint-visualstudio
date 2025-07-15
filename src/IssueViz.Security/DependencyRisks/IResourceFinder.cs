﻿/*
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
using System.Windows;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

/// <summary>
/// Abstraction for finding resources introduced to allow UI testability.
/// </summary>
public interface IResourceFinder
{
    /// <summary>
    /// Finds a resource by its key in the context of the specified element.
    /// </summary>
    object TryFindResource(FrameworkElement element, string resourceKey);
}

[ExcludeFromCodeCoverage]
public class ResourceFinder : IResourceFinder
{
    /// <inheritdoc cref="IResourceFinder.TryFindResource"/>
    public object TryFindResource(FrameworkElement element, string resourceKey) => element.TryFindResource(resourceKey);
}
