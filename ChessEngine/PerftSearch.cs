using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ChessEngine
{
	public class PerftSearch : ISearch
	{
		private bool shouldStop = false;

		public Action<string> Output { get; set; }
		public Thread RunningThread { get; set; }
		public bool DisableOutput { get; set; }

		private ulong Perft(ref BitBoard board, int depth, PerftHashTable hashTable)
		{
			if (shouldStop) return 0;
			ulong nodes = 0;

			if (depth <= 0)
				return 1UL;

			if (hashTable.TryLoad(board.hash, depth, out nodes))
			{
				return nodes;
			}

			Span<Move> moves = stackalloc Move[218];
			int count = MoveGen.GenerateLegalMoves(board, moves);

#if DEBUG
			if (Debugger.IsAttached)
			{
				Span<Move> psMoves = stackalloc Move[218];
				int pseudoCount = MoveGen.GeneratePseudoLegalMoves(board, psMoves);
				for (int i = 0; i < pseudoCount; i++)
				{
					var unMove = board.MakeMove(psMoves[i]);
					if (board.IsInMate())
					{
						psMoves[i--] = psMoves[--pseudoCount];
					}
					board.UnMakeMove(unMove);
				}
				if (pseudoCount != count)
				{
					Debugger.Break();
				}
			}
#endif

			if (depth == 1)
				return (ulong)count;

			for (int i = 0; i < count; i++)
			{
				if (shouldStop) return 0;
				var unMove = board.MakeMove(moves[i]);
#if DEBUG
				if (!board.IsValid) throw new Exception("Board state is not valid!");
#endif
				nodes += Perft(ref board, depth - 1, hashTable);

				board.UnMakeMove(unMove);

			}

			hashTable.SaveItem(board.hash, depth, nodes);

			return nodes;
		}


		public void Run(BitBoard startingPosition, SearchOptions options)
		{
			BitBoard board = startingPosition;
			PerftHashTable hashTable = new PerftHashTable();
			var moves = MoveGen.GenerateMoveList(board);
			ulong allNodes = 0;
			for (int i = 0; i < moves.Count; i++)
			{
				var unMove = board.MakeMove(moves[i]);
				ulong nodes = Perft(ref board, options.depth - 1, hashTable);
				if (shouldStop) break;
				allNodes += nodes;
				if (!DisableOutput) Output($"{moves[i]}: {nodes}");
				board.UnMakeMove(unMove);
			}

			if (!DisableOutput) Output($"Nodes searched: {allNodes}");
		}

		public void Stop()
		{
			shouldStop = true;
		}
	}
}
