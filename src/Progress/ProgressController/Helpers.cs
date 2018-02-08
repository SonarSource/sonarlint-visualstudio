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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Progress controller helpers
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// The <paramref name="onFinishedAction"/> will be called once when the <paramref name="controller"/>
        /// will finish by invoking <see cref="IProgressEvents.Finished"/>.
        /// </summary>
        /// <param name="controller">Required. <seealso cref="IProgressController"/></param>
        /// <param name="onFinishedAction">Required. The action that will be invoked with the finished <see cref="ProgressControllerResult"/></param>
        /// <remarks>This code will not cause memory leaks due to event registration</remarks>
        public static void RunOnFinished(this IProgressEvents controller, Action<ProgressControllerResult> onFinishedAction)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (onFinishedAction == null)
            {
                throw new ArgumentNullException(nameof(onFinishedAction));
            }

            EventHandler<ProgressControllerFinishedEventArgs> onFinished = null;
            onFinished = (o, e) =>
            {
                controller.Finished -= onFinished;
                onFinishedAction.Invoke(e.Result);
            };

            controller.Finished += onFinished;
        }
    }
}
