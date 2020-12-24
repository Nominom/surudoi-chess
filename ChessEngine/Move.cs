using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessEngine
{
	public struct Move
	{
		public SquareIndex startSquareIdx;
		public SquareIndex endSquareIdx;
		public Piece piece;
		public Piece cPiece;
		public Piece promoteTo;
		public MoveType moveType;

		public ulong GetStartSquareMask()
		{
			return 1UL << startSquareIdx.value;
		}

		public ulong GetEndSquareMask()
		{
			return 1UL << endSquareIdx.value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public Color GetColor() { return (Color)((int)piece & 1); }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public Color GetCapColor() { return (Color)((int)cPiece & 1); }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsQuiet() { return moveType == MoveType.Quiet; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsCapture() { return (moveType & MoveType.Capture) != 0; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsEnPassant() { return (moveType & MoveType.EnPassant) != 0; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsCastling() { return (moveType & MoveType.Castling) != 0; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsKingSideCastle() { return (moveType & MoveType.KingCastle) == MoveType.KingCastle; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsQueenSideCastle() { return (moveType & MoveType.QueenCastle) == MoveType.QueenCastle; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsPromotion() { return (moveType & MoveType.Promotion) != 0; }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsDoublePush() { return (moveType & MoveType.DoublePush) != 0; }

		/*
		 * Is not 100% accurate. Only set when a move explicitly creates a check, eg. not discovered check
		 */
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public bool IsCheck() { return (moveType & MoveType.Check) != 0; }

		public string ToUciString()
		{
			if (IsPromotion())
			{
				return startSquareIdx.ToString() + endSquareIdx.ToString() + promoteTo.ToLowerCasePieceName();
			}
			else
			{
				return startSquareIdx.ToString() + endSquareIdx.ToString();
			}
		}

		public override string ToString()
		{
			return ToUciString();
		}

		public static Move FromUciString(string uciMove, BitBoard board)
		{
			if ((board.castling & BitBoard.wKingSideCastling) != 0 && uciMove == "e1g1")
			{
				return CreateCastle(false, Color.White);
			}
			if ((board.castling & BitBoard.wQueenSideCastling) != 0 && uciMove == "e1c1")
			{
				return CreateCastle(true, Color.White);
			}
			if ((board.castling & BitBoard.bKingSideCastling) != 0 && uciMove == "e8g8")
			{
				return CreateCastle(false, Color.Black);
			}
			if ((board.castling & BitBoard.bQueenSideCastling) != 0 && uciMove == "e8c8")
			{
				return CreateCastle(true, Color.Black);
			}

			var span = uciMove.AsSpan();
			var startPos = SquareIndex.Parse(span.Slice(0, 2));
			var endPos = SquareIndex.Parse(span.Slice(2, 2));

			Piece movedPiece = board.mailbox[startPos];
			Color movedColor = movedPiece.Color();
			Piece capturePiece = board.mailbox[endPos];

			bool capture = capturePiece != Piece.Empty;

			if (span.Length == 5)
			{
				switch (span[4])
				{
					case 'q':
						return CreatePromotion(startPos, endPos, movedPiece,
							movedColor == Color.White ? Piece.WQueen : Piece.BQueen,
							false, capture, capturePiece);
					case 'r':
						return CreatePromotion(startPos, endPos, movedPiece,
							movedColor == Color.White ? Piece.WRook : Piece.BRook,
							false, capture, capturePiece);
					case 'b':
						return CreatePromotion(startPos, endPos, movedPiece,
							movedColor == Color.White ? Piece.WBishop : Piece.BBishop,
							false, capture, capturePiece);
					case 'n':
						return CreatePromotion(startPos, endPos, movedPiece,
							movedColor == Color.White ? Piece.WKnight : Piece.BKnight,
							false, capture, capturePiece);
					default:
						throw new ArgumentException("Promotion piece in uciMove is invalid");
				}
			}

			if ((movedPiece == Piece.WPawn || movedPiece == Piece.BPawn) && capturePiece == Piece.Empty &&
				endPos == board.enPassantTargetSquare)
			{
				//en passant
				return CreateEnPassant(startPos, endPos, movedPiece,
					movedColor == Color.White ? Piece.BPawn : Piece.WPawn);
			}

			if (capture)
			{
				return CreateCapture(startPos, endPos, movedPiece, capturePiece);
			}

			if ((movedPiece == Piece.WPawn || movedPiece == Piece.BPawn) && Math.Abs(startPos.Rank - endPos.Rank) > 1)
			{
				return CreatePawnDouble(startPos, endPos, movedPiece);
			}

			return CreateQuiet(startPos, endPos, movedPiece);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static Move CreateQuiet(SquareIndex start, SquareIndex end, Piece piece, bool isCheck = false)
		{
			Move move;
			move.moveType = isCheck ? MoveType.Check : MoveType.Quiet;
			move.cPiece = Piece.Empty;
			move.promoteTo = Piece.Empty;

			move.startSquareIdx = start;
			move.endSquareIdx = end;
			move.piece = piece;

			return move;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static Move CreatePawnDouble(SquareIndex start, SquareIndex end, Piece piece, bool isCheck = false)
		{
			Move move;
			move.moveType = MoveType.DoublePush | (isCheck ? MoveType.Check : MoveType.Quiet);
			move.cPiece = Piece.Empty;
			move.promoteTo = Piece.Empty;

			move.startSquareIdx = start;
			move.endSquareIdx = end;
			move.piece = piece;

			return move;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static Move CreateCapture(SquareIndex start, SquareIndex end, Piece piece, Piece captured, bool isCheck = false)
		{
			Move move;
			move.moveType = MoveType.Capture | (isCheck ? MoveType.Check : MoveType.Quiet);
			move.cPiece = captured;
			move.promoteTo = Piece.Empty;

			move.startSquareIdx = start;
			move.endSquareIdx = end;
			move.piece = piece;

			return move;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static Move CreateEnPassant(SquareIndex start, SquareIndex end, Piece piece, Piece capPiece, bool isCheck = false)
		{
			Move move;
			move.moveType = MoveType.EnPassant | (isCheck ? MoveType.Check : MoveType.Quiet);
			move.cPiece = capPiece;
			move.promoteTo = Piece.Empty;

			move.startSquareIdx = start;
			move.endSquareIdx = end;
			move.piece = piece;

			return move;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static Move CreatePromotion(SquareIndex start, SquareIndex end, Piece piece, Piece promotionPiece, bool isCheck = false, bool capture = false, Piece capPiece = Piece.Empty)
		{
			Move move;
			move.promoteTo = promotionPiece;
			move.startSquareIdx = start;
			move.endSquareIdx = end;
			move.piece = piece;
			move.cPiece = capPiece;

			if (capture)
			{
				move.moveType = MoveType.PromotionCap | (isCheck ? MoveType.Check : MoveType.Quiet);
			}
			else
			{
				move.moveType = MoveType.Promotion | (isCheck ? MoveType.Check : MoveType.Quiet);
			}

			return move;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static Move CreateCastle(bool queenSide, Color color, bool isCheck = false)
		{
			Move move;
			move.moveType = (queenSide ? MoveType.QueenCastle : MoveType.KingCastle)
							| (isCheck ? MoveType.Check : MoveType.Quiet);
			move.cPiece = Piece.Empty;
			move.promoteTo = Piece.Empty;

			if (color == Color.White)
			{
				move.piece = Piece.WKing;
				move.startSquareIdx = SquareIndex.E1;
				move.endSquareIdx = queenSide ? SquareIndex.C1 : SquareIndex.G1;
			}
			else
			{
				move.piece = Piece.BKing;
				move.startSquareIdx = SquareIndex.E8;
				move.endSquareIdx = queenSide ? SquareIndex.C8 : SquareIndex.G8;
			}

			return move;
		}
	}

	public struct UnMove
	{
		public byte castling;
		public SquareIndex enPassantTargetSquare;
		public Move move;
		public int halfMovesSinceCapOrPawn;
		public ulong wStaticAttacks;
		public ulong wSliderAttacks;
		public ulong bStaticAttacks;
		public ulong bSliderAttacks;
		public ulong hash;
	}
}
