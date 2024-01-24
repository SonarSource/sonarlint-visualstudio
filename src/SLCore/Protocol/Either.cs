/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

namespace SonarLint.VisualStudio.SLCore.Protocol
{
    /// <summary>
    /// Represents an option class where only one of the properties <see cref="Left"/> or <see cref="Right"/> is not null
    /// </summary>
    public sealed class Either<TLeft, TRight>
        where TLeft : class
        where TRight : class
    {
        private Either()
        {
        }

        public TLeft Left { get; private set; }
        public TRight Right { get; private set; }

        public static Either<TLeft, TRight> CreateLeft(TLeft left) => new Either<TLeft, TRight> { Left = left };
        public static Either<TLeft, TRight> CreateRight(TRight right) => new Either<TLeft, TRight> { Right = right };
    }
}
