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

using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core.Analysis
{
    [Export(typeof(IIssueConsumerStorage))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class IssueConsumerStorage : IIssueConsumerStorage
    {
        private readonly object Lock = new();
        internal /*For testing*/ readonly Dictionary<string, IIssueConsumer> internalStorage;

        [ImportingConstructor]
        public IssueConsumerStorage()
        {
            internalStorage = new Dictionary<string, IIssueConsumer>();
        }

        // todo set and remove should be buffer-based, bot file path based

        public void Remove(string filePath)
        {
            lock (Lock)
            {
                internalStorage.Remove(filePath);
            }
        }

        public void Set(string filePath, IIssueConsumer issueConsumer)
        {
            lock (Lock)
            {
                internalStorage[filePath] = issueConsumer;
            }
        }

        public bool TryGet(string filePath, out IIssueConsumer issueConsumer)
        {
            lock (Lock)
            {
                return internalStorage.TryGetValue(filePath, out issueConsumer);
            }
        }
    }
}
