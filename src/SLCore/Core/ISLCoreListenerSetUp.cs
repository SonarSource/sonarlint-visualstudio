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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.SLCore.Core
{
    public interface ISLCoreListenerSetUp
    {
        /// <summary>
        /// Attach all the listeners to SLCore implementation of JsonRpc.
        /// </summary>
        /// <param name="slcoreJsonRpc">SLCore wrapper around JsonRpc</param>
        void Setup(ISLCoreJsonRpc slcoreJsonRpc);
    }

    [Export(typeof(ISLCoreListenerSetUp))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SLCoreListenerSetUp : ISLCoreListenerSetUp
    {
        private readonly IEnumerable<ISLCoreListener> listeners;

        [ImportingConstructor]
        public SLCoreListenerSetUp([ImportMany] IEnumerable<ISLCoreListener> listeners)
        {
            this.listeners = listeners;
        }

        public void Setup(ISLCoreJsonRpc slcoreJsonRpc)
        {
            foreach (var listener in listeners)
            {
                slcoreJsonRpc.AttachListener(listener);
            }
            
            slcoreJsonRpc.StartListening();
        }
    }
}
