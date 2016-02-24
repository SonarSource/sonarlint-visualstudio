//-----------------------------------------------------------------------
// <copyright file="Helpers.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Progress controller helpers
    /// </summary>
    public static class Helpers
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
