using System;
using System.Diagnostics;

class Program {
    static int Main(string[] args) {
        Console.WriteLine("Whisper-S Tool Lab");
        Console.WriteLine("使用 C# 进行一次ffmpeg -version 的调用\n");

        //命令配置
        var startInfo = new ProcessStartInfo {
            FileName = "ffmpeg",
            Arguments = "-version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExcute = false,
            CreateNoWindow = true,
        };

        //创建进程对象
        using Process process = new Process();
        process.StartInfo = startInfo;

        try {
            //启动进程
            process.Start();

            //读取输出
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

        }    catch (Exception ex) {
            Console.WriteLine("调用 ffmpeg 失败: " + ex.Message);
            return -1;
        }

    }
}