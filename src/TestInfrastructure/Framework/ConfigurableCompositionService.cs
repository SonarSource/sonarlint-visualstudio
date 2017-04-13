/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

        #endregion Helper methods

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

        #endregion ICompositionService interface methods
    }
}