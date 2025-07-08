using System.IO;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.Core;

public interface ISonarLintConfigGenerator
{
    /// <summary>
    /// Generates the data for a SonarLint.xml file for the specified language
    /// </summary>
    SonarLintConfiguration Generate(
        IEnumerable<IRuleParameters> rules,
        IDictionary<string, string> sonarProperties,
        IFileExclusions fileExclusions,
        Language language);
}

public static class AnalysisMeasurement
{
    private static readonly List<WatchArgs> documentsWatched = new();
    private static readonly object lockObject = new();
    public const string MeasurementsFile = @"C:\Users\gabriela.trutan\Desktop\Roslyn\measurement.txt";

    public static void AddDocumentToWatch(string fileName, ActionType action)
    {
        var watchArgs = new WatchArgs(fileName, action, new Stopwatch());
        lock (lockObject)
        {
            if (documentsWatched.Any(d => d.FileName == fileName && d.Action == action))
            {
                return;
            }

            documentsWatched.Add(watchArgs);
            watchArgs.Stopwatch.Start();
        }
    }

    public static void StopDocumentWatch(string fileName)
    {
        lock (lockObject)
        {
            var watchArgs = documentsWatched.FirstOrDefault(d => d.FileName == fileName);
            if (watchArgs != null)
            {
                watchArgs.Stopwatch.Stop();
                File.AppendAllLines(MeasurementsFile,
                    [$"File: {Path.GetFileName(watchArgs.FileName)}, Action: {watchArgs.Action.ToString()}, Elapsed Time: {watchArgs.Stopwatch.ElapsedMilliseconds} ms"]);
                documentsWatched.Remove(watchArgs);
            }
        }
    }
}

public class WatchArgs(string fileName, ActionType action, Stopwatch stopwatch)
{
    public string FileName { get; } = fileName;
    public ActionType Action { get; } = action;
    public Stopwatch Stopwatch { get; } = stopwatch;
}

public enum ActionType
{
    Open,
    Save
}
