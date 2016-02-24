//-----------------------------------------------------------------------
// <copyright file="LocalizedCategoryAttribute.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class LocalizedCategoryAttribute : CategoryAttribute
    {
        private bool isLoaded;
        string category;

        public LocalizedCategoryAttribute(string categoryResource)
            : base(categoryResource)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            if (!isLoaded)
            {
                isLoaded = true;
                this.category = Strings.ResourceManager.GetString(this.Category);
            }
            return this.category;
        }
    }
}
