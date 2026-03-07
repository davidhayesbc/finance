using System.Diagnostics;
using System.Net;
using System.Text;
using Xunit.Sdk;

namespace Privestio.E2E.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private const string AppHostProjectPath = "src/Privestio.AppHost/Privestio.AppHost.csproj";
    private const string DefaultBaseUrl = "http://localhost:5211";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly StringBuilder _output = new();
    private Process? _appHostProcess;
    private bool _managesAppHost;

    public string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL") ?? DefaultBaseUrl;

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BASE_URL")))
        {
            await WaitForWebAppAsync();
            return;
        }

        _managesAppHost = true;

        var repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../")
        );
        var startInfo = new ProcessStartInfo(
            "dotnet",
            $"run --project {AppHostProjectPath} --launch-profile http"
        )
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _appHostProcess = new Process { StartInfo = startInfo };
        _appHostProcess.OutputDataReceived += OnOutputDataReceived;
        _appHostProcess.ErrorDataReceived += OnOutputDataReceived;

        if (!_appHostProcess.Start())
        {
            throw new XunitException("Failed to start the Aspire AppHost for E2E tests.");
        }

        _appHostProcess.BeginOutputReadLine();
        _appHostProcess.BeginErrorReadLine();

        try
        {
            await WaitForWebAppAsync();
        }
        catch
        {
            await DisposeManagedAppHostAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await DisposeManagedAppHostAsync();
        _httpClient.Dispose();
    }

    private async Task WaitForWebAppAsync()
    {
        var deadline = DateTime.UtcNow + StartupTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (_appHostProcess is { HasExited: true })
            {
                throw CreateStartupException(
                    "The Aspire AppHost exited before the web app became available."
                );
            }

            try
            {
                using var response = await _httpClient.GetAsync(BaseUrl);
                if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect)
                {
                    return;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }

            await Task.Delay(PollInterval);
        }

        throw CreateStartupException($"Timed out waiting for the web app at {BaseUrl}.");
    }

    private async Task DisposeManagedAppHostAsync()
    {
        if (!_managesAppHost || _appHostProcess is null)
        {
            return;
        }

        try
        {
            if (!_appHostProcess.HasExited)
            {
                _appHostProcess.Kill(entireProcessTree: true);
                await _appHostProcess.WaitForExitAsync();
            }
        }
        finally
        {
            _appHostProcess.OutputDataReceived -= OnOutputDataReceived;
            _appHostProcess.ErrorDataReceived -= OnOutputDataReceived;
            _appHostProcess.Dispose();
            _appHostProcess = null;
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Data))
        {
            return;
        }

        lock (_output)
        {
            _output.AppendLine(args.Data);
        }
    }

    private XunitException CreateStartupException(string message)
    {
        string output;
        lock (_output)
        {
            output = _output.ToString();
        }

        return new XunitException(
            $"{message}{Environment.NewLine}{Environment.NewLine}AppHost output:{Environment.NewLine}{output}"
        );
    }
}
