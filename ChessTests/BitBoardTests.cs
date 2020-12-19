using System;
using ChessEngine;
using Xunit;

namespace ChessTests
{
	public class BitBoardTests
	{
		[Fact]
		public void TestStartingPosition()
		{
			BitBoard board = BitBoard.InitialPosition();
			Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.ToFen());
			Assert.True(board.CheckState());
		}

		[Fact]
		public void TestStartPosFen()
		{
			BitBoard board = BitBoard.FromFen("startpos");
			Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.ToFen());
			Assert.True(board.CheckState());
		}

		[Theory]
		//[InlineData("3b4/1n4p1/1k2B1K1/1p5P/p1P5/P7/1P3P2/qr2B3 w - 10 40")]
		[InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq d6 1 2")]
		[InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b - e3 1 2")]
		[InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPP2PPP/RNBQKB1R w - - 1 3")]
		public void TestSameFen(string fen)
		{
			BitBoard board = BitBoard.FromFen(fen);
			Assert.Equal(fen, board.ToFen());
			Assert.True(board.CheckState());
		}
	}
}
