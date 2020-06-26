/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Reflection;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public class ExtendedCancellationTokenSource : CancellationTokenSource
    {
        public bool IsCancelledExplicitly { get; private set; }

        public new void Cancel()
        {
            IsCancelledExplicitly = true;
            base.Cancel();
        }

        public new void Cancel(bool throwOnException)
        {
            IsCancelledExplicitly = true;
            base.Cancel(throwOnException);
        }
    }

    public static class CancellationTokenExtensions
    {
        public static bool IsTimedOut(this CancellationToken token)
        {
            var fieldInfo = typeof(CancellationToken).GetField("m_source", BindingFlags.NonPublic | BindingFlags.Instance);
            var source = fieldInfo.GetValue(token);

            if (source is ExtendedCancellationTokenSource extendedTokenSource)
            {
                return !extendedTokenSource.IsCancelledExplicitly;
            }

            return false;
        }
    }
}
