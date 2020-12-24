using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ChessEngine;

namespace ChessBenchmarks
{
	[SimpleJob(RuntimeMoniker.NetCoreApp31)]
	public class MoveGenBenchmark
	{
		[GlobalSetup]
		public void Setup() {
			board = BitBoard.FromFen("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
		}

		private BitBoard board;


		[Benchmark]
		public ulong StackAllocMoveGenerator() {
			Span<Move> moves = stackalloc Move[218];
			int count = MoveGen.GenerateLegalMoves(board, moves);
			ulong nodes = 0;

			for (int i = 0; i < count; i++)
			{
				var unMove = board.MakeMove(moves[i]);
				nodes += 1;
				board.UnMakeMove(unMove);
			}

			return nodes;
		}
	}
}
