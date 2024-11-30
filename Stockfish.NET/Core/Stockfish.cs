using System.Diagnostics;
using System.IO.Compression;
using Stockfish.NET.Exceptions;
using Stockfish.NET.Models;

namespace Stockfish.NET.Core
{
    public class Stockfish : IStockfish
    {
        private const int MaxTries = 200;
        private const string StockfishText = "stockfish";
        private const string StockfishFile = "stockfish-windows-x86-64-avx2.exe";

        private static readonly string StockfishLocalDirectory = Path.Combine(Path.GetTempPath(), StockfishText);

        private int mDepth;

        public Settings Settings { get; set; }

        private string StockfishFileLocation { get; set; } = string.Empty;

        public static void Dispose()
        {
            if (File.Exists(StockfishLocalDirectory))
            {
                Directory.Delete(StockfishLocalDirectory);
            }
        }

        ~Stockfish()
        {
            Dispose();
        }

        //Evaluation
        //Best Move
        private async void DownloadStockfish()
        {
            if (!string.IsNullOrEmpty(StockfishFileLocation))
            {
                return;
            }

            bool finished = await DownloadAndUnzip(
                "https://github.com/official-stockfish/Stockfish/releases/latest/download/stockfish-windows-x86-64-avx2.zip",
                StockfishLocalDirectory);

            if (finished)
            {
                StockfishFileLocation = Path.Combine(StockfishLocalDirectory, StockfishText, StockfishFile);
            }
        }

        private static async Task<bool> DownloadAndUnzip(string requestUri, string directoryToUnzip)
        {
            using HttpResponseMessage response = await new HttpClient().GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            using ZipArchive zip = new(streamToReadFrom);
            zip.ExtractToDirectory(directoryToUnzip);

            return true;
        }

        private static Task<int> RunProcessAsync(Process process)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);
            process.OutputDataReceived += (s, ea) => Console.WriteLine(ea.Data);
            process.ErrorDataReceived += (s, ea) => Console.WriteLine("ERR: " + ea.Data);

            bool started = process.Start();
            if (!started)
            {
                //you may allow for the process to be re-used (started = false) 
                //but I'm not sure about the guarantees of the Exited event in such a case
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        public Stockfish(int depth = 2, Settings? settings = null)
        {
            mDepth = depth;
            Settings = settings ?? new Settings();

            DownloadStockfish();
        }

        public Stockfish(string path, int depth = 2, Settings? settings = null)
        {
            mDepth = depth;
            StockfishFileLocation = path;
        }

        private void Send(string command, Process process)
        {
            WriteLine(process, command);
        }

        private bool IsReady(Process process)
        {
            Send("isready", process);
            var tries = 0;
            while (tries < MaxTries)
            {
                ++tries;

                if (ReadLine(process) == "readyok")
                {
                    return true;
                }
            }
            throw new MaxTriesException();
        }

        private void SetOption(string name, string value, Process process)
        {
            Send($"setoption name {name} value {value}", process);
            if (!IsReady(process))
            {
                throw new ApplicationException();
            }
        }

        private void StartGame(Process process)
        {
            Send("ucinewgame", process);
            if (!IsReady(process))
            {
                throw new ApplicationException();
            }
        }

        public void StartFenGame(string fenPosition, Process process)
        {
            StartGame(process);
            Send($"position fen {fenPosition}", process);
            Send($"go depth {mDepth}", process);
        }

        private void SetThreads(int workingThreads, Process process)
        {
            SetOption("threads", $"{workingThreads - 1}", process);
        }

        public void WriteLine(Process process, string command)
        {
            if (process.StandardInput == null)
            {
                throw new NullReferenceException();
            }
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
        }

        public string ReadLine(Process process)
        {
            if (process.StandardOutput == null)
            {
                throw new NullReferenceException();
            }

            return process.StandardOutput.ReadLine() ?? string.Empty;
        }

        public async Task<string> GetBestMove(string fenPosition)
        {
            using Process process = new()
            {
                StartInfo =
                {
                    FileName = StockfishFileLocation,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            string bestMove = string.Empty;

            process.OutputDataReceived += (s, e) =>
            {
                var data = e.Data?.Split(' ').ToList();
                if (data != null && data.Count != 0)
                {
                    if (data[0] == "bestmove")
                    {
                        if (data[1] != "(none)")
                        {
                            bestMove = data[1];
                        }
                    }
                }
                Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                Console.WriteLine(e.Data);
            };

            await RunProcessAsync(process).ConfigureAwait(false);

            ReadLine(process);

            ThreadPool.GetAvailableThreads(out int workingThreads, out int _);
            SetThreads(workingThreads, process);

            SetOption("Skill level", Settings.SkillLevel.ToString(), process);
            foreach (var property in Settings.GetPropertiesAsDictionary())
            {
                SetOption(property.Key, property.Value, process);
            }

            StartFenGame(fenPosition, process);

            return bestMove;
        }

        public async Task<double> GetEvaluation(string fenPosition)
        {
            using Process process = new()
            {
                StartInfo =
                {
                    FileName = StockfishFileLocation,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            double evaluation = double.NaN;
            double finalEvaluation = double.NaN;
            Color compare =
                fenPosition.Contains("w") ? Color.White : Color.Black;

            process.OutputDataReceived += (s, e) =>
            {
                var data = e.Data?.Split(' ').ToList();
                if (data != null && data.Count != 0)
                {
                    if (data[0] == "info")
                    {
                        for (int i = 0; i < data.Count; i++)
                        {
                            if (data[i] != "score")
                            {
                                continue;
                            }

                            int k;
                            if (compare == Color.White)
                            {
                                k = 1;
                            }
                            else
                            {
                                k = -1;
                            }

                            evaluation = Convert.ToDouble(data[i + 2]) * k;
                        }
                    }
                    if (data[0] == "bestmove")
                    {
                        finalEvaluation = evaluation;
                    }
                }
                Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                Console.WriteLine(e.Data);
            };

            await RunProcessAsync(process).ConfigureAwait(false);

            ReadLine(process);

            ThreadPool.GetAvailableThreads(out int workingThreads, out int _);
            SetThreads(workingThreads, process);

            SetOption("Skill level", Settings.SkillLevel.ToString(), process);
            foreach (var property in Settings.GetPropertiesAsDictionary())
            {
                SetOption(property.Key, property.Value, process);
            }

            StartFenGame(fenPosition, process);

            return finalEvaluation;
        }
    }
}
