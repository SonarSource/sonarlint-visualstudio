/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.SLCore;

namespace SonarLint.VisualStudio.ConnectedMode.Transition;

public class MuteIssueException : Exception
{
    private MuteIssueException()
    {
    }

    private MuteIssueException(string message)
        : base(message)
    {
    }

    private MuteIssueException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    internal class UnavailableServiceProviderException() : MuteIssueException(SLCoreStrings.ServiceProviderNotInitialized);

    internal class ServerIssueNotFoundException() : MuteIssueException(Resources.MuteIssue_IssueNotFound);

    internal class NotInConnectedModeException() : MuteIssueException(Resources.MuteIssue_NotInConnectedMode);

    internal class NotPermittedException(string reason) : MuteIssueException(reason);

    internal class SlCoreException(Exception ex) : MuteIssueException(ex.Message, ex.InnerException);

    public class CancelledException : MuteIssueException;
}
