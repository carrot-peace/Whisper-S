# Whisper-S

A small C# command line tool that uses a local [`whisper.cpp`](https://github.com/ggml-org/whisper.cpp) + `whisper-large-v3-turbo` model to generate subtitles (SRT) from audio/video files.

> Input: audio or video file  
> Output: ready-to-use `.srt` subtitle file

调用本地 [`whisper.cpp`](https://github.com/ggml-org/whisper.cpp) + `whisper-large-v3-turbo` 模型的小型C#命令行工具，可以实现由音/视频文件自动生成SRT字幕文件

## Features (v0.1)

- Run locally on macOS (Apple Silicon).
- Use `ffmpeg` to extract/convert audio to 16 kHz mono WAV.
- Call `whisper.cpp` (`whisper-cli`) with `whisper-large-v3-turbo` to generate SRT.
- Simple CLI:
  ```bash
  wsub <input-audio-or-video-path>

- *Currently hard-coded for Chinese (`-l zh`). Language and other options will be configurable in future versions.*

- 仅能在macOS（Apple Silicon）上运行

- 使用 `ffmpeg` 来对音频进行格式转换和压缩（单通道16kHz，WAV）

- 调用 `whisper.cpp`的 `whisper-large-v3-turbo`模型生成SRT

- CLI 示例：

  ```bash
  wsub <input-audio-or-video-path>
  ```

- *目前仅支持中文转写，源文件语言和其他选项将在后续版本更新*

## Requirements

- macOS with Apple Silicon (tested on M4).
- [.NET SDK 8 or later](https://dotnet.microsoft.com/).
- [`ffmpeg`](https://ffmpeg.org/) available in `PATH`.
- A working [`whisper.cpp`](https://github.com/ggml-org/whisper.cpp?utm_source=chatgpt.com) checkout with:
  - `whisper.cpp/build/bin/whisper-cli`
  - `whisper.cpp/models/ggml-large-v3-turbo.bin`

## Setup

1. Clone and build `whisper.cpp`:

   ```
   git clone https://github.com/ggml-org/whisper.cpp.git
   cd whisper.cpp
   
   ./models/download-ggml-model.sh large-v3-turbo
   
   cmake -B build
   cmake --build build -j --config Release
   ```

2. Place `whisper.cpp` next to this project, for example:

   ```
   /Users/you/Whisper-S
     ├── WhisperS.sln
     ├── WhisperS.Cli/
     └── whisper.cpp/
   ```

3. Make sure `.gitignore` ignores `whisper.cpp/` (this repo treats it as an external dependency).

## Usage

From the project root:

```
dotnet run --project ./WhisperS.Cli/WhisperS.Cli.csproj -- "/path/to/input.m4a"
```

This will:

1. Use `ffmpeg` to convert the input to a temp 16 kHz mono WAV.
2. Call `whisper-cli` with `whisper-large-v3-turbo`.
3. Generate a `.srt` file next to your input file, with the same base name.

Example:

```
dotnet run --project ./WhisperS.Cli/WhisperS.Cli.csproj -- "/Users/you/voice-notes/TML-11.m4a"

# Output:
# /Users/you/voice-notes/TML-11.srt
```

## Configuration

By default the tool looks for `whisper.cpp` in:

```
private const string DefaultWhisperRoot = "/Users/you/Whisper-S/whisper.cpp";
```

You can override this via environment variables:

- `WSUB_WHISPER_ROOT` – path to `whisper.cpp` root.
- `WSUB_WHISPER_CLI` – full path to `whisper-cli`.
- `WSUB_WHISPER_MODEL` – full path to `ggml-large-v3-turbo.bin`.

## Roadmap

-  CLI options: language (`--lang`), threads (`--threads`), output directory.
-  Batch mode: process all files in a directory.
-  Config file support.
-  Better logging and progress display.
-  Packaging as a `dotnet tool`.