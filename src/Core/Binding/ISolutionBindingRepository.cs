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

namespace SonarLint.VisualStudio.Core.Binding;

public interface ILegacySolutionBindingRepository
{
    /// <summary>
    /// Retrieves solution binding information
    /// </summary>
    /// <returns>Can be null if not bound</returns>
    BoundSonarQubeProject Read(string configFilePath);
}

public interface ISolutionBindingRepository
{
    /// <summary>
    /// Retrieves solution binding information
    /// </summary>
    /// <returns>Can be null if not bound</returns>
    BoundServerProject Read(string configFilePath);

    /// <summary>
    /// Writes the binding information
    /// </summary>
    /// <returns>Has file been saved</returns>
    bool Write(string configFilePath, BoundServerProject binding);

    /// <summary>
    /// Deletes the binding information
    /// </summary>
    /// <param name="localBindingKey">The local binding key of the <see cref="BoundServerProject" /></param>
    /// <returns>If binding has been deleted</returns>
    bool DeleteBinding(string localBindingKey);

    /// <summary>
    /// Raises when <see cref="Write" /> operation completes successfully
    /// </summary>
    event EventHandler BindingUpdated;

    /// <summary>
    /// Raises when <see cref="DeleteBinding" /> operation completes successfully
    /// </summary>
    event EventHandler<LocalBindingKeyEventArgs> BindingDeleted;

    /// <summary>
    /// Lists all the binding information
    /// </summary>
    /// <returns></returns>
    IEnumerable<BoundServerProject> List();
}

public class LocalBindingKeyEventArgs(string localBindingKey) : EventArgs
{
    public string LocalBindingKey { get; } = localBindingKey;
}
