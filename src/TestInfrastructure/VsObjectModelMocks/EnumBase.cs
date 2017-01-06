/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio;
using System.Collections.Generic;

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
