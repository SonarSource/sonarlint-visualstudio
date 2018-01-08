/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper for verification of <see cref="IProgressStepFactory"/> usage
    /// </summary>
    public class ConfigurableProgressStepFactory : IProgressStepFactory
    {
        public ConfigurableProgressStepFactory()
        {
            this.CreatedOperations = new Dictionary<IProgressStepDefinition, IProgressStepOperation>();
            this.CreatedNotifiers = new Dictionary<IProgressStepOperation, IProgressStepExecutionEvents>();
            this.CreateOpeartion = (definition) => new ConfigurableProgressTestOperation((c, n) => { });
        }

        #region Customization properties

        /// <summary>
        /// Map of the created <see cref="ConfigurableProgressTestOperation"/> for <see cref="IProgressStepDefinition"/>
        /// </summary>
        public Dictionary<IProgressStepDefinition, IProgressStepOperation> CreatedOperations
        {
            get;
            set;
        }

        /// <summary>
        /// Map of the <see cref="ConfigurableProgressStepExecutionNotifier"/> for <see cref="IProgressStepOperation"/>
        /// </summary>
        public Dictionary<IProgressStepOperation, IProgressStepExecutionEvents> CreatedNotifiers
        {
            get;
            set;
        }

        /// <summary>
        /// Replacement for the default <see cref="ConfigurableProgressTestOperation"/> which will be called for each <see cref="IProgressStepDefinition"/>
        /// </summary>
        public Func<IProgressStepDefinition, IProgressStepOperation> CreateOpeartion
        {
            get;
            set;
        }

        #endregion Customization properties

        #region Verification methods

        public void AssertStepOperationsCreatedForDefinitions(IProgressStepDefinition[] definitions, IProgressStepOperation[] stepOperations)
        {
            definitions.Should().HaveSameCount(stepOperations, "The number of definitions doesn't match the number of step operations");
            for (int i = 0; i < definitions.Length; i++)
            {
                this.CreatedOperations[definitions[i]].Should().Be(stepOperations[i], "Mismatch at definition {0}", i);
            }
        }

        #endregion Verification methods

        #region Test implementation of IProgressStepFactory (not to be used explicitly by the test code)

        IProgressStepOperation IProgressStepFactory.CreateStepOperation(IProgressController controller, IProgressStepDefinition definition)
        {
            return this.CreatedOperations[definition] = this.CreateOpeartion(definition);
        }

        IProgressStepExecutionEvents IProgressStepFactory.GetExecutionCallback(IProgressStepOperation stepOperation)
        {
            return this.CreatedNotifiers[stepOperation] = new ConfigurableProgressStepExecutionNotifier();
        }

        #endregion Test implementation of IProgressStepFactory (not to be used explicitly by the test code)
    }
}