using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Stockfish.NET.Core
{
    public class StockfishDownloader : IStockfishDownloader
    {
        public Task DownloadStockfishAsync()
        {
            throw new NotImplementedException();
        }

        public Task CreateXStockfishesAsync(int x)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetStockfishesPaths()
        {
            return new List<string>();
        }

        private const string StockfishText = "stockfish";
        private const string StockfishFile = "stockfish-windows-x86-64-avx2.exe";

        private static readonly string StockfishLocalDirectory = Path.Combine(Path.GetTempPath(), StockfishText);

        private ConcurrentBag<string> StockfishLibrary = new ConcurrentBag<string>();

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

        public async Task DownloadStockfish()
        {
            if (Directory.Exists(StockfishLocalDirectory))
            {
                return;
            }

            bool finished = await DownloadAndUnzip(
                "https://github.com/official-stockfish/Stockfish/releases/latest/download/stockfish-windows-x86-64-avx2.zip",
                StockfishLocalDirectory);

            StockfishLibrary.Add(Path.Combine(StockfishLocalDirectory, StockfishText, StockfishFile));
        }

        //DOwnload windows 
        //download osx
        //download linux ?

        //determine os
        //private async Task DetermineOs()
        //{
            
        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    {
        //        path = $@"{dir}\Stockfish.NET.Tests\Stockfish\win\stockfish_12_win_x64\stockfish_20090216_x64.exe";
        //    }
        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //    {
        //        path = "/usr/games/stockfish";
        //    }
        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //    {

        //    }
        //}
    }
}
