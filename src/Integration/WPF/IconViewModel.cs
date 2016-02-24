//-----------------------------------------------------------------------
// <copyright file="IconViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
