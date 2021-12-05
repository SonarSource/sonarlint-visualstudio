/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    [Export(typeof(IThreadHandling))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ThreadHandling : IThreadHandling
    {
        public bool CheckAccess() => ThreadHelper.CheckAccess();

        public void ThrowIfOnUIThread() => ThreadHelper.ThrowIfOnUIThread();

        public void ThrowIfNotOnUIThread() => ThreadHelper.ThrowIfNotOnUIThread();

        public async Task RunOnUIThread(Action op) => await VS.RunOnUIThread.RunAsync(op);

        public T Run<T>(Func<System.Threading.Tasks.Task<T>> asyncMethod) => ThreadHelper.JoinableTaskFactory.Run(asyncMethod);
    }
}
