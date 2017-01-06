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

using SonarLint.VisualStudio.Integration.WPF;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class RelayCommandTests
    {
        [TestMethod]
        public void RelayCommand_Ctor_EmptyPredicate_CanAlwaysExecute()
        {
            // Setup
            var command = new RelayCommand(() => { });

            // Act + Verify
            Assert.IsTrue(command.CanExecute());
        }

        [TestMethod]
        public void RelayCommandOfT_Ctor_EmptyPredicate_CanAlwaysExecute()
        {
            // Setup
            var command = new RelayCommand<object>(x => { });

            // Act + Verify
            Assert.IsTrue(command.CanExecute(null));
        }
    }
}
