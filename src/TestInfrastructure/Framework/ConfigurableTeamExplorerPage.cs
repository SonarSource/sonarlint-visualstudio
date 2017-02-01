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
using System.ComponentModel;
using Microsoft.TeamFoundation.Controls;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableTeamExplorerPage : ITeamExplorerPage
    {
        public Guid PageId { get; }

        public ConfigurableTeamExplorerPage(Guid guid)
        {
            this.PageId = guid;
        }

        #region ITeamExplorerPage

        string ITeamExplorerPage.Title
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ITeamExplorerPage.PageContent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool ITeamExplorerPage.IsBusy
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        void ITeamExplorerPage.Initialize(object sender, PageInitializeEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.Loaded(object sender, PageLoadedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.SaveContext(object sender, PageSaveContextEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.Refresh()
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.Cancel()
        {
            throw new NotImplementedException();
        }

        object ITeamExplorerPage.GetExtensibilityService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion ITeamExplorerPage
    }
}