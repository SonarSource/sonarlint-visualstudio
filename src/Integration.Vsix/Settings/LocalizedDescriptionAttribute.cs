/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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
