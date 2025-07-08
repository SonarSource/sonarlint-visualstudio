using System.ComponentModel.Composition;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode;

[Export(typeof(IAnalysisRpcServer))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class RoslynAnalysisRpcServer : IAnalysisRpcServer
{
    private readonly IRoslynAnalyzerRunner roslynAnalyzerRunner;
    private readonly ILogger logger;

    [ImportingConstructor]
    public RoslynAnalysisRpcServer(
        IRoslynAnalyzerRunner roslynAnalyzerRunner,
        ILogger logger)
    {
        this.roslynAnalyzerRunner = roslynAnalyzerRunner;
        this.logger = logger.ForVerboseContext(nameof(RoslynAnalyzerRunner));
    }

    public async Task StartListen()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:60000/");
        listener.Start();
        logger.WriteLine("Listening analysis RPC");

        while (true)
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            // Example: Read a parameter from the query string
            string name = request.QueryString["filename"];
            AnalysisMeasurement.AddDocumentToWatch(name, ActionType.Open);
            var diagnostics = await roslynAnalyzerRunner.AnalyzeFileAsync(name);
            string responseString = JsonConvert.SerializeObject(diagnostics);

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            AnalysisMeasurement.StopDocumentWatch(name);
        }
    }
}

public interface IAnalysisRpcServer
{
    Task StartListen();
}
