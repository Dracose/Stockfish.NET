using Chess;
using Stockfish.NET.Models;
using Xunit;
using Xunit.Abstractions;

namespace Stockfish.NET.Tests
{
    public class TestStockfishMethods
    {
        private IStockfish Stockfish { get; }

        public TestStockfishMethods(ITestOutputHelper testOutputHelper)
        {
            var path = Utils.GetStockfishDir();
            Stockfish = new Core.Stockfish(path, depth: 2);
        }

        [Fact(Timeout = 2000)]
        public void TestGetBestMoveFirstMove()
        {
            Task<string> bestMove = Stockfish.GetBestMove(Utils.StartingFen());

            Assert.Contains(bestMove.Result, new List<string>
            {
                "e2e3",
                "e2e4",
                "g1f3",
                "b1c3",
                "d2d4"
            });
        }

        [Fact(Timeout = 2000)]
        public void TestGetBestMoveNotFirstMove()
        {
            ChessBoard chess = ChessBoard.LoadFromFen(Utils.StartingFen());
            var firstMoved = chess.Move("e2e4");
            var secondMoved = chess.Move("e7e6");

            Assert.True(firstMoved);
            Assert.True(secondMoved);

            var bestMove = Stockfish.GetBestMove(chess.ToFen());
            Assert.Contains(bestMove.Result, new List<string>
            {
                "d2d4", "g1f3"
            });
        }

        [Fact(Timeout = 2000)]
        public void TestGetBestMoveMate()
        {
            ChessBoard chess = ChessBoard.LoadFromFen(Utils.StartingFen());

            var firstMoved = chess.Move("f2f3");
            var secondMoved = chess.Move("e7e5");
            var thirdMoved = chess.Move("g2g4");
            var fourthMoved = chess.Move("d8h4");

            Assert.True(firstMoved);
            Assert.True(secondMoved);
            Assert.True(thirdMoved);
            Assert.True(fourthMoved);

            var bestMove = Stockfish.GetBestMove(chess.ToFen());
            Assert.Null(bestMove);
        }

        [Fact(Timeout = 2000)]
        public void TestSetFenPosition()
        {
            ChessBoard chess = ChessBoard.LoadFromFen("7r/1pr1kppb/2n1p2p/2NpP2P/5PP1/1P6/P6K/R1R2B2 w - - 1 27");

            var firstMove = chess.IsValidMove("f4f6");
            var secondMove = chess.IsValidMove("a1c1");
            
            Assert.True(firstMove);
            Assert.False(secondMove);
        }

        [Fact(Timeout = 2000)]
        public void TestGetEvaluationMate()
        {
            var fen = "6k1/p4p1p/6p1/5r2/3b4/6PP/4qP2/5RK1 b - - 14 36";

            var evaluation = Stockfish.GetEvaluation(fen);
            Assert.True(evaluation.Result < 0);
        }

        [Fact(Timeout = 2000)]
        public void TestGetEvaluationCP()
        {
            var fen = "r4rk1/pppb1p1p/2nbpqp1/8/3P4/3QBN2/PPP1BPPP/R4RK1 w - - 0 11";
            var evaluation = Stockfish.GetEvaluation(fen);

            Assert.True(evaluation.Result > 0);
        }

        [Fact(Timeout = 2000)]
        public void TestGetEvaluationStalemate()
        {
            var fen = "1nb1kqn1/pppppppp/8/6r1/5b1K/6r1/8/8 w - - 2 2";
            var evaluation = Stockfish.GetEvaluation(fen);

            Assert.True(evaluation.Result == 0);
        }
    }
}