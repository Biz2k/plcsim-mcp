using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlcSimMcpProxy
{
    class Program
    {
        private static Process? _workerProcess;
        private static string? _initMessage;
        private static readonly object _lock = new object();
        private static bool _isShuttingDown = false;
        private static CancellationTokenSource _workerCts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var workerPath = Path.Combine(baseDir, "PlcSimMcpServer.exe");

            if (!File.Exists(workerPath))
            {
                // Fallback for debugging in IDE
                workerPath = Path.Combine(baseDir, "..", "..", "..", "..", "PlcSimMcpServer", "bin", "Debug", "net48", "PlcSimMcpServer.exe");
                if (!File.Exists(workerPath))
                {
                    workerPath = Path.Combine(baseDir, "..", "..", "..", "..", "PlcSimMcpServer", "bin", "Release", "net48", "PlcSimMcpServer.exe");
                }
            }

            if (!File.Exists(workerPath))
            {
                Console.Error.WriteLine($"[Proxy Error] Worker executable not found at: {workerPath}");
                return;
            }

            StartWorker(workerPath);

            // Forward stdin from Client to Worker
            using var stdin = new StreamReader(Console.OpenStandardInput());
            while (!_isShuttingDown)
            {
                var line = await stdin.ReadLineAsync();
                if (line == null) break;

                // Capture initialize message to replay on crash
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("method", out var methodProp) && methodProp.GetString() == "initialize")
                    {
                        _initMessage = line;
                    }
                }
                catch { } // Ignore parse errors

                lock (_lock)
                {
                    if (_workerProcess != null && !_workerProcess.HasExited)
                    {
                        _workerProcess.StandardInput.WriteLine(line);
                        _workerProcess.StandardInput.Flush();
                    }
                }
            }
            
            StopWorker();
        }

        static void StartWorker(string workerPath)
        {
            lock (_lock)
            {
                _workerCts = new CancellationTokenSource();
                
                _workerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = workerPath,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _workerProcess.EnableRaisingEvents = true;
                
                _workerProcess.Exited += (s, e) =>
                {
                    if (!_isShuttingDown)
                    {
                        _workerCts.Cancel();
                        
                        // Wait a tiny bit to avoid rapid crash loops blocking CPU
                        Thread.Sleep(500);
                        
                        // Restart worker
                        StartWorker(workerPath);
                    }
                };
                
                try
                {
                    _workerProcess.Start();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Proxy Error] Failed to start worker: {ex.Message}");
                    return;
                }

                // Fire-and-forget streams forwarding
                _ = ForwardStreamAsync(_workerProcess.StandardOutput.BaseStream, Console.OpenStandardOutput(), _workerCts.Token);
                _ = ForwardStreamAsync(_workerProcess.StandardError.BaseStream, Console.OpenStandardError(), _workerCts.Token);

                // Replay initialization if this is a restart
                if (_initMessage != null)
                {
                    _workerProcess.StandardInput.WriteLine(_initMessage);
                    _workerProcess.StandardInput.Flush();
                    
                    // We can also send a notification back to the client that the worker restarted!
                    SendNotification("notifications/message", "Siemens API crashed. The MCP worker has been automatically restarted.");
                }
            }
        }

        static async Task ForwardStreamAsync(Stream source, Stream destination, CancellationToken token)
        {
            var buffer = new byte[8192];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0) break;
                    await destination.WriteAsync(buffer, 0, bytesRead, token);
                    await destination.FlushAsync(token);
                }
            }
            catch { }
        }

        static void SendNotification(string method, string message)
        {
            try
            {
                var payload = new
                {
                    jsonrpc = "2.0",
                    method = method,
                    params_ = new { message = message } // Note: MCP might expect different params schema, but this is a debug fallback
                };
                
                var json = JsonSerializer.Serialize(payload).Replace("params_", "params");
                var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(bytes, 0, bytes.Length);
                stdout.Flush();
            }
            catch { }
        }

        static void StopWorker()
        {
            _isShuttingDown = true;
            _workerCts.Cancel();
            lock (_lock)
            {
                if (_workerProcess != null && !_workerProcess.HasExited)
                {
                    try { _workerProcess.Kill(); } catch { }
                }
            }
        }
    }
}
