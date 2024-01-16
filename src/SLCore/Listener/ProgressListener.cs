﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.Listener
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ProgressListener : ISLCoreListener
    {
        /// <summary>
        /// Stub method for compability with SLCore. We do not support progress
        /// </summary>
        /// <param name="parameters">Parameter's here for compability we discard it</param>
        public Task StartProgressAsync(object parameters)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stub method for compability with SLCore. We do not support progress
        /// </summary>
        /// <param name="parameters">Parameter's here for compability we discard it</param>
        public Task ReportProgressAsync(object parameters)
        {
            return Task.CompletedTask;
        }
    }
}
