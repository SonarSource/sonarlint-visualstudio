//-----------------------------------------------------------------------
// <copyright file="ConfigurableCompositionService.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
