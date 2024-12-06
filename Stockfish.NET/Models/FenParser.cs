using Chess;

namespace Stockfish.NET.Models
{
    public delegate Task FenParsedHandler<in TFenArgs>(object? source, TFenArgs e);

    public class FenParser
    {
        public event FenParsedHandler<FenError> FenParsed;

        public async Task ParseFen(string fenGame)
        {
            try
            {
                await DoFenEvaluation(fenGame);
            }
            catch (ChessArgumentException)
            {
                if (FenParsed is not null)
                {
                    await FenParsed(this, FenError.Bad);
                }
            }
        }

        private async Task DoFenEvaluation(string fenGame)
        {
            if (FenParsed is null)
            {
                return;
            }

            ChessBoard chessBoard = ChessBoard.LoadFromFen(fenGame);

            int movesNumber = chessBoard!.Moves().Length;

            switch (movesNumber)
            {
                case > 0:
                    await FenParsed(this, FenError.Okay);
                    break;
                case 0:
                    await FenParsed(this, FenError.Mate);
                    break;
            }
        }
    }
}
