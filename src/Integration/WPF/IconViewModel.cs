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

using System.Diagnostics;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.Integration.WPF
{
    public class IconViewModel : ViewModelBase
    {
        private Image image;

        public IconViewModel(object moniker, double width = 16.0, double height = 16.0)
        {
            this.Moniker = moniker;
            this.Height = height;
            this.Width = width;
        }

        public double Width
        {
            get;
        }

        /// <summary>
        /// Icon height. Default value is 16.
        /// </summary>
        public double Height
        {
            get;
        }

        /// <summary>
        /// <see cref="Microsoft.VisualStudio.Imaging.KnownMonikers"/>
        /// </summary>
        public object Moniker
        {
            get;
        }

        /// <summary>
        /// Returns the corresponding image for the specified <see cref="Moniker"/>.
        /// </summary>
        public Image Image
        {
            get
            {
                if (this.image == null && this.Moniker != null)
                {
                    var pendingImage = new Microsoft.VisualStudio.Imaging.CrispImage();
                    pendingImage.BeginInit();
                    pendingImage.Moniker = (Microsoft.VisualStudio.Imaging.Interop.ImageMoniker)this.Moniker;
                    pendingImage.Width = this.Width;
                    pendingImage.Height = this.Height;
                    pendingImage.EndInit();
                    this.image = pendingImage;

                    Debug.Assert(this.image.Source != null, "Could not resolve the image source. CrispImage style was supposed to be applied");
                }

                return this.image;
            }
        }
    }
}
