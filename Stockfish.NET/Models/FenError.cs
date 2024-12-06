using System.Runtime.Serialization;

namespace Stockfish.NET.Models
{
    public enum FenError
    {
        [EnumMember(Value = "Mate")]
        Mate = 0,
        [EnumMember(Value = "Okay")]
        Okay = 1,
        [EnumMember(Value = "Bad")]
        Bad = 2,
        [EnumMember(Value = "Unread")]
        Unread = -1
    }
}
