/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Sentry;

namespace SonarLint.VisualStudio.Integration.Telemetry;

internal interface ISentrySdk
{
    IDisposable PushScope();
    void ConfigureScope(Action<Scope> configureScope);
    IDisposable Init(Action<SentryOptions> options);
    void CaptureException(Exception exception);
    void Close();
}

[Export(typeof(ISentrySdk))]
[PartCreationPolicy(CreationPolicy.Shared)]
[ExcludeFromCodeCoverage]
internal sealed class SentrySdkAdapter : ISentrySdk
{
    public IDisposable PushScope() => SentrySdk.PushScope();

    public void ConfigureScope(Action<Scope> configureScope) => SentrySdk.ConfigureScope(configureScope);

    public IDisposable Init(Action<SentryOptions> options) => SentrySdk.Init(options);

    public void CaptureException(Exception exception) => SentrySdk.CaptureException(exception);

    public void Close() => SentrySdk.Close();
}
