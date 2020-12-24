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

		private struct PinnedMap
		{
			public ulong allPinned;
			public ulong straightPinned;
			public ulong diagonalPinned;
			public ulong pinnedAllowedStraight;
			public ulong pinnedAllowedDiagonal;
		}

		private struct KingCheckState
		{
			public ulong attackMap;
			public ulong pushMap;
			public int numAttackers;
		}

		public static int GeneratePseudoLegalMoves(in BitBoard board, Span<Move> moves)
		{
			int numMoves = 0;

			KingCheckState checkState = new KingCheckState()
			{
				attackMap = ~0UL,
				numAttackers = 0,
				pushMap = ~0UL
			};

			PinnedMap pinned = default;

			GeneratePawnAttacks(board, pinned, checkState, moves, ref numMoves);
			GeneratePawnPushes(board, pinned, checkState, moves, ref numMoves);
			GenerateKnightMoves(board, pinned, checkState, moves, ref numMoves);

			int c = (int)board.sideToMove;
			int oc = (c + 1) & 1;
			int kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing + oc]);

			if ((board.pieces[(int)Piece.WQueen + c]) != 0)
			{
				ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
				ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
				GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions, moves, ref numMoves,
					(Piece)((int)Piece.WRook + (int)board.sideToMove));
				GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions, moves, ref numMoves,
					(Piece)((int)Piece.WBishop + (int)board.sideToMove));

				GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions | diagonalSliderCheckPositions, moves, ref numMoves,
					(Piece)((int)Piece.WQueen + (int)board.sideToMove));
				GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions | straightSliderCheckPositions, moves, ref numMoves,
					(Piece)((int)Piece.WQueen + (int)board.sideToMove));
			}
			else
			{
				if ((board.pieces[(int)Piece.WRook + c]) != 0)
				{
					ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
					GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WRook + (int)board.sideToMove));
				}

				if ((board.pieces[(int)Piece.WBishop + c]) != 0)
				{
					ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
					GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WBishop + (int)board.sideToMove));
				}
			}

			GenerateKingMoves(board, moves, ref numMoves);
			return numMoves;
		}

		public static int GenerateLegalMoves(in BitBoard board, Span<Move> moves)
		{
			int numMoves = 0;

			KingCheckState checkState = GenerateKingCheckState(board);

			if (checkState.numAttackers > 1)
			{
				GenerateKingMoves(board, moves, ref numMoves);
			}
			else
			{
				PinnedMap pinned = GeneratePinnedMap(board);

				GeneratePawnAttacks(board, pinned, checkState, moves, ref numMoves);
				GeneratePawnPushes(board, pinned, checkState, moves, ref numMoves);
				GenerateKnightMoves(board, pinned, checkState, moves, ref numMoves);


				int c = (int)board.sideToMove;
				int oc = (c + 1) & 1;
				int kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing + oc]);

				if ((board.pieces[(int)Piece.WQueen + c]) != 0)
				{
					ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
					ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
					GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WRook + (int)board.sideToMove));
					GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WBishop + (int)board.sideToMove));

					GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions | diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WQueen + (int)board.sideToMove));
					GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions | straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WQueen + (int)board.sideToMove));
				}
				else
				{
					if ((board.pieces[(int)Piece.WRook + c]) != 0)
					{
						ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
						GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions, moves, ref numMoves,
							(Piece)((int)Piece.WRook + (int)board.sideToMove));
					}

					if ((board.pieces[(int)Piece.WBishop + c]) != 0)
					{
						ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
						GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions, moves, ref numMoves,
							(Piece)((int)Piece.WBishop + (int)board.sideToMove));
					}
				}

				GenerateKingMoves(board, moves, ref numMoves);
			}

			return numMoves;
		}

		public static int GenerateQuiescentSearchMoves(in BitBoard board, Span<Move> moves)
		{
			int numMoves = 0;

			KingCheckState checkState = GenerateKingCheckState(board);

			if (checkState.numAttackers > 1)
			{
				GenerateKingMoves(board, moves, ref numMoves);
			}
			else if (checkState.numAttackers == 1)
			{
				PinnedMap pinned = GeneratePinnedMap(board);

				GeneratePawnAttacks(board, pinned, checkState, moves, ref numMoves);
				GeneratePawnPushes(board, pinned, checkState, moves, ref numMoves);
				GenerateKnightMoves(board, pinned, checkState, moves, ref numMoves);

				int c = (int)board.sideToMove;
				int oc = (c + 1) & 1;
				int kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing + oc]);

				if ((board.pieces[(int)Piece.WQueen + c]) != 0)
				{
					ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
					ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
					GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WRook + (int)board.sideToMove));
					GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WBishop + (int)board.sideToMove));

					GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions | diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WQueen + (int)board.sideToMove));
					GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions | straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WQueen + (int)board.sideToMove));
				}
				else
				{
					if ((board.pieces[(int)Piece.WRook + c]) != 0)
					{
						ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
						GenerateRookMoves(board, pinned, checkState, straightSliderCheckPositions, moves, ref numMoves,
							(Piece)((int)Piece.WRook + (int)board.sideToMove));
					}

					if ((board.pieces[(int)Piece.WBishop + c]) != 0)
					{
						ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
						GenerateBishopMoves(board, pinned, checkState, diagonalSliderCheckPositions, moves, ref numMoves,
							(Piece)((int)Piece.WBishop + (int)board.sideToMove));
					}
				}

				GenerateKingMoves(board, moves, ref numMoves);
			}
			else
			{
				PinnedMap pinned = GeneratePinnedMap(board);

				GeneratePawnAttacks(board, pinned, checkState, moves, ref numMoves);
				GeneratePawnPushChecksAndPromo(board, pinned, moves, ref numMoves);
				GenerateKnightAttackAndChecks(board, pinned, moves, ref numMoves);

				int c = (int)board.sideToMove;
				int oc = (c + 1) & 1;
				int kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing + oc]);

				if ((board.pieces[(int)Piece.WQueen + c]) != 0)
				{
					ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
					ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
					GenerateRookAttackAndChecks(board, pinned, straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WRook + (int)board.sideToMove));
					GenerateBishopAttackAndChecks(board, pinned, diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WBishop + (int)board.sideToMove));

					GenerateRookAttackAndChecks(board, pinned, straightSliderCheckPositions | diagonalSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WQueen + (int)board.sideToMove));
					GenerateBishopAttackAndChecks(board, pinned, diagonalSliderCheckPositions | straightSliderCheckPositions, moves, ref numMoves,
						(Piece)((int)Piece.WQueen + (int)board.sideToMove));
				}
				else
				{
					if ((board.pieces[(int)Piece.WRook + c]) != 0)
					{
						ulong straightSliderCheckPositions = Magic.RookAttacks(kingSquare, board.occupied);
						GenerateRookAttackAndChecks(board, pinned, straightSliderCheckPositions, moves, ref numMoves,
							(Piece)((int)Piece.WRook + (int)board.sideToMove));
					}

					if ((board.pieces[(int)Piece.WBishop + c]) != 0)
					{
						ulong diagonalSliderCheckPositions = Magic.BishopAttacks(kingSquare, board.occupied);
						GenerateBishopAttackAndChecks(board, pinned, diagonalSliderCheckPositions, moves, ref numMoves,
							(Piece)((int)Piece.WBishop + (int)board.sideToMove));
					}
				}


			}

			return numMoves;
		}

		public static List<Move> GenerateMoveList(in BitBoard board)
		{
			Span<Move> moves = stackalloc Move[218];
			int numMoves = GenerateLegalMoves(board, moves);
			List<Move> list = new List<Move>(20);

			for (int i = 0; i < numMoves; i++)
			{
				list.Add(moves[i]);
			}

			return list;
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
					ulong emptyMask = board.empty | board.pieces[(int)Piece.BKing];
					wAttacks |= Avx2Dumb7FillAttacks(board.pieces[(int)Piece.WQueen],
						board.pieces[(int)Piece.WRook],
						board.pieces[(int)Piece.WBishop], emptyMask);

				}
				else
				{
					var rooks = board.pieces[(int)Piece.WRook] | board.pieces[(int)Piece.WQueen];
					ulong occupiedMask = board.occupied ^ board.pieces[(int)Piece.BKing];
					while (rooks != 0)
					{
						SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
						ulong rookMask = rookSquare.ToMask();
						rooks ^= rookMask;
						var rookAttackPattern = Magic.RookAttacks(rookSquare, occupiedMask);
						wAttacks |= rookAttackPattern;
					}

					var bishops = board.pieces[(int)Piece.WBishop] | board.pieces[(int)Piece.WQueen];
					while (bishops != 0)
					{
						SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
						ulong bishopMask = bishopSquare.ToMask();
						bishops ^= bishopMask;
						var bishopAttackPattern = Magic.BishopAttacks(bishopSquare, occupiedMask);
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
					ulong emptyMask = board.empty | board.pieces[(int)Piece.WKing];

					bAttacks |= Avx2Dumb7FillAttacks(board.pieces[(int)Piece.BQueen],
						board.pieces[(int)Piece.BRook],
						board.pieces[(int)Piece.BBishop], emptyMask);
				}
				else
				{
					ulong occupiedMask = board.occupied ^ board.pieces[(int)Piece.WKing];
					var rooks = board.pieces[(int)Piece.BRook] | board.pieces[(int)Piece.BQueen];
					while (rooks != 0)
					{
						SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
						ulong rookMask = rookSquare.ToMask();
						rooks ^= rookMask;
						var rookAttackPattern = Magic.RookAttacks(rookSquare, occupiedMask);
						bAttacks |= rookAttackPattern;
					}

					var bishops = board.pieces[(int)Piece.BBishop] | board.pieces[(int)Piece.BQueen];
					while (bishops != 0)
					{
						SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
						ulong bishopMask = bishopSquare.ToMask();
						bishops ^= bishopMask;
						var bishopAttackPattern = Magic.BishopAttacks(bishopSquare, occupiedMask);
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
		private static void GeneratePawnPushes(in BitBoard board, PinnedMap pinned, KingCheckState checkState, Span<Move> moves, ref int numMoves)
		{

			if (board.sideToMove == Color.White)
			{

				var wPawns = board.pieces[(int)Piece.WPawn] & ~pinned.diagonalPinned;
				var bKing = board.pieces[(int)Piece.BKing];

				var pawnCheck1pushStartPositions = MoveGenHelpers.soSoEa(bKing) | MoveGenHelpers.soSoWe(bKing);


				var wPawns1Push = MoveGenHelpers.wPawnsAble2Push(wPawns, board.empty);

				while (wPawns1Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(wPawns1Push);
					ulong startMask = nextIndex.ToMask();
					wPawns1Push ^= startMask;

					SquareIndex endPos = nextIndex.OneNorth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					if ((endMask & checkState.pushMap) == 0)
					{
						continue;
					}

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WQueen);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WRook);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WBishop);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WKnight);
					}
					else
					{
						bool isCheck = (startMask & pawnCheck1pushStartPositions) != 0;
						Move m = Move.CreateQuiet(nextIndex, endPos, Piece.WPawn, isCheck);
						moves[numMoves++] = m;
					}
				}

				var pawnCheck2pushStartPositions = MoveGenHelpers.SouthOne(pawnCheck1pushStartPositions);

				var wPawns2Push = MoveGenHelpers.wPawnsAble2DblPush(wPawns, board.empty);
				while (wPawns2Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(wPawns2Push);
					ulong startMask = nextIndex.ToMask();
					wPawns2Push ^= startMask;

					SquareIndex endPos = nextIndex.TwoNorth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					if ((endMask & checkState.pushMap) == 0)
					{
						continue;
					}

					bool isCheck = (startMask & pawnCheck2pushStartPositions) != 0;

					Move m = Move.CreatePawnDouble(nextIndex, endPos, Piece.WPawn, isCheck);
					moves[numMoves++] = m;
				}
			}
			else
			{
				var bPawns = board.pieces[(int)Piece.BPawn] & ~pinned.diagonalPinned;
				var wKing = board.pieces[(int)Piece.WKing];
				var pawnCheck1pushStartPositions = MoveGenHelpers.noNoEa(wKing) | MoveGenHelpers.noNoWe(wKing);

				var bPawns1Push = MoveGenHelpers.bPawnsAble2Push(bPawns, board.empty);

				while (bPawns1Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(bPawns1Push);
					ulong startMask = nextIndex.ToMask();
					bPawns1Push ^= startMask;

					SquareIndex endPos = nextIndex.OneSouth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					if ((endMask & checkState.pushMap) == 0)
					{
						continue;
					}

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BQueen);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BRook);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BBishop);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BKnight);
					}
					else
					{
						bool isCheck = (startMask & pawnCheck1pushStartPositions) != 0;
						Move m = Move.CreateQuiet(nextIndex, endPos, Piece.BPawn, isCheck);
						moves[numMoves++] = m;
					}
				}

				var pawnCheck2pushStartPositions = MoveGenHelpers.NorthOne(pawnCheck1pushStartPositions);

				var bPawns2Push = MoveGenHelpers.bPawnsAble2DblPush(bPawns, board.empty);
				while (bPawns2Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(bPawns2Push);
					ulong startMask = nextIndex.ToMask();
					bPawns2Push ^= startMask;

					SquareIndex endPos = nextIndex.TwoSouth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					if ((endMask & checkState.pushMap) == 0)
					{
						continue;
					}

					bool isCheck = (startMask & pawnCheck2pushStartPositions) != 0;

					Move m = Move.CreatePawnDouble(nextIndex, endPos, Piece.BPawn, isCheck);
					moves[numMoves++] = m;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GeneratePawnPushChecksAndPromo(in BitBoard board, PinnedMap pinned, Span<Move> moves, ref int numMoves)
		{

			if (board.sideToMove == Color.White)
			{

				var wPawns = board.pieces[(int)Piece.WPawn] & ~pinned.diagonalPinned;
				var bKing = board.pieces[(int)Piece.BKing];

				var pawnCheck1pushStartPositions = MoveGenHelpers.soSoEa(bKing) | MoveGenHelpers.soSoWe(bKing)
																				| MoveGenHelpers.Ranks[6];

				var wPawns1Push = MoveGenHelpers.wPawnsAble2Push(wPawns, board.empty) & pawnCheck1pushStartPositions;

				while (wPawns1Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(wPawns1Push);
					ulong startMask = nextIndex.ToMask();
					wPawns1Push ^= startMask;

					SquareIndex endPos = nextIndex.OneNorth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WQueen);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.WPawn, Piece.WKnight);
					}
					else
					{
						Move m = Move.CreateQuiet(nextIndex, endPos, Piece.WPawn, true);
						moves[numMoves++] = m;
					}
				}

				var pawnCheck2pushStartPositions = MoveGenHelpers.SouthOne(pawnCheck1pushStartPositions);

				var wPawns2Push = MoveGenHelpers.wPawnsAble2DblPush(wPawns, board.empty) & pawnCheck2pushStartPositions;
				while (wPawns2Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(wPawns2Push);
					ulong startMask = nextIndex.ToMask();
					wPawns2Push ^= startMask;

					SquareIndex endPos = nextIndex.TwoNorth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					Move m = Move.CreatePawnDouble(nextIndex, endPos, Piece.WPawn, true);
					moves[numMoves++] = m;
				}
			}
			else
			{
				var bPawns = board.pieces[(int)Piece.BPawn] & ~pinned.diagonalPinned;
				var wKing = board.pieces[(int)Piece.WKing];
				var pawnCheck1pushStartPositions = MoveGenHelpers.noNoEa(wKing) | MoveGenHelpers.noNoWe(wKing)
																				| MoveGenHelpers.Ranks[1];
				var bPawns1Push = MoveGenHelpers.bPawnsAble2Push(bPawns, board.empty) & pawnCheck1pushStartPositions;

				while (bPawns1Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(bPawns1Push);
					ulong startMask = nextIndex.ToMask();
					bPawns1Push ^= startMask;

					SquareIndex endPos = nextIndex.OneSouth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BQueen);
						moves[numMoves++] = Move.CreatePromotion(nextIndex, endPos, Piece.BPawn, Piece.BKnight);
					}
					else
					{
						Move m = Move.CreateQuiet(nextIndex, endPos, Piece.BPawn, true);
						moves[numMoves++] = m;
					}
				}

				var pawnCheck2pushStartPositions = MoveGenHelpers.NorthOne(pawnCheck1pushStartPositions);

				var bPawns2Push = MoveGenHelpers.bPawnsAble2DblPush(bPawns, board.empty) & pawnCheck2pushStartPositions;
				while (bPawns2Push > 0)
				{
					SquareIndex nextIndex = BitOperations.TrailingZeroCount(bPawns2Push);
					ulong startMask = nextIndex.ToMask();
					bPawns2Push ^= startMask;

					SquareIndex endPos = nextIndex.TwoSouth();
					ulong endMask = endPos.ToMask();

					if ((startMask & pinned.straightPinned) != 0 && (endMask & pinned.pinnedAllowedStraight) == 0)
					{
						continue;
					}

					Move m = Move.CreatePawnDouble(nextIndex, endPos, Piece.BPawn, true);
					moves[numMoves++] = m;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GeneratePawnAttacks(in BitBoard board, PinnedMap pinned, KingCheckState checkState, Span<Move> moves, ref int numMoves)
		{

			if (!board.enPassantTargetSquare.IsNoneSquare)
			{
				ulong enPassantMask = board.enPassantTargetSquare.ToMask();

				if (board.sideToMove == Color.White)
				{
					var wPawns = board.pieces[(int)Piece.WPawn] & ~pinned.straightPinned;
					var wPawnAbleToEnPassant = MoveGenHelpers.wPawnsAble2CaptureAny(wPawns, enPassantMask);
					var bKing = board.pieces[(int)Piece.BKing];

					var pawnCheckEndPositions = MoveGenHelpers.SoEaOne(bKing) | MoveGenHelpers.SoWeOne(bKing);

					while (wPawnAbleToEnPassant != 0)
					{
						SquareIndex startIdx = BitOperations.TrailingZeroCount(wPawnAbleToEnPassant);
						var startMask = startIdx.ToMask();
						wPawnAbleToEnPassant ^= startMask;
						var endIdx = board.enPassantTargetSquare;
						var endMask = enPassantMask;
						var captureIdx = new SquareIndex(startIdx.Rank, endIdx.File);
						var captureMask = captureIdx.ToMask();

						bool isCheck = (endMask & pawnCheckEndPositions) != 0;

						if (!((startMask & pinned.diagonalPinned) != 0 &&
							  (endMask & pinned.pinnedAllowedDiagonal) == 0))
						{
							if ((captureMask & checkState.attackMap) != 0 ||
								(endMask & checkState.pushMap) != 0)
							{
								if (CheckEnPassantNotDiscoverCheck(board, startIdx, captureIdx))
								{
									moves[numMoves++] = Move.CreateEnPassant(startIdx, board.enPassantTargetSquare,
										Piece.WPawn, Piece.BPawn, isCheck);
								}
							}
						}
					}
				}
				else
				{
					var bPawns = board.pieces[(int)Piece.BPawn] & ~pinned.straightPinned;
					var bPawnAbleToEnPassant = MoveGenHelpers.bPawnsAble2CaptureAny(bPawns, enPassantMask);
					var wKing = board.pieces[(int)Piece.WKing];

					var pawnCheckEndPositions = MoveGenHelpers.NoEaOne(wKing) | MoveGenHelpers.NoWeOne(wKing);

					while (bPawnAbleToEnPassant != 0)
					{
						SquareIndex startIdx = BitOperations.TrailingZeroCount(bPawnAbleToEnPassant);
						var startMask = startIdx.ToMask();
						bPawnAbleToEnPassant ^= startMask;
						var endIdx = board.enPassantTargetSquare;
						var endMask = enPassantMask;
						var captureIdx = new SquareIndex(startIdx.Rank, endIdx.File);
						var captureMask = captureIdx.ToMask();

						bool isCheck = (endMask & pawnCheckEndPositions) != 0;

						if (!((startMask & pinned.diagonalPinned) != 0 &&
							  (endMask & pinned.pinnedAllowedDiagonal) == 0))
						{
							if ((captureMask & checkState.attackMap) != 0 ||
								(endMask & checkState.pushMap) != 0)
							{
								if (CheckEnPassantNotDiscoverCheck(board, startIdx, captureIdx))
								{
									moves[numMoves++] = Move.CreateEnPassant(startIdx, board.enPassantTargetSquare,
										Piece.BPawn, Piece.WPawn, isCheck);
								}
							}
						}
					}
				}
			}

			if (board.sideToMove == Color.White)
			{
				var wPawns = board.pieces[(int)Piece.WPawn] & ~pinned.straightPinned;
				var bPieces = board.pieces[(int)Piece.BlackAll];
				var bKing = board.pieces[(int)Piece.BKing];

				var pawnCheckEndPositions = MoveGenHelpers.SoEaOne(bKing) | MoveGenHelpers.SoWeOne(bKing);

				var wPawnsAbleToWest = MoveGenHelpers.wPawnsAble2CaptureWest(wPawns, bPieces);

				while (wPawnsAbleToWest != 0)
				{
					SquareIndex startIndex = BitOperations.TrailingZeroCount(wPawnsAbleToWest);
					ulong startMask = startIndex.ToMask();
					wPawnsAbleToWest ^= startMask;

					ulong endMask = MoveGenHelpers.NoWeOne(startMask);
					SquareIndex endIndex = BitOperations.TrailingZeroCount(endMask);

					if ((startMask & pinned.diagonalPinned) != 0 && (endMask & pinned.pinnedAllowedDiagonal) == 0)
					{
						continue;
					}

					if ((endMask & checkState.attackMap) == 0)
					{
						continue;
					}

					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WQueen, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WRook, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WBishop, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WKnight, false, true, capPiece);
					}
					else
					{
						bool isCheck = (endMask & pawnCheckEndPositions) != 0;
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.WPawn, capPiece, isCheck);
						moves[numMoves++] = move;
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

					if ((startMask & pinned.diagonalPinned) != 0 && (endMask & pinned.pinnedAllowedDiagonal) == 0)
					{
						continue;
					}

					if ((endMask & checkState.attackMap) == 0)
					{
						continue;
					}



					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[7]))
					{
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WQueen, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WRook, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WBishop, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.WPawn, Piece.WKnight, false, true, capPiece);
					}
					else
					{
						bool isCheck = (endMask & pawnCheckEndPositions) != 0;
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.WPawn, capPiece, isCheck);
						moves[numMoves++] = move;
					}
				}
			}
			else
			{
				var bPawns = board.pieces[(int)Piece.BPawn] & ~pinned.straightPinned;
				var wPieces = board.pieces[(int)Piece.WhiteAll];
				var wKing = board.pieces[(int)Piece.WKing];

				var pawnCheckEndPositions = MoveGenHelpers.NoEaOne(wKing) | MoveGenHelpers.NoWeOne(wKing);

				var bPawnsAbleToWest = MoveGenHelpers.bPawnsAble2CaptureWest(bPawns, wPieces);

				while (bPawnsAbleToWest != 0)
				{
					SquareIndex startIndex = BitOperations.TrailingZeroCount(bPawnsAbleToWest);
					ulong startMask = startIndex.ToMask();
					bPawnsAbleToWest ^= startMask;

					ulong endMask = MoveGenHelpers.SoWeOne(startMask);
					SquareIndex endIndex = BitOperations.TrailingZeroCount(endMask);

					if ((startMask & pinned.diagonalPinned) != 0 && (endMask & pinned.pinnedAllowedDiagonal) == 0)
					{
						continue;
					}

					if ((endMask & checkState.attackMap) == 0)
					{
						continue;
					}

					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BQueen, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BRook, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BBishop, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BKnight, false, true, capPiece);
					}
					else
					{
						bool isCheck = (endMask & pawnCheckEndPositions) != 0;
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.BPawn, capPiece, isCheck);
						moves[numMoves++] = move;
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

					if ((startMask & pinned.diagonalPinned) != 0 && (endMask & pinned.pinnedAllowedDiagonal) == 0)
					{
						continue;
					}

					if ((endMask & checkState.attackMap) == 0)
					{
						continue;
					}



					Piece capPiece = board.mailbox[endIndex];

					if (endMask.Any(MoveGenHelpers.Ranks[0]))
					{
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BQueen, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BRook, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BBishop, false, true, capPiece);
						moves[numMoves++] = Move.CreatePromotion(startIndex, endIndex, Piece.BPawn, Piece.BKnight, false, true, capPiece);
					}
					else
					{
						bool isCheck = (endMask & pawnCheckEndPositions) != 0;
						Move move = Move.CreateCapture(startIndex, endIndex, Piece.BPawn, capPiece, isCheck);
						moves[numMoves++] = move;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateKingMoves(in BitBoard board, Span<Move> moves, ref int numMoves)
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
						moves[numMoves++] = m;
					}
					else
					{
						Move m = Move.CreateQuiet(kingSquare, endIndex, Piece.WKing);
						moves[numMoves++] = m;
					}
				}

				if (!board.isKingInCheck)
				{
					if ((board.castling & BitBoard.wKingSideCastling) != 0 &&
						(MoveGenHelpers.wCastleKingSideOccupiedMask & board.occupied) == 0 &&
						(MoveGenHelpers.wCastleKingSideAttackMask & board.bAttacks) == 0)
					{
						moves[numMoves++] = Move.CreateCastle(false, Color.White);
					}

					if ((board.castling & BitBoard.wQueenSideCastling) != 0 &&
						(MoveGenHelpers.wCastleQueenSideOccupiedMask & board.occupied) == 0 &&
						(MoveGenHelpers.wCastleQueenSideAttackMask & board.bAttacks) == 0)
					{
						moves[numMoves++] = Move.CreateCastle(true, Color.White);
					}
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
						moves[numMoves++] = m;
					}
					else
					{
						Move m = Move.CreateQuiet(kingSquare, endIndex, Piece.BKing);
						moves[numMoves++] = m;
					}
				}

				if (!board.isKingInCheck)
				{
					if ((board.castling & BitBoard.bKingSideCastling) != 0 &&
						(MoveGenHelpers.bCastleKingSideOccupiedMask & board.occupied) == 0 &&
						(MoveGenHelpers.bCastleKingSideAttackMask & board.wAttacks) == 0)
					{
						moves[numMoves++] = Move.CreateCastle(false, Color.Black);
					}

					if ((board.castling & BitBoard.bQueenSideCastling) != 0 &&
						(MoveGenHelpers.bCastleQueenSideOccupiedMask & board.occupied) == 0 &&
						(MoveGenHelpers.bCastleQueenSideAttackMask & board.wAttacks) == 0)
					{
						moves[numMoves++] = Move.CreateCastle(true, Color.Black);
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateKnightMoves(in BitBoard board, PinnedMap pinned, KingCheckState checkState, Span<Move> moves, ref int numMoves)
		{
			if (board.sideToMove == Color.White)
			{
				var knights = board.pieces[(int)Piece.WKnight] & ~pinned.allPinned;
				var bKingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.BKing]);
				var checkPositions = knightAttacks[bKingSquare];

				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;

					var knightAttackPattern = knightAttacks[knightSquare] & ~board.pieces[(int)Piece.WhiteAll];

					knightAttackPattern &= checkState.attackMap | checkState.pushMap;

					while (knightAttackPattern != 0)
					{
						SquareIndex endIndex = BitOperations.TrailingZeroCount(knightAttackPattern);
						ulong endMask = endIndex.ToMask();
						knightAttackPattern ^= endMask;

						bool isCheck = (endMask & checkPositions) != 0;

						var capPiece = board.mailbox[endIndex];
						if (capPiece != Piece.Empty)
						{
							Move m = Move.CreateCapture(knightSquare, endIndex, Piece.WKnight, capPiece, isCheck);
							moves[numMoves++] = m;
						}
						else
						{
							Move m = Move.CreateQuiet(knightSquare, endIndex, Piece.WKnight, isCheck);
							moves[numMoves++] = m;
						}
					}
				}

			}
			else
			{
				var knights = board.pieces[(int)Piece.BKnight] & ~pinned.allPinned;
				var wKingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing]);
				var checkPositions = knightAttacks[wKingSquare];

				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;

					var knightAttackPattern = knightAttacks[knightSquare] & ~board.pieces[(int)Piece.BlackAll];

					knightAttackPattern &= checkState.attackMap | checkState.pushMap;

					while (knightAttackPattern != 0)
					{
						SquareIndex endIndex = BitOperations.TrailingZeroCount(knightAttackPattern);
						ulong endMask = endIndex.ToMask();
						knightAttackPattern ^= endMask;

						bool isCheck = (endMask & checkPositions) != 0;

						var capPiece = board.mailbox[endIndex];
						if (capPiece != Piece.Empty)
						{
							Move m = Move.CreateCapture(knightSquare, endIndex, Piece.BKnight, capPiece, isCheck);
							moves[numMoves++] = m;
						}
						else
						{
							Move m = Move.CreateQuiet(knightSquare, endIndex, Piece.BKnight, isCheck);
							moves[numMoves++] = m;
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateKnightAttackAndChecks(in BitBoard board, PinnedMap pinned, Span<Move> moves, ref int numMoves)
		{
			if (board.sideToMove == Color.White)
			{
				var knights = board.pieces[(int)Piece.WKnight] & ~pinned.allPinned;
				var bKingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.BKing]);
				var checkPositions = knightAttacks[bKingSquare];

				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;

					var knightAttackPattern = knightAttacks[knightSquare] & ~board.pieces[(int)Piece.WhiteAll];

					knightAttackPattern &= board.pieces[(int)Piece.BlackAll] | checkPositions;

					while (knightAttackPattern != 0)
					{
						SquareIndex endIndex = BitOperations.TrailingZeroCount(knightAttackPattern);
						ulong endMask = endIndex.ToMask();
						knightAttackPattern ^= endMask;

						bool isCheck = (endMask & checkPositions) != 0;

						var capPiece = board.mailbox[endIndex];
						if (capPiece != Piece.Empty)
						{
							Move m = Move.CreateCapture(knightSquare, endIndex, Piece.WKnight, capPiece, isCheck);
							moves[numMoves++] = m;
						}
						else
						{
							Move m = Move.CreateQuiet(knightSquare, endIndex, Piece.WKnight, isCheck);
							moves[numMoves++] = m;
						}
					}
				}

			}
			else
			{
				var knights = board.pieces[(int)Piece.BKnight] & ~pinned.allPinned;
				var wKingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing]);
				var checkPositions = knightAttacks[wKingSquare];

				while (knights != 0)
				{
					SquareIndex knightSquare = BitOperations.TrailingZeroCount(knights);
					ulong knightMask = knightSquare.ToMask();
					knights ^= knightMask;

					var knightAttackPattern = knightAttacks[knightSquare] & ~board.pieces[(int)Piece.BlackAll];

					knightAttackPattern &= board.pieces[(int)Piece.WhiteAll] | checkPositions;

					while (knightAttackPattern != 0)
					{
						SquareIndex endIndex = BitOperations.TrailingZeroCount(knightAttackPattern);
						ulong endMask = endIndex.ToMask();
						knightAttackPattern ^= endMask;

						bool isCheck = (endMask & checkPositions) != 0;

						var capPiece = board.mailbox[endIndex];
						if (capPiece != Piece.Empty)
						{
							Move m = Move.CreateCapture(knightSquare, endIndex, Piece.BKnight, capPiece, isCheck);
							moves[numMoves++] = m;
						}
						else
						{
							Move m = Move.CreateQuiet(knightSquare, endIndex, Piece.BKnight, isCheck);
							moves[numMoves++] = m;
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateRookMoves(in BitBoard board, PinnedMap pinned, KingCheckState checkState, ulong kingCheckSquares, Span<Move> moves, ref int numMoves, Piece piece)
		{
			Color ownColor = piece.Color();
			Color enemyColor = ownColor.Invert();
			ulong occupancy = board.occupied;

			var rooks = board.pieces[(int)piece] & ~pinned.diagonalPinned;
			while (rooks != 0)
			{
				SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
				ulong rookMask = rookSquare.ToMask();
				rooks ^= rookMask;

				var rookPattern = Magic.RookAttacks(rookSquare, occupancy) & ~board.pieces[(int)ownColor];

				//If pinned, only allow moves in allowed squares
				if ((rookMask & pinned.straightPinned) != 0)
				{
					rookPattern &= pinned.pinnedAllowedStraight;
				}

				var rookAttacks = rookPattern & board.pieces[(int)enemyColor];
				var rookQuiets = rookPattern & board.empty;

				rookAttacks &= checkState.attackMap;
				rookQuiets &= checkState.pushMap;

				while (rookAttacks != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(rookAttacks);
					ulong endMask = endSquare.ToMask();
					rookAttacks ^= endMask;

					bool isCheck = (endMask & kingCheckSquares) != 0;

					Piece capPiece = board.mailbox[endSquare];

					Move move = Move.CreateCapture(rookSquare, endSquare, piece, capPiece, isCheck);
					moves[numMoves++] = move;
				}

				while (rookQuiets != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(rookQuiets);
					ulong endMask = endSquare.ToMask();
					rookQuiets ^= endMask;

					bool isCheck = (endMask & kingCheckSquares) != 0;

					Move move = Move.CreateQuiet(rookSquare, endSquare, piece, isCheck);
					moves[numMoves++] = move;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateBishopMoves(in BitBoard board, PinnedMap pinned, KingCheckState checkState, ulong kingCheckSquares, Span<Move> moves, ref int numMoves, Piece piece)
		{
			Color ownColor = piece.Color();
			Color enemyColor = ownColor.Invert();
			ulong occupancy = board.occupied;

			var bishops = board.pieces[(int)piece] & ~pinned.straightPinned;
			while (bishops != 0)
			{
				SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
				ulong bishopMask = bishopSquare.ToMask();
				bishops ^= bishopMask;

				var bishopPattern = Magic.BishopAttacks(bishopSquare, occupancy) & ~board.pieces[(int)ownColor];

				//If pinned, only allow moves in allowed squares
				if ((bishopMask & pinned.diagonalPinned) != 0)
				{
					bishopPattern &= pinned.pinnedAllowedDiagonal;
				}

				var bishopAttacks = bishopPattern & board.pieces[(int)enemyColor];
				var bishopQuiets = bishopPattern & board.empty;

				bishopAttacks &= checkState.attackMap;
				bishopQuiets &= checkState.pushMap;

				while (bishopAttacks != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(bishopAttacks);
					ulong endMask = endSquare.ToMask();
					bishopAttacks ^= endMask;

					Piece capPiece = board.mailbox[endSquare];

					bool isCheck = (endMask & kingCheckSquares) != 0;

					Move move = Move.CreateCapture(bishopSquare, endSquare, piece, capPiece, isCheck);
					moves[numMoves++] = move;
				}

				while (bishopQuiets != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(bishopQuiets);
					ulong endMask = endSquare.ToMask();
					bishopQuiets ^= endMask;

					bool isCheck = (endMask & kingCheckSquares) != 0;

					Move move = Move.CreateQuiet(bishopSquare, endSquare, piece, isCheck);
					moves[numMoves++] = move;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateRookAttackAndChecks(in BitBoard board, PinnedMap pinned, ulong kingCheckSquares, Span<Move> moves, ref int numMoves, Piece piece)
		{
			Color ownColor = piece.Color();
			Color enemyColor = ownColor.Invert();
			ulong occupancy = board.occupied;

			var rooks = board.pieces[(int)piece] & ~pinned.diagonalPinned;
			while (rooks != 0)
			{
				SquareIndex rookSquare = BitOperations.TrailingZeroCount(rooks);
				ulong rookMask = rookSquare.ToMask();
				rooks ^= rookMask;

				var rookPattern = Magic.RookAttacks(rookSquare, occupancy) & ~board.pieces[(int)ownColor];

				//If pinned, only allow moves in allowed squares
				if ((rookMask & pinned.straightPinned) != 0)
				{
					rookPattern &= pinned.pinnedAllowedStraight;
				}

				var rookAttacks = rookPattern & board.pieces[(int)enemyColor];
				var rookQuiets = rookPattern & board.empty;

				rookQuiets &= kingCheckSquares;

				while (rookAttacks != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(rookAttacks);
					ulong endMask = endSquare.ToMask();
					rookAttacks ^= endMask;

					bool isCheck = (endMask & kingCheckSquares) != 0;

					Piece capPiece = board.mailbox[endSquare];

					Move move = Move.CreateCapture(rookSquare, endSquare, piece, capPiece, isCheck);
					moves[numMoves++] = move;
				}

				while (rookQuiets != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(rookQuiets);
					ulong endMask = endSquare.ToMask();
					rookQuiets ^= endMask;

					Move move = Move.CreateQuiet(rookSquare, endSquare, piece, true);
					moves[numMoves++] = move;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static void GenerateBishopAttackAndChecks(in BitBoard board, PinnedMap pinned, ulong kingCheckSquares, Span<Move> moves, ref int numMoves, Piece piece)
		{
			Color ownColor = piece.Color();
			Color enemyColor = ownColor.Invert();
			ulong occupancy = board.occupied;

			var bishops = board.pieces[(int)piece] & ~pinned.straightPinned;
			while (bishops != 0)
			{
				SquareIndex bishopSquare = BitOperations.TrailingZeroCount(bishops);
				ulong bishopMask = bishopSquare.ToMask();
				bishops ^= bishopMask;

				var bishopPattern = Magic.BishopAttacks(bishopSquare, occupancy) & ~board.pieces[(int)ownColor];

				//If pinned, only allow moves in allowed squares
				if ((bishopMask & pinned.diagonalPinned) != 0)
				{
					bishopPattern &= pinned.pinnedAllowedDiagonal;
				}

				var bishopAttacks = bishopPattern & board.pieces[(int)enemyColor];
				var bishopQuiets = bishopPattern & board.empty;

				bishopQuiets &= kingCheckSquares;

				while (bishopAttacks != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(bishopAttacks);
					ulong endMask = endSquare.ToMask();
					bishopAttacks ^= endMask;

					bool isCheck = (endMask & kingCheckSquares) != 0;

					Piece capPiece = board.mailbox[endSquare];

					Move move = Move.CreateCapture(bishopSquare, endSquare, piece, capPiece, isCheck);
					moves[numMoves++] = move;
				}

				while (bishopQuiets != 0)
				{
					SquareIndex endSquare = BitOperations.TrailingZeroCount(bishopQuiets);
					ulong endMask = endSquare.ToMask();
					bishopQuiets ^= endMask;

					Move move = Move.CreateQuiet(bishopSquare, endSquare, piece, true);
					moves[numMoves++] = move;
				}
			}
		}

		private static PinnedMap GeneratePinnedMap(in BitBoard board)
		{
			ulong straightPinned = 0;
			ulong diagonalPinned = 0;

			int selfCi = (int)board.sideToMove;
			int oppCi = (selfCi + 1) & 1;
			int kingSquare = BitOperations.TrailingZeroCount(board.pieces[(int)Piece.WKing + selfCi]);

			ulong kingSlidingStraightMovesToOpponents = Magic.RookAttacks(kingSquare, board.pieces[oppCi]);
			ulong kingSlidingDiagonalMovesToOpponents = Magic.BishopAttacks(kingSquare, board.pieces[oppCi]);

			ulong possibleSPinners = kingSlidingStraightMovesToOpponents & board.pieces[oppCi];
			ulong possibleDPinners = kingSlidingDiagonalMovesToOpponents & board.pieces[oppCi];
			ulong possibleStraightPinnedPieces = Magic.RookAttacks(kingSquare, board.pieces[selfCi]) & board.pieces[selfCi];
			ulong possibleDiagonalPinnedPieces = Magic.BishopAttacks(kingSquare, board.pieces[selfCi]) & board.pieces[selfCi];

			ulong possibleStraightPinners = (board.pieces[(int)Piece.WRook + oppCi] |
											board.pieces[(int)Piece.WQueen + oppCi]) & possibleSPinners;
			while (possibleStraightPinners != 0)
			{
				SquareIndex pinnerSquare = BitOperations.TrailingZeroCount(possibleStraightPinners);
				ulong pinnerMask = pinnerSquare.ToMask();
				possibleStraightPinners ^= pinnerMask;
				var pattern = Magic.RookAttacks(pinnerSquare, board.occupied);

				var pinned = pattern & possibleStraightPinnedPieces;
				straightPinned |= pinned;
			}

			ulong possibleDiagonalPinners = (board.pieces[(int)Piece.WBishop + oppCi] |
											board.pieces[(int)Piece.WQueen + oppCi]) & possibleDPinners;
			while (possibleDiagonalPinners != 0)
			{
				SquareIndex pinnerSquare = BitOperations.TrailingZeroCount(possibleDiagonalPinners);
				ulong pinnerMask = pinnerSquare.ToMask();
				possibleDiagonalPinners ^= pinnerMask;
				var pattern = Magic.BishopAttacks(pinnerSquare, board.occupied);

				var pinned = pattern & possibleDiagonalPinnedPieces;
				diagonalPinned |= pinned;
			}

			return new PinnedMap()
			{
				allPinned = straightPinned | diagonalPinned,
				straightPinned = straightPinned,
				diagonalPinned = diagonalPinned,
				pinnedAllowedStraight = kingSlidingStraightMovesToOpponents,
				pinnedAllowedDiagonal = kingSlidingDiagonalMovesToOpponents
			};
		}

		private static KingCheckState GenerateKingCheckState(in BitBoard board)
		{
			if (!board.isKingInCheck)
			{
				return new KingCheckState()
				{
					attackMap = ~0UL,
					numAttackers = 0,
					pushMap = ~0UL
				};
			}

			int selfCi = (int)board.sideToMove;
			int oppCi = (selfCi + 1) & 1;
			ulong oppSliderAttacks = board.sideToMove == Color.White ? board.bSliderAttacks : board.wSliderAttacks;
			ulong oppStaticAttacks = board.sideToMove == Color.White ? board.bStaticAttacks : board.wStaticAttacks;

			ulong kingMask = board.pieces[(int)Piece.WKing + selfCi];
			int kingSquare = BitOperations.TrailingZeroCount(kingMask);

			ulong attackerMask = 0;
			ulong pushMask = 0;

			if ((kingMask & oppStaticAttacks) != 0)
			{
				if (selfCi == 0)
				{
					attackerMask |= MoveGenHelpers.wPawnAnyAttacks(kingMask) & board.pieces[(int)Piece.BPawn];
				}
				else
				{
					attackerMask |= MoveGenHelpers.bPawnAnyAttacks(kingMask) & board.pieces[(int)Piece.WPawn];
				}

				attackerMask |= knightAttacks[kingSquare] & board.pieces[(int)Piece.WKnight + oppCi];
			}

			if ((kingMask & oppSliderAttacks) != 0)
			{
				ulong kingSlidingStraightMoves = Magic.RookAttacks(kingSquare, board.occupied);
				ulong kingSlidingDiagonalMoves = Magic.BishopAttacks(kingSquare, board.occupied);

				ulong enemyStraightSliders = (board.pieces[(int)Piece.WRook + oppCi] |
											  board.pieces[(int)Piece.WQueen + oppCi]) & kingSlidingStraightMoves;
				ulong enemyDiagonalSliders = (board.pieces[(int)Piece.WBishop + oppCi] |
											  board.pieces[(int)Piece.WQueen + oppCi]) & kingSlidingDiagonalMoves;

				attackerMask |= enemyStraightSliders;
				attackerMask |= enemyDiagonalSliders;

				int numAttackers = BitOperations.PopCount(attackerMask);

				if (numAttackers > 1) //double check
				{
					return new KingCheckState()
					{
						attackMap = attackerMask,
						numAttackers = numAttackers,
						pushMap = pushMask
					};
				}


				if (enemyStraightSliders != 0)
				{
					SquareIndex sliderSquare = BitOperations.TrailingZeroCount(enemyStraightSliders);
					var pattern = Magic.RookAttacks(sliderSquare, board.occupied);

					pushMask |= pattern & kingSlidingStraightMoves;
				}
				else // Has to be diagonal
				{
					SquareIndex sliderSquare = BitOperations.TrailingZeroCount(enemyDiagonalSliders);
					var pattern = Magic.BishopAttacks(sliderSquare, board.occupied);

					pushMask |= pattern & kingSlidingDiagonalMoves;
				}

				return new KingCheckState()
				{
					attackMap = attackerMask,
					numAttackers = numAttackers,
					pushMap = pushMask
				};
			}
			else
			{
				int numAttackers = BitOperations.PopCount(attackerMask);
				return new KingCheckState()
				{
					attackMap = attackerMask,
					numAttackers = numAttackers,
					pushMap = 0
				};
			}
		}

		private static bool CheckEnPassantNotDiscoverCheck(in BitBoard board, SquareIndex startIndex,
			SquareIndex eatenSquare)
		{
			ulong occupied = board.occupied;
			int selfCi = (int)board.sideToMove;
			int oppCi = (selfCi + 1) & 1;
			int epRank = startIndex.Rank;
			ulong rankMask = MoveGenHelpers.Ranks[epRank];
			ulong ownKingMask = board.pieces[(int)Piece.WKing + selfCi];

			if ((ownKingMask & rankMask) == 0) //King not in same rank as en passant
			{
				return true;
			}
			ulong enemyStraightSlidersOnSameRank = (board.pieces[(int)Piece.WRook + oppCi] |
													board.pieces[(int)Piece.WQueen + oppCi]) & rankMask;

			if (enemyStraightSlidersOnSameRank == 0) // No straight enemy sliders on same rank
			{
				return true;
			}

			// Remove both pieces from the board
			occupied ^= startIndex.ToMask() | eatenSquare.ToMask();

			// Rook attack from king
			int kingSquare = BitOperations.TrailingZeroCount(ownKingMask);
			var kingStraightSlider = Magic.RookAttacks(kingSquare, occupied);

			//Discovered a slider
			if ((kingStraightSlider & enemyStraightSlidersOnSameRank) != 0)
			{
				return false;
			}

			// No threat found
			return true;
		}
	}
}
