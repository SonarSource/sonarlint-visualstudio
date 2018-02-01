/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Diagnostics;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.Integration.WPF
{
    internal class IconViewModel : ViewModelBase
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
