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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class RelayCommandTests
    {
        [TestMethod]
        public void RelayCommand_Ctor_EmptyPredicate_CanAlwaysExecute()
        {
            // Arrange
            var command = new RelayCommand(() => { });

            // Act + Assert
            command.CanExecute().Should().BeTrue();
        }

        [TestMethod]
        public void RelayCommandOfT_Ctor_EmptyPredicate_CanAlwaysExecute()
        {
            // Arrange
            var command = new RelayCommand<object>(x => { });

            // Act + Assert
            command.CanExecute(null).Should().BeTrue();
        }

        [TestMethod]
        public void RelayCommand_CanExecute_NonCriticalException_False()
        {
            var command = new RelayCommand(() => { }, () => throw new NotImplementedException("this is a test"));

            command.CanExecute().Should().BeFalse();
        }

        [TestMethod]
        public void GenericRelayCommand_CanExecute_NonCriticalException_False()
        {
            var command = new RelayCommand<object>(_ => { }, _ => throw new NotImplementedException("this is a test"));

            command.CanExecute(null).Should().BeFalse();
        }

        [TestMethod]
        public void RelayCommand_CanExecute_CriticalException_ExceptionNotCaught()
        {
            var command = new RelayCommand(() => { }, () => throw new StackOverflowException("this is a test"));

            Action act = () => command.CanExecute();

            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void GenericRelayCommand_CanExecute_CriticalException_ExceptionNotCaught()
        {
            var command = new RelayCommand<object>(_ => { }, _ => throw new StackOverflowException("this is a test"));

            Action act = () => command.CanExecute(null);

            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void RelayCommand_Execute_NonCriticalException_False()
        {
            var wasCalled = false;
            var command = new RelayCommand(() =>
            {
                wasCalled = true;
                throw new NotImplementedException("this is a test");
            });

            Action act = () => command.Execute();
            act.Should().NotThrow();

            wasCalled.Should().BeTrue();
        }

        [TestMethod]
        public void GenericRelayCommand_Execute_NonCriticalException_False()
        {
            var wasCalled = false;
            var command = new RelayCommand<object>(_ =>
            {
                wasCalled = true;
                throw new NotImplementedException("this is a test");
            });

            Action act = () => command.Execute(null);
            act.Should().NotThrow();

            wasCalled.Should().BeTrue();
        }

        [TestMethod]
        public void RelayCommand_Execute_CriticalException_ExceptionNotCaught()
        {
            var command = new RelayCommand(() => throw new StackOverflowException("this is a test"));

            Action act = () => command.Execute();
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void GenericRelayCommand_Execute_CriticalException_ExceptionNotCaught()
        {
            var command = new RelayCommand<object>(_ => throw new StackOverflowException("this is a test"));

            Action act = () => command.Execute(null);
            act.Should().ThrowExactly<StackOverflowException>();
        }
    }
}
