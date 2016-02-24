//-----------------------------------------------------------------------
// <copyright file="LocalizedDisplayNameAttribute.cs" company="SonarSource SA and Microsoft Corporation">
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
    internal sealed class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private bool isLoaded;

        public LocalizedDisplayNameAttribute(string displayNameResource)
            : base(displayNameResource)
        {
        }

        public override string DisplayName
        {
            get
            {
                if (!isLoaded)
                {
                    isLoaded = true;
                    DisplayNameValue = Strings.ResourceManager.GetString(DisplayNameValue);
                }
                return DisplayNameValue;
            }
        }
    }
}
