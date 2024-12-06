using System.Diagnostics;
using Stockfish.NET.Models;

namespace Stockfish.NET.Core
{
    public class Stockfish : IStockfish, IDisposable
    {
        //private static readonly StringBuilder mStockfishOutput = null;


        private static int mDepth = 2;

        public required Settings Settings { get; set; }

        private string StockfishFileLocation { get; set; } = string.Empty;

        private readonly IStockfishDownloader mStockfishDownloader;

        public void Dispose()
        {
            mStockfishDownloader.Dispose();
        }

        ~Stockfish()
        {
            Dispose();
        }

        public Stockfish(int depth = 2, Settings? settings = null)
        {
            mDepth = depth;
            Settings = settings ?? new Settings();
        }

        //IsGameOverOrIllegal

        public Stockfish(string path, int depth = 2, Settings? settings = null)
        {
            mDepth = depth;
            StockfishFileLocation = path;
            Settings = settings ?? new Settings();
        }

        public async Task SetOption(string name, string value, StreamWriter input)
        {
            await WriteLine($"setoption name {name} value {value}", input);
        }

        public async Task StartFenGame(string fenPosition, StreamWriter input)
        {
            await WriteLine("ucinewgame", input);
            await WriteLine($"position fen {fenPosition}", input);
            await WriteLine($"go depth {mDepth}", input);
        }

        public async Task WriteLine(string command, StreamWriter input)
        {
            await input.WriteLineAsync(command);
        }

        public async Task EvaluateFen(string fenPosition, TaskCompletionSource<FenError> fenParsing)
        {
            FenParser fenParser = new();

            async Task OnFenParserOnFenParsed(object source, FenError fenArgs)
            {
                await Task.Factory.StartNew(async state =>
                {
                    var result = (FenError)(state is FenError ? state : FenError.Unread);

                    fenParsing.SetResult(result);
                }, fenArgs);
            }

            fenParser.FenParsed += OnFenParserOnFenParsed; 
            
            await fenParser.ParseFen(fenPosition);
        }

        public async Task<string> GetBestMove(string fenPosition)
        {
            TaskCompletionSource<FenError> fenParsing = new(TaskCreationOptions.RunContinuationsAsynchronously);
            
            await EvaluateFen(fenPosition, fenParsing);

            var fenParsedError = await fenParsing.Task;

            TaskCompletionSource<string> evaluationCompletion = new();

            using Process process = new()
            {
                StartInfo =
                {
                    FileName = StockfishFileLocation,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    RedirectStandardInput = true
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    return;
                }

                if (!e.Data.StartsWith("bestmove", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                List<string>? data = e.Data?.Split(' ').ToList();
                if (data == null || data.Count == 0)
                {
                    return;
                }

                if (data[0] != "bestmove")
                {
                    return;
                }

                if (data[1] == "(none)")
                {
                    return;
                }

                evaluationCompletion.SetResult(data[1]);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                Console.WriteLine("Error : " + e.Data);
            };

            switch (fenParsedError)
            {
                case FenError.Bad:
                    return FenError.Bad.ToStringByAttributes();
                case FenError.Mate:
                    return FenError.Mate.ToStringByAttributes();
                case FenError.Okay:
                    break;
                case FenError.Unread:
                    break;
            }
        
            process.Start();

            StreamWriter sortStream = process.StandardInput;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.StandardInput.FlushAsync();

            foreach (var property in Settings.GetPropertiesAsDictionary())
            {
                await SetOption(property.Key, property.Value, sortStream);
            }

            await StartFenGame(fenPosition, sortStream);

            return await evaluationCompletion.Task;
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
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();


            //one thread for the error line 
            //one thread for the output line ? 


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

            //You'll wait forever.. beware or be scarewd
            bool started = process.Start();
            if (!started)
            {
                //you may allow for the process to be re-used (started = false) 
                //but I'm not sure about the guarantees of the Exited event in such a case
                throw new InvalidOperationException("Could not start process: " + process);
            }
            StreamWriter streamWriter = process.StandardInput;

            process.BeginOutputReadLine();
            //process.BeginErrorReadLine();

            //await ReadLine(process);

            ThreadPool.GetAvailableThreads(out int workingThreads, out int _);
            //await SetThreads(workingThreads, streamWriter);

            await SetOption("Skill level", Settings.SkillLevel.ToString(), streamWriter);
            foreach (var property in Settings.GetPropertiesAsDictionary())
            {
                await SetOption(property.Key, property.Value, streamWriter);
            }

            await StartFenGame(fenPosition, streamWriter);

            return finalEvaluation;
        }
    }
}
