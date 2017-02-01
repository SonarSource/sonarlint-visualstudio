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
        #endregion

        #region Verification methods
        public void AssertStepOperationsCreatedForDefinitions(IProgressStepDefinition[] definitions, IProgressStepOperation[] stepOperations)
        {
            definitions.Should().HaveSameCount(stepOperations, "The number of definitions doesn't match the number of step operations");
            for (int i = 0; i < definitions.Length; i++)
            {
                this.CreatedOperations[definitions[i]].Should().Be(stepOperations[i], "Mismatch at definition {0}", i);
            }
        }
        #endregion

        #region Test implementation of IProgressStepFactory (not to be used explicitly by the test code)
        IProgressStepOperation IProgressStepFactory.CreateStepOperation(IProgressController controller, IProgressStepDefinition definition)
        {
            return this.CreatedOperations[definition] = this.CreateOpeartion(definition);
        }

        IProgressStepExecutionEvents IProgressStepFactory.GetExecutionCallback(IProgressStepOperation stepOperation)
        {
            return this.CreatedNotifiers[stepOperation] = new ConfigurableProgressStepExecutionNotifier();
        }
        #endregion
    }
}
