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

using System.Reflection;
using Moq.Language;
using Moq.Language.Flow;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    public static class MoqExtensions
    {
        public delegate void OutAction<TOut>(out TOut outVal);
        public delegate void OutAction<in T1, TOut>(T1 arg1, out TOut outVal);
        public delegate void OutAction<in T1, in T2, TOut>(T1 arg1, T2 args2, out TOut outVal);
        public delegate void OutAction<in T1, in T2, in T3, TOut>(T1 arg1, T2 args2, T3 args3, out TOut outVal);

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, TOut>(this ICallback<TMock, TReturn> mock,
            OutAction<TOut> action)
            where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, TOut>(this ICallback<TMock, TReturn> mock,
            OutAction<T1, TOut> action)
            where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, T2, TOut>(
            this ICallback<TMock, TReturn> mock, OutAction<T1, T2, TOut> action)
            where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, T2, T3, TOut>(
            this ICallback<TMock, TReturn> mock, OutAction<T1, T2, T3, TOut> action)
            where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        private static IReturnsThrows<TMock, TReturn> OutCallbackInternal<TMock, TReturn>(ICallback<TMock, TReturn> mock,
            object action)
            where TMock : class
        {
            mock.GetType()
                .Assembly.GetType("Moq.MethodCall")
                .InvokeMember("SetCallbackWithArguments",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, mock, new[] { action });
            return mock as IReturnsThrows<TMock, TReturn>;
        }
    }
}
