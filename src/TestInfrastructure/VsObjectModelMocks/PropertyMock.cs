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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class PropertyMock : Property
    {
        private readonly Properties parent;

        public PropertyMock(string name, Properties parent)
        {
            this.parent = parent;
            this.Name = name;
        }

        #region Property

        public object Application
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Properties Collection
        {
            get
            {
                return parent;
            }
        }

        public DTE DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get;
            set;
        }

        public short NumIndices
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Object
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Properties Parent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Value
        {
            get;
            set;
        }

        public object get_IndexedValue(object Index1, object Index2, object Index3, object Index4)
        {
            throw new NotImplementedException();
        }

        public void let_Value(object lppvReturn)
        {
            throw new NotImplementedException();
        }

        public void set_IndexedValue(object Index1, object Index2, object Index3, object Index4, object Val)
        {
            throw new NotImplementedException();
        }

        #endregion Property
    }
}