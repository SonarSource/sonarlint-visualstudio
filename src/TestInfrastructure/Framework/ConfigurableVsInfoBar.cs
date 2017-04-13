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

using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBar : IVsInfoBar, IVsInfoBarActionItemCollection, IVsInfoBarTextSpanCollection
    {
        private readonly List<IVsInfoBarActionItem> actions = new List<IVsInfoBarActionItem>();
        private readonly List<IVsInfoBarTextSpan> spans = new List<IVsInfoBarTextSpan>();

        #region IVsInfoBarActionItemCollection

        int IVsInfoBarActionItemCollection.Count
        {
            get
            {
                return this.actions.Count;
            }
        }

        IVsInfoBarActionItem IVsInfoBarActionItemCollection.GetItem(int index)
        {
            return this.actions[index];
        }

        #endregion IVsInfoBarActionItemCollection

        #region IVsInfoBarTextSpanCollection

        int IVsInfoBarTextSpanCollection.Count
        {
            get
            {
                return this.spans.Count;
            }
        }

        IVsInfoBarTextSpan IVsInfoBarTextSpanCollection.GetSpan(int index)
        {
            return this.spans[index];
        }

        #endregion IVsInfoBarTextSpanCollection

        #region IVsInfoBar

        IVsInfoBarActionItemCollection IVsInfoBar.ActionItems
        {
            get
            {
                return this;
            }
        }

        ImageMoniker IVsInfoBar.Image
        {
            get
            {
                return this.Image;
            }
        }

        bool IVsInfoBar.IsCloseButtonVisible
        {
            get
            {
                return this.IsCloseButtonVisible;
            }
        }

        IVsInfoBarTextSpanCollection IVsInfoBar.TextSpans
        {
            get
            {
                return this;
            }
        }

        #endregion IVsInfoBar

        #region Test helpers

        public ImageMoniker Image
        {
            get;
            set;
        }

        public bool IsCloseButtonVisible
        {
            get;
            set;
        }

        #endregion Test helpers
    }
}