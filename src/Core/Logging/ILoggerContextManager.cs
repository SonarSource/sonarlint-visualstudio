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

namespace SonarLint.VisualStudio.Core.Logging;

public interface ILoggerContextManager
{
    /// <summary>
    /// Returns a combination of logger-level context and <see cref="messageLevelContext"/>.
    /// If one of the contexts is null, only returns the other.
    /// If both contexts are null, returns null.
    /// </summary>
    public string GetFormattedContextOrNull(MessageLevelContext messageLevelContext);
    /// <summary>
    /// Returns a combination of logger-level verbose context and verbose <see cref="messageLevelContext"/>.
    /// If one of the contexts is null, only returns the other.
    /// If both contexts are null, returns null.
    /// </summary>
    public string GetFormattedVerboseContextOrNull(MessageLevelContext messageLevelContext);

    /// <summary>
    /// Creates a new instance of logger-level context with appended <see cref="additionalContexts"/>
    /// </summary>
    ILoggerContextManager CreateAugmentedContext(IEnumerable<string> additionalContexts);

    /// <summary>
    /// Creates a new instance of logger-level context with appended <see cref="additionalVerboseContexts"/>
    /// </summary>
    ILoggerContextManager CreateAugmentedVerboseContext(IEnumerable<string> additionalVerboseContexts);
}
