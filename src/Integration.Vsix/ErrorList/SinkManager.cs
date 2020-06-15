/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.TableManager;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // Interface introduced to simplify testing.
    internal interface ISinkManagerRegister
    {
        void AddSinkManager(SinkManager manager);
        void RemoveSinkManager(SinkManager manager);
    }

    /// <summary>
    /// Error list plumbing
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every consumer of data from an <see cref="ITableDataSource"/> provides an <see cref="ITableDataSink"/> to record the changes. We give the consumer
    /// an IDisposable (this object) that they hang on to as long as they are interested in our data (and they Dispose() of it when they are done).
    /// </para>
    /// <para>
    /// The sink is an external component that might not handle notifications correctly.
    /// See https://github.com/SonarSource/sonarlint-visualstudio/issues/1055 for an example.
    /// Consequently, we'll code defensively.
    /// </para>
    /// <para>
    /// See the README.md in this folder for more information
    /// </para>
    /// </summary>
    internal sealed class SinkManager : IDisposable
    {
        private ISinkManagerRegister sinkRegister;
        private readonly ITableDataSink sink;

        internal SinkManager(ISinkManagerRegister sinkRegister, ITableDataSink sink)
        {
            this.sinkRegister = sinkRegister;
            this.sink = sink;

            sinkRegister.AddSinkManager(this);
        }

        public void Dispose()
        {
            sinkRegister?.RemoveSinkManager(this);
            sinkRegister = null;
        }

        public void AddFactory(SnapshotFactory factory)
        {
            SafeOperation("AddFactory", () => sink.AddFactory(factory));
        }

        public void RemoveFactory(SnapshotFactory factory)
        {
            SafeOperation("RemoveFactory", () => sink.RemoveFactory(factory));
        }

        public void UpdateSink()
        {
            SafeOperation("FactorySnapshotChanged", () => sink.FactorySnapshotChanged(null));
        }

        private void SafeOperation(string operationName, Action op)
        {
            try
            {
                op();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exception.
                // We are not logging the errors to the output window because it might be too noisy e.g. if
                // bug #1055 mentioned above occurs then the faulty sink will throw an exception each
                // time a character is typed in the editor.
                System.Diagnostics.Debug.WriteLine($"Error in sink {sink.GetType().FullName}.{operationName}: {ex.Message}");
            }
        }
    }
}
