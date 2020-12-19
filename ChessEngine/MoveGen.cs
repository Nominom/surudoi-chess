using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;


namespace ChessEngine
{

	public static class MoveGen
	{
		private const int sliderCountForAvx = 2;
		public static ulong[] kingAttacks = MoveGenHelpers.GenerateKingAttacks();
		public static ulong[] knightAttacks = MoveGenHelpers.GenerateKnightAttacks();

		public static PooledList<Move> GeneratePseudoLegalMoves(in BitBoard board)
		{
			PooledList<Move> moves = PooledList<Move>.Create();

			GeneratePawnAttacks(board, moves);
			GeneratePawnPushes(board, moves);
			GenerateKingMoves(board, moves);
			GenerateKnightMoves(board, moves);
			GenerateRookMoves(board, moves, (Piece)((int)Piece.WRook + (int)board.sideToMove));
			GenerateBishopMoves(board, moves, (Piece)((int)Piece.WBishop + (int)board.sideToMove));
			GenerateRookMoves(board, moves, (Piece)((int)Piece.WQueen + (int)board.sideToMove));
			GenerateBishopMoves(board, moves, (Piece)((int)Piece.WQueen + (int)board.sideToMove));

			return moves;
		}

		public static PooledList<Move> GenerateOnlyLegalMoves(ref BitBoard board)
		{
			PooledList<Move> moves = GeneratePseudoLegalMoves(board);

			for (int i = 0; i < moves.Count; i++)
			{
				var unMove = board.MakeMove(moves[i]);
				if (!unMove.wasLegal)
				{
					moves.RemoveAt(i);
					i--;
				}
				board.UnMakeMove(unMove);
			}

			return moves;
		}

		public static (ulong, ulong, ulong) GenerateWhiteAttackMaps(in BitBoard board)
		{
			ulong wStaticAttacks = GenerateWhiteStaticAttackMaps(board);
			ulong wSliderAttacks = GenerateWhiteSliderAttackMaps(board);
			ulong wAttacks = wStaticAttacks | wSliderAttacks;

			return (wAttacks, wStaticAttacks, wSliderAttacks);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static ulong GenerateWhiteStaticAttackMaps(in BitBoard board)
		{
			ulong wAttacks = 0;

			var wPawns = board.pieces[(int)Piece.WPawn];
			wAttacks |= MoveGenHelpers.wPawnAnyAttacks(wPawns);

			int wKingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing]);
			wAttacks |= kingAttacks[wKingSquare];

			if (Avx2.IsSupported)
			{
				if (board.pieces[(int)Piece.WKnight] != 0)
				{
					wAttacks |= Avx2KnightAttacks(board.pieces[(int)Piece.WKnight]);
				}
			}
			else
			{
				var knights = board.pieces[(int)Piece.WKnight];
				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;
					var knightAttackPattern = knightAttacks[knightSquare];
					wAttacks |= knightAttackPattern;
				}
			}
			return wAttacks;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static ulong GenerateWhiteSliderAttackMaps(in BitBoard board)
		{
			ulong wAttacks = 0;

			ulong wsliders = board.pieces[(int)Piece.WQueen] | board.pieces[(int)Piece.WRook] |
							 board.pieces[(int)Piece.WBishop];
			if (wsliders != 0)
			{
				int sliderCount = BitOperations.PopCount(wsliders);
				if (Avx2.IsSupported && sliderCount > sliderCountForAvx)
				{
					wAttacks |= Avx2Dumb7FillAttacks(board.pieces[(int)Piece.WQueen],
						board.pieces[(int)Piece.WRook],
						board.pieces[(int)Piece.WBishop], board.empty);

				}
				else
				{
					var rooks = board.pieces[(int)Piece.WRook] | board.pieces[(int)Piece.WQueen];
					while (rooks != 0)
					{
						SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
						ulong rookMask = rookSquare.ToMask();
						rooks ^= rookMask;
						var rookAttackPattern = Magic.RookAttacks(rookSquare, board.occupied);
						wAttacks |= rookAttackPattern;
					}

					var bishops = board.pieces[(int)Piece.WBishop] | board.pieces[(int)Piece.WQueen];
					while (bishops != 0)
					{
						SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
						ulong bishopMask = bishopSquare.ToMask();
						bishops ^= bishopMask;
						var bishopAttackPattern = Magic.BishopAttacks(bishopSquare, board.occupied);
						wAttacks |= bishopAttackPattern;
					}
				}
			}
			return wAttacks;
		}

		public static (ulong, ulong, ulong) GenerateBlackAttackMaps(in BitBoard board)
		{
			ulong bStaticAttacks = GenerateBlackStaticAttackMaps(board);
			ulong bSliderAttacks = GenerateBlackSliderAttackMaps(board);
			ulong bAttacks = bStaticAttacks | bSliderAttacks;

			return (bAttacks, bStaticAttacks, bSliderAttacks);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static ulong GenerateBlackStaticAttackMaps(in BitBoard board)
		{
			ulong bAttacks = 0;

			var bPawns = board.pieces[(int)Piece.BPawn];
			bAttacks |= MoveGenHelpers.bPawnAnyAttacks(bPawns);

			int bKingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.BKing]);
			bAttacks |= kingAttacks[bKingSquare];

			if (Avx2.IsSupported)
			{
				if (board.pieces[(int)Piece.BKnight] != 0)
				{
					bAttacks |= Avx2KnightAttacks(board.pieces[(int)Piece.BKnight]);
				}
			}
			else
			{
				var knights = board.pieces[(int)Piece.BKnight];
				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;
					var knightAttackPattern = knightAttacks[knightSquare];
					bAttacks |= knightAttackPattern;
				}
			}

			return bAttacks;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static ulong GenerateBlackSliderAttackMaps(in BitBoard board)
		{
			ulong bAttacks = 0;
			ulong bsliders = board.pieces[(int)Piece.BQueen] | board.pieces[(int)Piece.BRook] |
							 board.pieces[(int)Piece.BBishop];
			if (bsliders != 0)
			{
				int sliderCount = BitOperations.PopCount(bsliders);
				if (Avx2.IsSupported && sliderCount > sliderCountForAvx)
				{

					bAttacks |= Avx2Dumb7FillAttacks(board.pieces[(int)Piece.BQueen],
						board.pieces[(int)Piece.BRook],
						board.pieces[(int)Piece.BBishop], board.empty);
				}
				else
				{
					var rooks = board.pieces[(int)Piece.BRook] | board.pieces[(int)Piece.BQueen];
					while (rooks != 0)
					{
						SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
						ulong rookMask = rookSquare.ToMask();
						rooks ^= rookMask;
						var rookAttackPattern = Magic.RookAttacks(rookSquare, board.occupied);
						bAttacks |= rookAttackPattern;
					}

					var bishops = board.pieces[(int)Piece.BBishop] | board.pieces[(int)Piece.BQueen];
					while (bishops != 0)
					{
						SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
						ulong bishopMask = bishopSquare.ToMask();
						bishops ^= bishopMask;
						var bishopAttackPattern = Magic.BishopAttacks(bishopSquare, board.occupied);
						bAttacks |= bishopAttackPattern;
					}
				}
			}

			return bAttacks;
		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static ulong Avx2KnightAttacks(ulong knights)
		{
			Vector256<ulong> qmask = Vector256.Create(
				MoveGenHelpers.notABFile, MoveGenHelpers.notAFile,
				MoveGenHelpers.notHFile, MoveGenHelpers.notGHFile);
			Vector256<ulong> qshift = Vector256.Create(10UL, 17UL, 15UL, 6UL);
			Vector256<ulong> qKnights = Vector256.Create(knights);

			Vector256<ulong> knight_noEaEa_noNoEa_noNoWe_noWeWe =
				Avx2.And(Avx2.ShiftLeftLogicalVariable(qKnights, qshift), qmask);

			qmask = Vector256.Create(
				MoveGenHelpers.notGHFile, MoveGenHelpers.notHFile,
				MoveGenHelpers.notAFile, MoveGenHelpers.notABFile);
			Vector256<ulong> knight_soWeWe_soSoWe_soSoEa_soEaEa =
				Avx2.And(Avx2.ShiftRightLogicalVariable(qKnights, qshift), qmask);

			Vector256<ulong> attacks =
				Avx2.Or(knight_noEaEa_noNoEa_noNoWe_noWeWe,
					knight_soWeWe_soSoWe_soSoEa_soEaEa);

			return attacks.GetElement(0) | attacks.GetElement(1) |
						attacks.GetElement(2) | attacks.GetElement(3);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static ulong Avx2Dumb7FillAttacks(ulong queens, ulong rooks, ulong bishops, ulong empty)
		{
			ulong rq = rooks | queens;
			ulong bq = bishops | queens;
			Vector256<ulong> qsliders = Vector256.Create(rq, rq, bq, bq);
			Vector256<ulong> qslidersCopy = qsliders;
			Vector256<ulong> qmask = Vector256.Create(
				MoveGenHelpers.notAFile, ~0UL,
				MoveGenHelpers.notHFile, MoveGenHelpers.notAFile);
			Vector256<ulong> qshift = Vector256.Create(1UL, 8UL, 7UL, 9UL);
			Vector256<ulong> qflood = qsliders;

			Vector256<ulong> qempty = Avx2.And(Vector256.Create(empty), qmask);

			//east_nort_noWe_noEa
			qsliders = Avx2.And(Avx2.ShiftLeftLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftLeftLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftLeftLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftLeftLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftLeftLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qflood = Avx2.Or(qflood, Avx2.And(Avx2.ShiftLeftLogicalVariable(qsliders, qshift), qempty));
			var attacks_east_nort_noWe_noEa = Avx2.And(Avx2.ShiftLeftLogicalVariable(qflood, qshift), qmask);


			qmask = Vector256.Create(
				MoveGenHelpers.notHFile, ~0UL,
				MoveGenHelpers.notAFile, MoveGenHelpers.notHFile);
			qsliders = qslidersCopy;
			qflood = qsliders;

			qempty = Avx2.And(Vector256.Create(empty), qmask);

			//west_sout_soEa_soWe
			qsliders = Avx2.And(Avx2.ShiftRightLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftRightLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftRightLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftRightLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qsliders = Avx2.And(Avx2.ShiftRightLogicalVariable(qsliders, qshift), qempty);
			qflood = Avx2.Or(qflood, qsliders);
			qflood = Avx2.Or(qflood, Avx2.And(Avx2.ShiftRightLogicalVariable(qsliders, qshift), qempty));
			var attacks_west_sout_soEa_soWe = Avx2.And(Avx2.ShiftRightLogicalVariable(qflood, qshift), qmask);

			var attacks = Avx2.Or(attacks_east_nort_noWe_noEa, attacks_west_sout_soEa_soWe);

			return attacks.GetElement(0) | attacks.GetElement(1) |
				   attacks.GetElement(2) | attacks.GetElement(3);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GeneratePawnPushes(in BitBoard board, PooledList<Move> moves)
		{

			if (board.sideToMove == Color.White)
			{

				var wPawns = board.pieces[(int)Piece.WPawn];

				var wPawns1Push = MoveGenHelpers.wPawnsAble2Push(wPawns, board.empty);

				while (wPawns1Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(wPawns1Push);
					wPawns1Push ^= nextIndex.ToMask();

					SquareIndex endPos = nextIndex.OneNorth();
					ulong endMask = endPos.ToMask();

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WQueen));
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WRook));
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WBishop));
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WKnight));
					}
					else
					{
						Move m = Move.CreateQuiet(nextIndex, endPos, Piece.WPawn);
						moves.Add(m);
					}
				}


				var wPawns2Push = MoveGenHelpers.wPawnsAble2DblPush(wPawns, board.empty);
				while (wPawns2Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(wPawns2Push);
					wPawns2Push ^= nextIndex.ToMask();

					SquareIndex endPos = nextIndex.TwoNorth();

					Move m = Move.CreatePawnDouble(nextIndex, endPos, Piece.WPawn);
					moves.Add(m);
				}
			}
			else
			{
				var bPawns = board.pieces[(int)Piece.BPawn];

				var bPawns1Push = MoveGenHelpers.bPawnsAble2Push(bPawns, board.empty);

				while (bPawns1Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(bPawns1Push);
					bPawns1Push ^= nextIndex.ToMask();

					SquareIndex endPos = nextIndex.OneSouth();
					ulong endMask = endPos.ToMask();

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BQueen));
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BRook));
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BBishop));
						moves.Add(Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BKnight));
					}
					else
					{
						Move m = Move.CreateQuiet(nextIndex, endPos, Piece.BPawn);
						moves.Add(m);
					}
				}


				var bPawns2Push = MoveGenHelpers.bPawnsAble2DblPush(bPawns, board.empty);
				while (bPawns2Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(bPawns2Push);
					bPawns2Push ^= nextIndex.ToMask();

					SquareIndex endPos = nextIndex.TwoSouth();

					Move m = Move.CreatePawnDouble(nextIndex, endPos, Piece.BPawn);
					moves.Add(m);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GeneratePawnAttacks(in BitBoard board, PooledList<Move> moves)
		{

			if (!board.enPassantTargetSquare.IsNoneSquare)
			{
				ulong enPassantMask = board.enPassantTargetSquare.ToMask();

				if (board.sideToMove == Color.White)
				{
					var wPawns = board.pieces[(int)Piece.WPawn];
					var wPawnAbleToWest = MoveGenHelpers.wPawnsAble2CaptureWest(wPawns, enPassantMask);
					if (wPawnAbleToWest != 0)
					{
						var startIdx = BitOperations.TrailingZeroCount(wPawnAbleToWest);
						moves.Add(Move.CreateEnPassant(startIdx, board.enPassantTargetSquare, Piece.WPawn, Piece.BPawn));
					}
					var wPawnAbleToEast = MoveGenHelpers.wPawnsAble2CaptureEast(wPawns, enPassantMask);
					if (wPawnAbleToEast != 0)
					{
						var startIdx = BitOperations.TrailingZeroCount(wPawnAbleToEast);
						moves.Add(Move.CreateEnPassant(startIdx, board.enPassantTargetSquare, Piece.WPawn, Piece.BPawn));
					}
				}
				else
				{
					var bPawns = board.pieces[(int)Piece.BPawn];
					var bPawnAbleToWest = MoveGenHelpers.bPawnsAble2CaptureWest(bPawns, enPassantMask);
					if (bPawnAbleToWest != 0)
					{
						var startIdx = BitOperations.TrailingZeroCount(bPawnAbleToWest);
						moves.Add(Move.CreateEnPassant(startIdx, board.enPassantTargetSquare, Piece.BPawn, Piece.WPawn));
					}
					var bPawnAbleToEast = MoveGenHelpers.bPawnsAble2CaptureEast(bPawns, enPassantMask);
					if (bPawnAbleToEast != 0)
					{
						var startIdx = BitOperations.TrailingZeroCount(bPawnAbleToEast);
						moves.Add(Move.CreateEnPassant(startIdx, board.enPassantTargetSquare, Piece.BPawn, Piece.WPawn));
					}
				}
			}

			if (board.sideToMove == Color.White)
			{
				var wPawns = board.pieces[(int)Piece.WPawn];
				var bPieces = board.pieces[(int)Piece.BlackAll];

				var wPawnsAbleToWest = MoveGenHelpers.wPawnsAble2CaptureWest(wPawns, bPieces);

				while (wPawnsAbleToWest != 0)
				{
					SquareIndex startIndex = BitOperations.TrailingZeroCount(wPawnsAbleToWest);
					ulong startMask = startIndex.ToMask();
					wPawnsAbleToWest ^= startMask;

					ulong endMask = MoveGenHelpers.NoWeOne(startMask);
					SquareIndex endIndex = BitOperations.TrailingZeroCount(endMask);

					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WQueen, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WRook, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WBishop, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WKnight, true, capPiece));
					}
					else
					{
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.WPawn, capPiece);
						moves.Add(move);
					}
				}

				var wPawnsAbleToEast = MoveGenHelpers.wPawnsAble2CaptureEast(wPawns, bPieces);

				while (wPawnsAbleToEast != 0)
				{
					SquareIndex startIndex = BitOperations.TrailingZeroCount(wPawnsAbleToEast);
					ulong startMask = startIndex.ToMask();
					wPawnsAbleToEast ^= startMask;

					ulong endMask = MoveGenHelpers.NoEaOne(startMask);
					SquareIndex endIndex = BitOperations.TrailingZeroCount(endMask);

					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WQueen, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WRook, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WBishop, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WKnight, true, capPiece));
					}
					else
					{
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.WPawn, capPiece);
						moves.Add(move);
					}
				}
			}
			else
			{
				var bPawns = board.pieces[(int)Piece.BPawn];
				var wPieces = board.pieces[(int)Piece.WhiteAll];

				var bPawnsAbleToWest = MoveGenHelpers.bPawnsAble2CaptureWest(bPawns, wPieces);

				while (bPawnsAbleToWest != 0)
				{
					SquareIndex startIndex = BitOperations.TrailingZeroCount(bPawnsAbleToWest);
					ulong startMask = startIndex.ToMask();
					bPawnsAbleToWest ^= startMask;

					ulong endMask = MoveGenHelpers.SoWeOne(startMask);
					SquareIndex endIndex = BitOperations.TrailingZeroCount(endMask);

					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BQueen, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BRook, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BBishop, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BKnight, true, capPiece));
					}
					else
					{
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.BPawn, capPiece);
						moves.Add(move);
					}
				}

				var bPawnsAbleToEast = MoveGenHelpers.bPawnsAble2CaptureEast(bPawns, wPieces);

				while (bPawnsAbleToEast != 0)
				{
					SquareIndex startIndex = BitOperations.TrailingZeroCount(bPawnsAbleToEast);
					ulong startMask = startIndex.ToMask();
					bPawnsAbleToEast ^= startMask;

					ulong endMask = MoveGenHelpers.SoEaOne(startMask);
					SquareIndex endIndex = BitOperations.TrailingZeroCount(endMask);

					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BQueen, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BRook, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BBishop, true, capPiece));
						moves.Add(Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BKnight, true, capPiece));
					}
					else
					{
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.BPawn, capPiece);
						moves.Add(move);
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateKingMoves(in BitBoard board, PooledList<Move> moves)
		{
			if (board.sideToMove == Color.White)
			{
				SquareIndex kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing]);

				var kingAttackPattern = kingAttacks[kingSquare];
				var tabooSquares = board.pieces[(int)Piece.WhiteAll] | board.bAttacks;

				var moveTargets = kingAttackPattern & ~tabooSquares;
				while (moveTargets != 0)
				{
					SquareIndex endIndex = BitOperations.TrailingZeroCount(moveTargets);
					ulong endMask = endIndex.ToMask();
					moveTargets ^= endMask;

					var capPiece = board.mailbox[endIndex];
					if (capPiece != Piece.Empty)
					{
						Move m = Move.CreateCapture(kingSquare, endIndex, Piece.WKing, capPiece);
						moves.Add(m);
					}
					else
					{
						Move m = Move.CreateQuiet(kingSquare, endIndex, Piece.WKing);
						moves.Add(m);
					}
				}

				if (board.wKingSideAllowed &&
					(MoveGenHelpers.wCastleKingSideOccupiedMask & board.occupied) == 0 &&
					(MoveGenHelpers.wCastleKingSideAttackMask & board.bAttacks) == 0)
				{
					moves.Add(Move.CreateCastle(false, Color.White));
				}
				if (board.wQueenSideAllowed &&
					(MoveGenHelpers.wCastleQueenSideOccupiedMask & board.occupied) == 0 &&
					(MoveGenHelpers.wCastleQueenSideAttackMask & board.bAttacks) == 0)
				{
					moves.Add(Move.CreateCastle(true, Color.White));
				}
			}
			else
			{
				SquareIndex kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.BKing]);

				var kingAttackPattern = kingAttacks[kingSquare];
				var tabooSquares = board.pieces[(int)Piece.BlackAll] | board.wAttacks;

				var moveTargets = kingAttackPattern & ~tabooSquares;
				while (moveTargets != 0)
				{
					SquareIndex endIndex = BitOperations.TrailingZeroCount(moveTargets);
					ulong endMask = endIndex.ToMask();
					moveTargets ^= endMask;

					var capPiece = board.mailbox[endIndex];
					if (capPiece != Piece.Empty)
					{
						Move m = Move.CreateCapture(kingSquare, endIndex, Piece.BKing, capPiece);
						moves.Add(m);
					}
					else
					{
						Move m = Move.CreateQuiet(kingSquare, endIndex, Piece.BKing);
						moves.Add(m);
					}
				}

				if (board.bKingSideAllowed &&
					(MoveGenHelpers.bCastleKingSideOccupiedMask & board.occupied) == 0 &&
					(MoveGenHelpers.bCastleKingSideAttackMask & board.wAttacks) == 0)
				{
					moves.Add(Move.CreateCastle(false, Color.Black));
				}
				if (board.bQueenSideAllowed &&
					(MoveGenHelpers.bCastleQueenSideOccupiedMask & board.occupied) == 0 &&
					(MoveGenHelpers.bCastleQueenSideAttackMask & board.wAttacks) == 0)
				{
					moves.Add(Move.CreateCastle(true, Color.Black));
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateKnightMoves(in BitBoard board, PooledList<Move> moves)
		{
			if (board.sideToMove == Color.White)
			{
				var knights = board.pieces[(int)Piece.WKnight];

				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;

					var knightAttackPattern = knightAttacks[knightSquare] & ~board.pieces[(int)Piece.WhiteAll];

					while (knightAttackPattern != 0)
					{
						SquareIndex endIndex = BitOperations.TrailingZeroCount(knightAttackPattern);
						ulong endMask = endIndex.ToMask();
						knightAttackPattern ^= endMask;

						var capPiece = board.mailbox[endIndex];
						if (capPiece != Piece.Empty)
						{
							Move m = Move.CreateCapture(knightSquare, endIndex, Piece.WKnight, capPiece);
							moves.Add(m);
						}
						else
						{
							Move m = Move.CreateQuiet(knightSquare, endIndex, Piece.WKnight);
							moves.Add(m);
						}
					}
				}

			}
			else
			{
				var knights = board.pieces[(int)Piece.BKnight];

				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;

					var knightAttackPattern = knightAttacks[knightSquare] & ~board.pieces[(int)Piece.BlackAll];

					while (knightAttackPattern != 0)
					{
						SquareIndex endIndex = BitOperations.TrailingZeroCount(knightAttackPattern);
						ulong endMask = endIndex.ToMask();
						knightAttackPattern ^= endMask;

						var capPiece = board.mailbox[endIndex];
						if (capPiece != Piece.Empty)
						{
							Move m = Move.CreateCapture(knightSquare, endIndex, Piece.BKnight, capPiece);
							moves.Add(m);
						}
						else
						{
							Move m = Move.CreateQuiet(knightSquare, endIndex, Piece.BKnight);
							moves.Add(m);
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateRookMoves(in BitBoard board, PooledList<Move> moves, Piece piece)
		{
			Color ownColor = piece.Color();
			Color enemyColor = ownColor.Invert();
			ulong occupancy = board.occupied;

			var rooks = board.pieces[(int)piece];
			while (rooks != 0)
			{
				SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
				ulong rookMask = rookSquare.ToMask();
				rooks ^= rookMask;

				var rookPattern = Magic.RookAttacks(rookSquare, occupancy) & ~board.pieces[(int)ownColor];
				var rookAttacks = rookPattern & board.pieces[(int)enemyColor];
				var rookQuiets = rookPattern & board.empty;

				while (rookAttacks != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(rookAttacks);
					ulong endUInt64 = endSquare.ToMask();
					rookAttacks ^= endUInt64;

					Piece capPiece = board.mailbox[endSquare];

					Move move = Move.CreateCapture(rookSquare, endSquare, piece, capPiece);
					moves.Add(move);
				}

				while (rookQuiets != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(rookQuiets);
					ulong endUInt64 = endSquare.ToMask();
					rookQuiets ^= endUInt64;

					Move move = Move.CreateQuiet(rookSquare, endSquare, piece);
					moves.Add(move);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateBishopMoves(in BitBoard board, PooledList<Move> moves, Piece piece)
		{
			Color ownColor = piece.Color();
			Color enemyColor = ownColor.Invert();
			ulong occupancy = board.occupied;

			var bishops = board.pieces[(int)piece];
			while (bishops != 0)
			{
				SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
				ulong bishopMask = bishopSquare.ToMask();
				bishops ^= bishopMask;

				var bishopPattern = Magic.BishopAttacks(bishopSquare, occupancy) & ~board.pieces[(int)ownColor];
				var bishopAttacks = bishopPattern & board.pieces[(int)enemyColor];
				var bishopQuiets = bishopPattern & board.empty;

				while (bishopAttacks != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(bishopAttacks);
					ulong endUInt64 = endSquare.ToMask();
					bishopAttacks ^= endUInt64;

					Piece capPiece = board.mailbox[endSquare];

					Move move = Move.CreateCapture(bishopSquare, endSquare, piece, capPiece);
					moves.Add(move);
				}

				while (bishopQuiets != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(bishopQuiets);
					ulong endUInt64 = endSquare.ToMask();
					bishopQuiets ^= endUInt64;

					Move move = Move.CreateQuiet(bishopSquare, endSquare, piece);
					moves.Add(move);
				}
			}
		}

	}
}
