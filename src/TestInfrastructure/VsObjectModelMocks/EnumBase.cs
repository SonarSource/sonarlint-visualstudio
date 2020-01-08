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

using System.Collections.Generic;
using Microsoft.VisualStudio;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Base helper class for the various VS enumeration wrappers
    /// </summary>
    /// <typeparam name="Tin">Wrapped type</typeparam>
    /// <typeparam name="Tout">VS type forced by the specific enumeration</typeparam>
    public abstract class EnumBase<Tin, Tout>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        protected EnumBase()
        {
            this.Reset();
        }

        /// <summary>
        /// Cloning constructor
        /// </summary>
        protected EnumBase(EnumBase<Tin, Tout> other)
        {
            this.Items.AddRange(other.Items);
            this.Reset();
        }

        protected List<Tin> Items
        {
            get;
        } = new List<Tin>();

        protected abstract Tout GetItem(Tin input);

        protected int CurrentItemIndex { get; set; }

        protected int Skip(uint numberOfItemsToSkip)
        {
            this.CurrentItemIndex += (int)numberOfItemsToSkip;
            return this.Items.Count > this.CurrentItemIndex ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        protected int Reset()
        {
            this.CurrentItemIndex = -1;
            return VSConstants.S_OK;
        }

        protected int Next(uint itemsToFetch, Tout[] output, out uint itemsFetched)
        {
            itemsFetched = 0;
            if (this.Items.Count <= this.CurrentItemIndex)
            {
                return VSConstants.E_FAIL;
            }

            if (output == null || output.Length != itemsToFetch)
            {
                return VSConstants.E_FAIL;
            }

            for (int i = 0; i < itemsToFetch; i++)
            {
                this.CurrentItemIndex++;
                if (this.CurrentItemIndex < this.Items.Count)
                {
                    var current = this.Items[this.CurrentItemIndex];
                    output[i] = GetItem(current);
                    itemsFetched++;
                }
            }

            return VSConstants.S_OK;
        }
    }
}