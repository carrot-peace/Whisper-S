using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WhisperS.Core;
using WhisperS.Core.Audio;

namespace WhisperS.Cli
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("Usage: wsub <input-audio-or-video-path>");
                return 1;
            }

            string inputPath = Path.GetFullPath(args[0]);

            // 从环境变量读取一些简单配置（后面可以换成正式 CLI 参数）
            var langEnv = Environment.GetEnvironmentVariable("WSUB_LANG");
            var coreMlEnv = Environment.GetEnvironmentVariable("WSUB_USE_COREML");
            var coreMlDecoderEnv = Environment.GetEnvironmentVariable("WSUB_USE_COREML_DECODER");
            var preprocEnv = Environment.GetEnvironmentVariable("WSUB_ENABLE_PREPROC");

            bool useCoreMl = coreMlEnv == "1" || coreMlEnv == "true";
            bool useCoreMlDecoder = coreMlDecoderEnv == "1" || coreMlDecoderEnv == "true";
            bool enablePreproc = preprocEnv == "1" || preprocEnv == "true";

            var preProcessors = new List<IAudioPreProcessor>();
            if (enablePreproc) {
                preProcessors.Add(new NormalizePreProcessor());
                preProcessors.Add(new SimpleFilterPreProcessor());
            }

            var generator = new SubtitleGenerator(
                language: string.IsNullOrWhiteSpace(langEnv) ? "zh" : langEnv,
                threads: 8,
                useCoreMl: useCoreMl,
                useCoreMlDecoder: useCoreMlDecoder,
                preProcessors: preProcessors
            );

            var progress = new Progress<string>(msg => Console.WriteLine(msg));

            try {
                string srtPath = await generator.GenerateSubtitleAsync(inputPath, progress);
                Console.WriteLine($"Subtitle generated: {srtPath}");
                return 0;
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}
