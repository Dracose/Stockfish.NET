using Chess;

namespace Stockfish.NET.Tests
{
    public class Utils
    {
        public static string GetStockfishDir()
        {
            string path = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.Windows) ? $@"C:\stockfish-windows-x86-64-avx2" : "/usr/games/stockfish";
            return path;
        }

        public static ChessBoard BasicBoard = new();

        public static string StartingFen()
        {
            return BasicBoard.ToFen();
        }
    }
}