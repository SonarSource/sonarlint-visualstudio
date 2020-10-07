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

using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common
{
    internal class TestableNormalizedTextChangeCollection : INormalizedTextChangeCollection
    {
        private readonly IList<ITextChange> changeCollection;

        public TestableNormalizedTextChangeCollection(IList<ITextChange> changeCollection)
        {
            this.changeCollection = changeCollection;
        }

        public IEnumerator<ITextChange> GetEnumerator() => changeCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)changeCollection).GetEnumerator();
        public void Add(ITextChange item) => changeCollection.Add(item);
        public void Clear() => changeCollection.Clear();
        public bool Contains(ITextChange item) => changeCollection.Contains(item);
        public void CopyTo(ITextChange[] array, int arrayIndex) => changeCollection.CopyTo(array, arrayIndex);
        public bool Remove(ITextChange item) => changeCollection.Remove(item);
        public int Count => changeCollection.Count;
        public bool IsReadOnly => changeCollection.IsReadOnly;
        public int IndexOf(ITextChange item) => changeCollection.IndexOf(item);
        public void Insert(int index, ITextChange item) => changeCollection.Insert(index, item);
        public void RemoveAt(int index) => changeCollection.RemoveAt(index);

        public ITextChange this[int index]
        {
            get => changeCollection[index];
            set => changeCollection[index] = value;
        }

        public bool IncludesLineChanges { get; }
    }
}
