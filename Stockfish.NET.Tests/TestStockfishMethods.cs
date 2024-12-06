using Chess;
using NUnit.Framework;
using Stockfish.NET.Models;

namespace Stockfish.NET.Tests
{
    public class TestStockfishMethods
    {
        private IStockfish Stockfish { get; set; }

        [SetUp]
        public void Instantiate()
        {
            var path = Utils.GetStockfishDir();

            var settings = new Settings();
            Stockfish = new Core.Stockfish(path, 10) {Settings = settings};
        }

        [TearDown()]
        public void Destroy()
        {
        }

        [Test]
        public async Task TestGetBestMoveFirstMove()
        {
            var bestMove = await Stockfish.GetBestMove(Utils.StartingFen());
            var bestMoves = new List<string>
            {
                "e2e3",
                "e2e4",
                "g1f3",
                "b1c3",
                "d2d4"
            };

            Assert.That(bestMoves.Contains(bestMove, StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public async Task TestGetBestMoveNotFirstMove()
        {
            ChessBoard chess = ChessBoard.LoadFromFen(Utils.StartingFen());
            var firstMoved = chess.Move("e2e4");
            var secondMoved = chess.Move("e7e6");

            Assert.That(firstMoved);
            Assert.That(secondMoved);

            ThreadPool.GetAvailableThreads(out int workingThreads, out int _);


            Assert.That(!chess.ToFen().Equals(Utils.StartingFen()));

            var bestMove = await Stockfish.GetBestMove(chess.ToFen());

            var bestMoves = new List<string>
            {
                "d2d4", "g1f3"
            };

            Assert.That(bestMoves.Contains(bestMove));
        }

        [Test]
        public async Task TestGetBestMoveMated()
        {
            ChessBoard chess = ChessBoard.LoadFromFen(@"rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 0 1");

            var bestMove = await Stockfish.GetBestMove(chess.ToFen());
            Assert.That(bestMove.Equals(FenError.Mate.ToStringByAttributes()));
        }

        [Test]
        public async Task TestBadFen()
        {
            var bestMove = await Stockfish.GetBestMove("garbagaagfbziubdiubqubf");

            Assert.That(bestMove.Equals(FenError.Bad.ToStringByAttributes()));
        }

        [Test]
        public async Task TestDownloadStockfish()
        {
            var settings = new Settings(threads: 1000, moveOverhead: 5000);

            using var stockfish = new Core.Stockfish() { Settings = settings };
            await stockfish.DownloadStockfish();
        }

        [Test]
        public async Task TestTwoStockfishesFirstMove()
        {
            var path = Utils.GetStockfishDir();

            var settings = new Settings();

            using var secondFish = new Core.Stockfish(path, 10) { Settings = settings };

            var bestMoves = new List<string>
            {
                "e2e3",
                "e2e4",
                "g1f3",
                "b1c3",
                "d2d4"
            };

            var bestMove = await Stockfish.GetBestMove(Utils.StartingFen());
            var secondFishBestMove = await secondFish.GetBestMove(Utils.StartingFen());

            Assert.That(bestMoves.Contains(bestMove, StringComparer.OrdinalIgnoreCase));
            Assert.That(bestMoves.Contains(secondFishBestMove, StringComparer.OrdinalIgnoreCase));
        }


        [Test]
        public void TestSetFenPosition()
        {
            ChessBoard chess = ChessBoard.LoadFromFen("7r/1pr1kppb/2n1p2p/2NpP2P/5PP1/1P6/P6K/R1R2B2 w - - 1 27");

            var firstMove = chess.IsValidMove("f4f6");
            var secondMove = chess.IsValidMove("a1c1");
            
            Assert.That(firstMove);
            Assert.That(!secondMove);
        }

        [Test]
        public async Task TestGetEvaluationMate()
        {
            var fen = "6k1/p4p1p/6p1/5r2/3b4/6PP/4qP2/5RK1 b - - 14 36";

            var evaluation = await Stockfish.GetEvaluation(fen);
            Assert.That(evaluation < 0);
        }

        [Test]
        public async Task TestGetEvaluationCP()
        {
            var fen = "r4rk1/pppb1p1p/2nbpqp1/8/3P4/3QBN2/PPP1BPPP/R4RK1 w - - 0 11";
            var evaluation = await Stockfish.GetEvaluation(fen);

            Assert.That(evaluation > 0);
        }

        [Test]
        public async Task TestGetEvaluationStalemate()
        {
            var fen = "1nb1kqn1/pppppppp/8/6r1/5b1K/6r1/8/8 w - - 2 2";
            var evaluation = await Stockfish.GetEvaluation(fen);

            Assert.That(evaluation == 0);
        }
    }
}