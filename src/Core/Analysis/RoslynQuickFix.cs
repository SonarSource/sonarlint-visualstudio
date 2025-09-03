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

namespace SonarLint.VisualStudio.Core.Analysis;

public class RoslynQuickFix(Guid id) : IRoslynQuickFix
{
    // private const string StoragePrefix = "#!#SQVS#!#ROSLYN|QUICKFIX|";
    private const string StoragePrefix = "|<SQVS_ROSLYN_QUICKFIX>|";

    public Guid Id { get; } = id;

    public string GetStorageValue() => StoragePrefix + Id;

    public static bool TryParse(string message, out RoslynQuickFix o)
    {
        if (message is not null && message.StartsWith(StoragePrefix) && Guid.TryParse(message.Substring(StoragePrefix.Length), out var id))
        {
            o = new RoslynQuickFix(id);
            return true;
        }

        o = null;
        return false;
    }
}
