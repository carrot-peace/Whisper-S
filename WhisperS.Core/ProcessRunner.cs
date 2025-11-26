using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WhisperS.Core
{
    internal static class ProcessRunner
    {
        public static Task<int> RunAsync(
            string fileName,
            string arguments,
            IProgress<string>? progress = null)
        {
            var tcs = new TaskCompletionSource<int>();

            var psi = new ProcessStartInfo {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    progress?.Report(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    progress?.Report(e.Data);
                }
            };

            process.Exited += (_, _) => {
                tcs.TrySetResult(process.ExitCode);
                process.Dispose();
            };

            if (!process.Start()) {
                throw new InvalidOperationException($"Failed to start process: {fileName}");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
