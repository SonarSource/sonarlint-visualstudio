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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="ICompositionService"/>
    /// </summary>
    public class ConfigurableCompositionService : ICompositionService
    {
        private readonly CompositionContainer container;

        #region Helper methods

        public List<object> PartsToCompose { get; private set; }
        public List<Export> ExportsToCompose { get; private set; }

        private bool alreadyComposed = false;

        public ConfigurableCompositionService(CompositionContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.container = container;
            this.PartsToCompose = new List<object>();
            this.ExportsToCompose = new List<Export>();
        }

        public bool DoNothingOnSatisfyImportsOnce { get; set; }
        #endregion

        #region ICompositionService interface methods

        public void SatisfyImportsOnce(ComposablePart part)
        {
            if (this.DoNothingOnSatisfyImportsOnce)
            {
                return;
            }

            CompositionBatch batch = new CompositionBatch();

            // We only want to include the standard exports and parts to compose in the first composition
            if (!this.alreadyComposed)
            {
                foreach (object instance in this.PartsToCompose)
                {
                    batch.AddPart(instance);
                }

                foreach (Export export in this.ExportsToCompose)
                {
                    batch.AddExport(export);
                }
            }

            if (part != null)
            {
                batch.AddPart(part);
            }

            this.container.Compose(batch);
            this.alreadyComposed = true;
        }

        #endregion
    }
}
