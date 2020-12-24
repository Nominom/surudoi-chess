using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine
{
	public static class Zorbrist {
		private const ulong seed = 0x4d4c80dacef14603UL;
		public static ulong[] squarePieceTable;
		public static ulong[] castlingTable;
		public static ulong[] enPassantTable;
		public static ulong sideToMove;

		static Zorbrist() {
			XorShift random = new XorShift(seed);

			squarePieceTable = new ulong[14 * 64];
			castlingTable = new ulong[16];
			enPassantTable = new ulong[8];
			sideToMove = random.Next();

			for (int i = 0; i < squarePieceTable.Length; i++) {
				squarePieceTable[i] = random.Next();
			}
			for (int i = 0; i < castlingTable.Length; i++) {
				castlingTable[i] = random.Next();
			}
			for (int i = 0; i < enPassantTable.Length; i++) {
				enPassantTable[i] = random.Next();
			}
		}

		public static ulong Rebuild(in BitBoard board) {
			ulong hash = board.sideToMove == Color.Black ? sideToMove : 0;
			hash ^= castlingTable[board.castling];

			if (!board.enPassantTargetSquare.IsNoneSquare) {
				hash ^= enPassantTable[board.enPassantTargetSquare & 0b111];
			}

			for (int i = 0; i < 64; i++) {
				if (board.mailbox[i] != Piece.Empty) {
					hash ^= squarePieceTable[(int) board.mailbox[i] * 64 + i];
				}
			}

			return hash;
		}
	}
}
