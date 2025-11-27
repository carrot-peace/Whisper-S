using System;
using System.IO;
using System.Threading.Tasks;

namespace WhisperS.Core.Audio
{
    public interface IAudioPreProcessor
    {
        string Name { get; }

        Task<string> ProcessAsync(string inputWavPath, IProgress<string>? progress = null);
    }

    // 插件 1：归一化（使用 ffmpeg 的 dynaudnorm）
    public sealed class NormalizePreProcessor : IAudioPreProcessor
    {
        private readonly string _ffmpegPath;

        public NormalizePreProcessor(string ffmpegPath = "ffmpeg")
        {
            _ffmpegPath = ffmpegPath;
        }

        public string Name => "Normalize";

        public async Task<string> ProcessAsync(
            string inputWavPath,
            IProgress<string>? progress = null)
        {
            string outputWav = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}_norm.wav");

            string args =
                $"-y -i \"{inputWavPath}\" " +
                "-af \"dynaudnorm\" " +
                "-ar 16000 -ac 1 -c:a pcm_s16le " +
                $"\"{outputWav}\"";

            int exitCode = await ProcessRunner.RunAsync(_ffmpegPath, args, progress);
            if (exitCode != 0) {
                throw new InvalidOperationException(
                    $"ffmpeg normalize failed with exit code {exitCode}.");
            }

            return outputWav;
        }
    }

    // 插件 2：简单高通 + 低通滤波（滤掉过低和过高频）
    public sealed class SimpleFilterPreProcessor : IAudioPreProcessor
    {
        private readonly string _ffmpegPath;

        public SimpleFilterPreProcessor(string ffmpegPath = "ffmpeg")
        {
            _ffmpegPath = ffmpegPath;
        }

        public string Name => "SimpleFilter";

        public async Task<string> ProcessAsync(
            string inputWavPath,
            IProgress<string>? progress = null)
        {
            string outputWav = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}_filt.wav");

            string filter = "highpass=f=100,lowpass=f=8000";

            string args =
                $"-y -i \"{inputWavPath}\" " +
                $"-af \"{filter}\" " +
                "-ar 16000 -ac 1 -c:a pcm_s16le " +
                $"\"{outputWav}\"";

            int exitCode = await ProcessRunner.RunAsync(_ffmpegPath, args, progress);
            if (exitCode != 0) {
                throw new InvalidOperationException(
                    $"ffmpeg filter failed with exit code {exitCode}.");
            }

            return outputWav;
        }
    }
}
