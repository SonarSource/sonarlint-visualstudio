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
