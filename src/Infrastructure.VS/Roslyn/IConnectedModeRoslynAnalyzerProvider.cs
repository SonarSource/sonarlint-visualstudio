﻿/*
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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

public interface IConnectedModeRoslynAnalyzerProvider
{
    /// <summary>
    /// Returns SonarAnalyzer.CSharp & SonarAnalyzer.VisualBasic analyzer DLLs that are downloaded from the server for the current binding
    /// </summary>
    Task<ImmutableArray<AnalyzerFileReference>?> GetOrNullAsync();

    /// <summary>
    /// Provides updates about the analyzers for a given connection
    /// </summary>
    /// <remarks>
    /// Internally this reacts to SLCore synchronization and updates to the cached Connected Mode analyzers
    /// </remarks>
    event EventHandler<AnalyzerUpdatedForConnectionEventArgs> AnalyzerUpdatedForConnection;
}

[ExcludeFromCodeCoverage]
public class AnalyzerUpdatedForConnectionEventArgs(ImmutableArray<AnalyzerFileReference>? analyzers) : EventArgs
{
    public ImmutableArray<AnalyzerFileReference>? Analyzers { get; } = analyzers;
}
