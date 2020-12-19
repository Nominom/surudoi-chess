using System;
using System.Collections.Generic;
using System.Text;
using ChessEngine;
using Xunit;
using Xunit.Abstractions;

namespace ChessTests
{
	public class MoveGenTests
	{

		private readonly ITestOutputHelper output;

		public MoveGenTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void FirstTurnMoves() {
			BitBoard board = BitBoard.InitialPosition();

			using var moves = MoveGen.GeneratePseudoLegalMoves(board);

			foreach (Move move in moves) {
				output.WriteLine(move.ToUciString());
			}
		}

		[Theory]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 1, 20)]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 2, 400)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 2, 1486)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62379)]
#if (!DEBUG)
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/P1B5/8/1PP1NnPP/RNBQK2R b KQ - 0 8", 4, 2101105)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 5, 89941194)]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 5, 4865609)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4, 2103487)]
#endif
		public void PerftTests(string fen, int depth, ulong expectedNodes) {
			BitBoard board = BitBoard.FromFen(fen);

			var perftList = board.PerftList(depth);
			ulong perftResult = 0UL;

			foreach((var mv, var ul) in perftList)
			{
				output.WriteLine($"{mv}: {ul}");
				perftResult += ul;
			}

			Assert.Equal(expectedNodes, perftResult);
		}

		[Fact]
		public void AttackMap() {
			BitBoard board = BitBoard.FromFen("5k2/8/3b4/8/8/1Q4r1/8/3K4 w - - 0 1");
			Assert.Equal(0x7274405462ff4040UL, board.bAttacks);
			Assert.Equal(0x4222120a077d1f1eUL, board.wAttacks);
		}

	}
}
