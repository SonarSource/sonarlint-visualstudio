/*
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

using System;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Wrappers around System.Diagnostics.Process for testing
/// </summary>
namespace SonarLint.VisualStudio.Core.SystemAbstractions
{
    public interface IProcessFactory
    {
        IProcess Start(ProcessStartInfo startInfo);
    }

    public interface IProcess : IDisposable
    {
        int Id { get; }
        bool HasExited { get; }
        int ExitCode { get; }
        ProcessStartInfo StartInfo { get; }
        void BeginOutputReadLine();
        Task WaitForExitAsync(int milliseconds);
        void Kill();

        /// <summary>
        /// Callback when output is received.
        /// Logically equivalent to handling the <see cref="System.Diagnostics.Process.OutputDataReceived"/> event.
        /// </summary>
        /// <remarks>
        /// <see cref="System.Diagnostics.DataReceivedEventArgs"/> does not have public constructor so we
        /// can't just wrap the <see cref="System.Diagnostics.Process.OutputDataReceived"/> event.
        /// We could create an equivalent event, but since there is only one string field it is simpler
        /// to use an action.
        /// </remarks>
        Action<string> HandleOutputDataReceived { get; set; }
    }

    public class ProcessFactory : IProcessFactory
    {
        public IProcess Start(ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo);
            return new ProcessWrapper(process);
        }
    }

    internal sealed class ProcessWrapper : IProcess
    {
        private readonly Process wrapped;

        public ProcessWrapper(Process wrapped) => this.wrapped = wrapped;

        public int Id => wrapped.Id;
        public bool HasExited => wrapped.HasExited;
        public int ExitCode => wrapped.ExitCode;
        public ProcessStartInfo StartInfo => wrapped.StartInfo;
        public void Kill() => wrapped.Kill();

        // Make task genuinely async
        public Task WaitForExitAsync(int milliseconds) => Task.FromResult(wrapped.WaitForExit(milliseconds));
        public void BeginOutputReadLine() => wrapped.BeginOutputReadLine();

        private Action<string> handleOutputData;

        public Action<string> HandleOutputDataReceived
        {
            get { return handleOutputData; }
            set
            {
                handleOutputData = value;
                if (value == null)
                {
                    wrapped.OutputDataReceived -= Wrapped_OutputDataReceived;
                }
                else
                {
                    wrapped.OutputDataReceived += Wrapped_OutputDataReceived;
                }
            }
        }

        public void Dispose() => wrapped.Dispose();

        private void Wrapped_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleOutputDataReceived?.Invoke(e.Data);
        }
    }
}
