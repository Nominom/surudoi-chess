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

		[Theory]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 1, 20)]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 2, 400)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 2, 1486)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62379)]
		[InlineData("rnBq1k1r/pp2bppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R b KQ - 0 8", 2, 1668)]
		[InlineData("rnBq1k1r/pp2bppp/2p5/8/2B5/3n4/PPP1N1PP/RNBQK2R w KQ - 1 9", 1, 5)]
		[InlineData("8/8/8/8/k2Pp2Q/8/8/3K4 b - d3 0 1", 1, 6)]
		[InlineData("8/8/8/2k5/3Pp3/8/8/3K4 b - d3 0 1", 1, 9)]
#if (!DEBUG)
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/P1B5/8/1PP1NnPP/RNBQK2R b KQ - 0 8", 4, 2101105)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 5, 89941194)]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 5, 4865609)]
		[InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 6, 119060324)]
		[InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4, 2103487)]
#endif
		public void PerftTests(string fen, int depth, ulong expectedNodes) {
			Options.HashSize = 128;
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


		[Theory]
		[InlineData("8/8/7B/8/3Pp2Q/k7/4K3/6R1 w - - 0 1", 8, 7)]
		[InlineData("8/4k3/2p5/Q2P4/3N4/8/1K1B4/6R1 w - - 0 1", 14, 13)]
		[InlineData("8/4k3/2p5/Q1bP4/3N2r1/4n3/1K1B1q2/6R1 b - - 0 1", 10, 5)]
		[InlineData("8/3p4/1p6/4k3/2K5/5P2/3P4/8 w - - 0 1", 2, 2)]
		[InlineData("8/3p4/1p6/4k3/2K5/5P2/3P4/8 b - - 0 1", 2, 2)]
		public void QuiescentMoveGenTests(string fen, int expectedNodes, int expectedChecks) {
			BitBoard board = BitBoard.FromFen(fen);

			Span<Move> moves = stackalloc Move[64];
			int numMoves = MoveGen.GenerateQuiescentSearchMoves(board, moves);
			int checks = 0;

			for (int i = 0; i < numMoves; i++) {
				output.WriteLine(moves[i].ToUciString());
				if (moves[i].IsCheck()) {
					checks++;
				}
			}

			Assert.Equal(expectedNodes, numMoves);
			Assert.Equal(expectedChecks, checks);
		}

		[Theory]
		[InlineData("8/8/7B/8/3Pp2Q/k7/4K3/6R1 w - - 0 1", 7)]
		[InlineData("8/4k3/2p5/Q2P4/3N4/8/1K1B4/6R1 w - - 0 1", 13)]
		[InlineData("8/4k3/2p5/Q1bP4/3N2r1/4n3/1K1B1q2/6R1 b - - 0 1", 5)]
		[InlineData("8/3p4/1p6/4k3/2K5/5P2/3P4/8 w - - 0 1", 2)]
		[InlineData("8/3p4/1p6/4k3/2K5/5P2/3P4/8 b - - 0 1", 2)]
		public void MoveGenCheckDetectionTests(string fen, int expectedChecks) {
			BitBoard board = BitBoard.FromFen(fen);

			Span<Move> moves = stackalloc Move[100];
			int numMoves = MoveGen.GenerateLegalMoves(board, moves);
			int checks = 0;

			for (int i = 0; i < numMoves; i++) {
				if (moves[i].IsCheck()) {
					output.WriteLine(moves[i].ToUciString());
					checks++;
				}
				
			}

			Assert.Equal(expectedChecks, checks);
		}

		[Fact]
		public void AttackMap() {
			BitBoard board = BitBoard.FromFen("5k2/8/3b4/8/8/1Q4r1/8/3K4 w - - 0 1");
			Assert.Equal(0x7274405462ff4040UL, board.bAttacks);
			Assert.Equal(0x4222120a077d1f1eUL, board.wAttacks);
		}

		[Fact]
		public void MoveOrdering() {
			BitBoard board = BitBoard.FromFen("8/4k3/2p5/Q1bP4/3N2r1/4n3/1K1B1q2/6R1 b - - 0 1");
			Span<Move> moves = stackalloc Move[100];
			int numMoves = MoveGen.GenerateLegalMoves(board, moves);
			MoveSorter.SortMoves(moves, numMoves);

			for (int i = 0; i < numMoves; i++) {
				output.WriteLine(moves[i].ToUciString());
			}
		}


	}
}
