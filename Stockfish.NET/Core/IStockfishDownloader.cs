using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stockfish.NET.Core
{
    public interface IStockfishDownloader : IDisposable
    {
        Task DownloadStockfishAsync();

        Task CreateXStockfishesAsync(int x);
        //Add number to folder so you can download multiple instances of Stockfish at any one time
    }
}
