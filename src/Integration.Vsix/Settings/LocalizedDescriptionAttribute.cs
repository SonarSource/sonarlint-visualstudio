//-----------------------------------------------------------------------
// <copyright file="LocalizedDescriptionAttribute.cs" company="SonarSource SA and Microsoft Corporation">
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
    internal sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private bool isLoaded;

        public LocalizedDescriptionAttribute(string descriptionResource)
            : base(descriptionResource)
        {
        }

        public override string Description
        {
            get
            {
                if (!isLoaded)
                {
                    isLoaded = true;
                    DescriptionValue = Strings.ResourceManager.GetString(DescriptionValue);
                }
                return DescriptionValue;
            }
        }
    }
}
