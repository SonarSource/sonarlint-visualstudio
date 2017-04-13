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
namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Factory representation to separate the <see cref="IProgressStepDefinition"/> from <see cref="IProgressStepOperation"/> and the execution notification using <see cref="IProgressStepExecutionEvents"/>
    /// </summary>
    public interface IProgressStepFactory
    {
        /// <summary>
        /// Create <see cref="CreateStepOperation"/> based on the <see cref="IProgressStepDefinition"/>
        /// </summary>
        /// <param name="controller">An instance of <see cref="IProgressController"/> for which to create the operation for <see cref="IProgressStepDefinition"/></param>
        /// <param name="definition">The definition to use when creating <see cref="IProgressStepOperation"/></param>
        /// <returns>An instance of <see cref="IProgressStepOperation"/></returns>
        IProgressStepOperation CreateStepOperation(IProgressController controller, IProgressStepDefinition definition);

        /// <summary>
        /// Returns a <see cref="IProgressStepExecutionEvents"/> that can be used to inform about the <see cref="IProgressStepOperation"/> execution changes
        /// </summary>
        /// <param name="stepOperation">The operation for which to fetch the <see cref="IProgressStepExecutionEvents"/></param>
        /// <returns>An instance of <see cref="IProgressStepExecutionEvents"/></returns>
        IProgressStepExecutionEvents GetExecutionCallback(IProgressStepOperation stepOperation);
    }
}
