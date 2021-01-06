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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class ObservableCollectionExtensionsTests
    {
        [TestMethod]
        public void RemoveAll_NoItems_CollectionChangedEventNotRaised()
        {
            var eventHandler = new Mock<NotifyCollectionChangedEventHandler>();
            var collection = new ObservableCollection<int>();

            collection.CollectionChanged += eventHandler.Object;

            collection.RemoveAll();

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void RemoveAll_HasItems_ItemsRemovedAndCollectionChangedEventRaised(int numberOfItems)
        {
            var items = Enumerable.Range(1, numberOfItems).ToList();

            var eventHandler = new Mock<NotifyCollectionChangedEventHandler>();
            var collection = new ObservableCollection<int>(items);

            collection.CollectionChanged += eventHandler.Object;

            collection.RemoveAll();

            foreach (var item in items)
            {
                eventHandler.Verify(x => x(collection,
                        It.Is((NotifyCollectionChangedEventArgs e) =>
                            e.NewItems == null &&
                            e.OldItems != null &&
                            e.OldItems.Count == 1 &&
                            (int)e.OldItems[0] == item &&
                            e.Action == NotifyCollectionChangedAction.Remove)),
                    Times.Once);
            }

            eventHandler.VerifyNoOtherCalls();
        }
    }
}
