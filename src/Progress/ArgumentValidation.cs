//-----------------------------------------------------------------------
// <copyright file="ArgumentValidation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
