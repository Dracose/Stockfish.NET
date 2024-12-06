namespace Stockfish.NET.Models
{
    public class Settings
    {
        public int Contempt { get; set; }
        public int Threads { get; set; }
        public bool Ponder { get; set; }
        public int MultiPV { get; set; }
        public int SkillLevel { get; set; }
        public int MoveOverhead { get; set; }
        public bool UCIChess960 { get; set; }

        public Settings(
            int contempt = 0,
            int threads = 0,
            bool ponder = false,
            int multiPV = 1,
            int skillLevel = 20,
            int moveOverhead = 10,
            bool uciChess960 = false
        )
        {
            Contempt = contempt;
            Ponder = ponder;
            Threads = threads;
            MultiPV = multiPV;
            SkillLevel = skillLevel;
            MoveOverhead = moveOverhead;
            UCIChess960 = uciChess960;
        }

        public Dictionary<string, string> GetPropertiesAsDictionary()
        {
            return new Dictionary<string, string>
            {
                ["Contempt"] = Contempt.ToString(),
                ["Threads"] = Threads.ToString(),
                ["Ponder"] = Ponder.ToString(),
                ["MultiPV"] = MultiPV.ToString(),
                ["Skill Level"] = SkillLevel.ToString(),
                ["Move Overhead"] = MoveOverhead.ToString(),
                ["UCI_Chess960"] = UCIChess960.ToString(),
            };
        }
    }
}