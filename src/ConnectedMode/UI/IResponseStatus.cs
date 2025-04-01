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


public interface IResponseStatus
{
    bool Success { get; }

    string WarningText { get; }
}

public class ResponseStatusWithData<T>(bool success, T responseData, string warningText = null) : IResponseStatus
{
    public ResponseStatusWithData() : this(false, default) { }

    public bool Success { get; init; } = success;
    public string WarningText { get; init; } = warningText;
    public T ResponseData { get; } = responseData;
}

public class ResponseStatus(bool success, string warningText = null) : IResponseStatus
{
    public ResponseStatus() : this(false) { }

    public bool Success { get; } = success;
    public string WarningText { get; init; } = warningText;
}
