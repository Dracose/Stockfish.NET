using System.Collections.Generic;
using Stockfish.NET.Models;

namespace Stockfish.NET
{
    public interface IStockfish
    {
        Task<string> GetBestMove(string fenPosition);
        Task<double> GetEvaluation(string fenPosition);
    }
}