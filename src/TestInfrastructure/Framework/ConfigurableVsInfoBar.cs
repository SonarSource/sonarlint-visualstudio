//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsInfoBar.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;

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
        #endregion

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
        #endregion

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

        #endregion

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
        #endregion
    }
}
