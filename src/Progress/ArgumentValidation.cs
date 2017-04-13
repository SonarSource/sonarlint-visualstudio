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
using System.Diagnostics.Contracts;
using System.Linq;

namespace SonarLint.VisualStudio.Progress
{
    public static class ArgumentValidation
    {
        [ContractArgumentValidator]
        public static void NotNull([ValidatedNotNull]object variable, string variableName)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(variableName);
            }

            Contract.EndContractBlock();
        }

        [ContractArgumentValidator]
        public static void NotNullOrEmpty<T>([ValidatedNotNull]IEnumerable<T> enumerable, string variableName)
        {
            ArgumentValidation.NotNull(enumerable, variableName);

            if (!enumerable.Any())
            {
                throw new ArgumentException(variableName);
            }

            Contract.EndContractBlock();
        }

        [ContractArgumentValidator]
        public static TValue NotNullPassThrough<TTarget, TValue>([ValidatedNotNull]TTarget variable, string variableName, Func<TTarget, TValue> valueGetter)
            where TTarget : class
        {
            if (variable == null)
            {
                throw new ArgumentNullException(variableName);
            }

            ArgumentValidation.NotNull(valueGetter, "valueGetter");

            Contract.EndContractBlock();

            return valueGetter(variable);
        }
    }
}
