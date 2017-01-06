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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Generic class that MEF imports an arbitrary type.
    /// Used when testing that platforms extensions can be imported as expected.
    /// </summary>
    public class SingleObjectImporter<T> where T : class
    {
        [Import]
        public T Import { get; set; }

        #region Assertions

        public void AssertImportIsNull()
        {
            Assert.IsNull(this.Import, "Expecting the import to be null");
        }

        public void AssertImportIsNotNull()
        {
            Assert.IsNotNull(this.Import, "Expecting the import not to be null");
        }

        public void AssertExpectedImport(T expected)
        {
            this.AssertImportIsNotNull();
            Assert.AreSame(this.Import, expected, "An unexpected instance was imported");
        }

        public void AssertImportIsInstanceOf<TExpected>()
        {
            this.AssertImportIsNotNull();
            Assert.IsInstanceOfType(this.Import, typeof(TExpected), "Import is not of the expected type");
        }

        #endregion
    }
}
