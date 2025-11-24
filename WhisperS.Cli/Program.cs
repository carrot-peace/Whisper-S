using System;
using System.Diagnostics;
using System.IO;

namespace WhisperS.Cli
{
    internal class Program
    {
        // whisper.cpp 仓库的根目录
        private const string DefaultWhisperRoot = "/Users/ptilopsis/Whisper-S/whisper.cpp";

        static int Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("Usage: wsub <input-audio-or-video-path>");
                return 1;
            }

            string inputPath = Path.GetFullPath(args[0]);

            if (!File.Exists(inputPath)) {
                Console.WriteLine($"Input file not found: {inputPath}");
                return 1;
            }

            string whisperRoot = Environment.GetEnvironmentVariable("WSUB_WHISPER_ROOT")
                                 ?? DefaultWhisperRoot;

            string whisperCliPath = Environment.GetEnvironmentVariable("WSUB_WHISPER_CLI")
                                    ?? Path.Combine(whisperRoot, "build/bin/whisper-cli");

            string modelPath = Environment.GetEnvironmentVariable("WSUB_WHISPER_MODEL")
                               ?? Path.Combine(whisperRoot, "models", "ggml-large-v3-turbo.bin");

            string ffmpegPath = "ffmpeg";

            if (!File.Exists(whisperCliPath)) {
                Console.WriteLine($"whisper-cli not found: {whisperCliPath}");
                Console.WriteLine("Please check DefaultWhisperRoot or WSUB_WHISPER_CLI.");
                return 1;
            }

            if (!File.Exists(modelPath)) {
                Console.WriteLine($"Model file not found: {modelPath}");
                Console.WriteLine("Please check WSUB_WHISPER_MODEL or your whisper.cpp models directory.");
                return 1;
            }

            string inputDir = Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);

            string tempWav = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
            string outputPrefix = Path.Combine(inputDir, fileNameWithoutExt);

            Console.WriteLine($"Input : {inputPath}");
            Console.WriteLine($"Temp  : {tempWav}");
            Console.WriteLine($"Output: {outputPrefix}.srt");
            Console.WriteLine();

            try {
                string ffmpegArgs =
                    $"-i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{tempWav}\"";

                Console.WriteLine("Running ffmpeg...");
                int ffmpegExit = RunProcess(ffmpegPath, ffmpegArgs);
                if (ffmpegExit != 0) {
                    Console.WriteLine($"ffmpeg failed with exit code {ffmpegExit}.");
                    return ffmpegExit;
                }

                string whisperArgs =
                    $"-m \"{modelPath}\" " +
                    $"-f \"{tempWav}\" " +
                    $"--output-srt " +
                    $"-of \"{outputPrefix}\" " +
                    $"-l zh " +
                    $"-t 8";

                Console.WriteLine("Running whisper-cli...");
                int whisperExit = RunProcess(whisperCliPath, whisperArgs);
                if (whisperExit != 0) {
                    Console.WriteLine($"whisper-cli failed with exit code {whisperExit}.");
                    return whisperExit;
                }

                Console.WriteLine();
                Console.WriteLine($"Subtitle generated: {outputPrefix}.srt");
                return 0;
            }
            finally {
                if (File.Exists(tempWav)) {
                    try {
                        File.Delete(tempWav);
                    } catch (Exception ex) {
                        Console.WriteLine($"Warning: failed to delete temp file: {ex.Message}");
                    }
                }
            }
        }

        private static int RunProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false
            };

            using (var process = Process.Start(psi)) {
                if (process == null) {
                    throw new InvalidOperationException($"Failed to start process: {fileName}");
                }

                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
