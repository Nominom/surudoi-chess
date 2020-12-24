using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine
{
	public static class MoveSorter
	{

		private static int MoveScore(Move move) {
			int attackerScore = move.piece.MvvLvaScore();
			int victimScore = 0;
			int promoScore = 0;
			int extraScore = 0;
			if (move.IsPromotion()) {
				promoScore = move.promoteTo.MvvLvaScore();
			}
			if (move.IsCapture()) {
				victimScore = move.cPiece.MvvLvaScore();
			}
			if (move.IsCheck()) {
				extraScore = 1000;
			}
			if (move.IsCastling()) {
				extraScore = 700;
			}

			return (victimScore - attackerScore) + promoScore + extraScore;
		}

		public static Move SelectNext(Span<Move> moves, int numMoves, ref int index) {

			if (index == numMoves - 1) return moves[index];

			int maxIdx = index;
			int maxScore = int.MinValue;

			for (int i = index; i < numMoves; i++) {
				int score = MoveScore(moves[i]);
				if (score > maxScore) {
					maxScore = score;
					maxIdx = i;
				}
			}

			// Swap max move with index
			Move max = moves[maxIdx];
			moves[maxIdx] = moves[index];
			moves[index] = max;

			index++;

			return max;
		}

		public static void SortMoves(Span<Move> moves, int numMoves) {
			for (int i = 0; i < numMoves - 1;) {
				SelectNext(moves, numMoves, ref i);
			}
		}
	}
}
