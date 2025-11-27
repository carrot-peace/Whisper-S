using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WhisperS.Core.Audio;

// New: enable preprocessor support
namespace WhisperS.Core
{
    public class SubtitleGenerator
    {
        private readonly string _whisperRoot;
        private readonly string _whisperCliPath;
        private readonly string _modelPath;
        private readonly string _ffmpegPath;
        private readonly string? _language;
        private readonly int _threads;
        private readonly bool _useCoreMl;
        private readonly bool _useCoreMlDecoder;
        private readonly IReadOnlyList<IAudioPreProcessor> _preProcessors;

        public SubtitleGenerator(
            string? whisperRoot = null,
            string? whisperCliPath = null,
            string? modelPath = null,
            string ffmpegPath = "ffmpeg",
            string? language = "zh",
            int threads = 8,
            bool useCoreMl = false,
            bool useCoreMlDecoder = false,
            IEnumerable<IAudioPreProcessor>? preProcessors = null)
        {
            _whisperRoot = whisperRoot
                            ?? Environment.GetEnvironmentVariable("WSUB_WHISPER_ROOT")
                            ?? "/Users/ptilopsis/Whisper-S/whisper.cpp";

            _whisperCliPath = whisperCliPath
                              ?? Environment.GetEnvironmentVariable("WSUB_WHISPER_CLI")
                              ?? Path.Combine(_whisperRoot, "build/bin/whisper-cli");

            _modelPath = modelPath
                         ?? Environment.GetEnvironmentVariable("WSUB_WHISPER_MODEL")
                         ?? Path.Combine(_whisperRoot, "models", "ggml-large-v3-turbo.bin");

            _ffmpegPath = ffmpegPath;

            if (string.IsNullOrWhiteSpace(language) || language == "auto") {
                _language = null;     // 交给 whisper 自动检测
            } else {
                _language = language; // 如 "zh" / "en" / "ja"
            }

            _threads = threads;
            _useCoreMl = useCoreMl;
            _useCoreMlDecoder = useCoreMlDecoder;
            _preProcessors = (preProcessors != null) ? preProcessors.ToList() : new List<IAudioPreProcessor>();
        }

        public async Task<string> GenerateSubtitleAsync(
            string inputPath,
            IProgress<string>? progress = null)
        {
            inputPath = Path.GetFullPath(inputPath);

            if (!File.Exists(inputPath)) {
                throw new FileNotFoundException("Input file not found", inputPath);
            }

            if (!File.Exists(_whisperCliPath)) {
                throw new FileNotFoundException("whisper-cli not found", _whisperCliPath);
            }

            if (!File.Exists(_modelPath)) {
                throw new FileNotFoundException("Model file not found", _modelPath);
            }

            string inputDir = Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);

            string tempWav = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
            string processedWav = tempWav;
            string outputPrefix = Path.Combine(inputDir, fileNameWithoutExt);
            string outputSrt = outputPrefix + ".srt";

            try {
                // 1. 原始转码
                progress?.Report($"[ffmpeg] Converting to wav: {tempWav}");

                string ffmpegArgs =
                    $"-i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{tempWav}\"";

                int ffmpegExit = await ProcessRunner.RunAsync(_ffmpegPath, ffmpegArgs, progress);
                if (ffmpegExit != 0) {
                    throw new InvalidOperationException($"ffmpeg failed with exit code {ffmpegExit}.");
                }

                // 2. 依次执行所有预处理插件
                foreach (var processor in _preProcessors) {
                    progress?.Report($"[pre] {processor.Name}...");
                    processedWav = await processor.ProcessAsync(processedWav, progress);
                }

                // 3. 构造 whisper-cli 参数
                string whisperArgs =
                    $"-m \"{_modelPath}\" " +
                    $"-f \"{processedWav}\" " +
                    "--output-srt " +
                    $"-of \"{outputPrefix}\" " +
                    $"-t {_threads} ";

                if (!string.IsNullOrEmpty(_language)) {
                    whisperArgs += $"-l {_language} ";
                }

                if (_useCoreMl) {
                    whisperArgs += "--coreml ";
                }

                if (_useCoreMlDecoder) {
                    whisperArgs += "--coreml-decoder ";
                }

                progress?.Report("[whisper] Running whisper-cli...");
                int whisperExit = await ProcessRunner.RunAsync(_whisperCliPath, whisperArgs, progress);
                if (whisperExit != 0) {
                    throw new InvalidOperationException($"whisper-cli failed with exit code {whisperExit}.");
                }

                progress?.Report($"[done] Subtitle generated: {outputSrt}");
                return outputSrt;
            }
            finally {
                TryDelete(tempWav);
                if (processedWav != tempWav) {
                    TryDelete(processedWav);
                }
            }
        }

        private static void TryDelete(string path)
        {
            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            } catch {
                // ignore
            }
        }
    }
}
