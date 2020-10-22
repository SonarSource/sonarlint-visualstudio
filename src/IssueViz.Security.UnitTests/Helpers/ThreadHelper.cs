using FluentAssertions;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Helpers
{
    internal class ThreadHelper
    {
        /// <summary>
        /// Shell-dependent implementation of <see cref="Integration.UnitTests.ThreadHelper"/>.
        /// </summary>
        public static void SetCurrentThreadAsUIThread()
        {
            var methodInfo = typeof(Microsoft.VisualStudio.Shell.ThreadHelper).GetMethod("SetUIThread", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            methodInfo.Should().NotBeNull("Could not find ThreadHelper.SetUIThread");
            methodInfo.Invoke(null, null);
        }
    }
}
