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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class ErrorHandlerTests
    {
        [TestMethod]
        public void IsCriticalException_SimpleCriticalExceptions_ReturnsTrue()
        {
            CheckIsCriticalException(new StackOverflowException());
            CheckIsCriticalException(new AccessViolationException());
            CheckIsCriticalException(new AppDomainUnloadedException());
            CheckIsCriticalException(new BadImageFormatException());
            CheckIsCriticalException(new DivideByZeroException());
        }

        [TestMethod]
        public void IsCriticalException_WrappedCriticalExceptions_ReturnsTrue()
        {
            // 1. Wrapped 
            var ex = new AggregateException(
                new InvalidCastException(),         // not critical
                new StackOverflowException(),       // critical
                new ArgumentOutOfRangeException()); // not critical
            CheckIsCriticalException(ex);

            ex = new AggregateException(
                new InvalidCastException(),         // not critical
                new AggregateException(ex));        // nested exception is critical
            CheckIsCriticalException(ex);
        }

        [TestMethod]
        public void IsCriticalException_NonCriticalException_ReturnsFalse()
        {
            CheckIsNotCriticalException(new ArgumentOutOfRangeException());
            CheckIsNotCriticalException(new ArgumentNullException());
            CheckIsNotCriticalException(new NullReferenceException());
        }

        [TestMethod]
        public void IsCriticalException_WrappedNonCriticalExceptions_ReturnsFalse()
        {
            // 1. Wrapped 
            var ex = new AggregateException(
                new InvalidCastException(),         // not critical
                new ArgumentOutOfRangeException()); // not critical
            CheckIsNotCriticalException(ex);

            ex = new AggregateException(
                new InvalidCastException(),         // not critical
                new AggregateException(ex));        // nested exception is critical
            CheckIsNotCriticalException(ex);
        }

        private static void CheckIsCriticalException(Exception exception)
        {
            ErrorHandler.IsCriticalException(exception).Should().BeTrue();
        }

        private static void CheckIsNotCriticalException(Exception exception)
        {
            ErrorHandler.IsCriticalException(exception).Should().BeFalse();
        }
    }
}
