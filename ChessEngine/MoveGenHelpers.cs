using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessEngine
{
	internal static class MoveGenHelpers
	{
		public static int[] Shift = { 9, 1, -7, -8, -9, -1, 7, 8 };

		public static ulong[] AvoidWrap = {
			0xfefefefefefefe00,
			0xfefefefefefefefe,
			0x00fefefefefefefe,
			0x00ffffffffffffff,
			0x007f7f7f7f7f7f7f,
			0x7f7f7f7f7f7f7f7f,
			0x7f7f7f7f7f7f7f00,
			0xffffffffffffff00
		};

		public static ulong[] Ranks = {
			0x00000000000000FF,
			0x000000000000FF00,
			0x0000000000FF0000,
			0x00000000FF000000,
			0x000000FF00000000,
			0x0000FF0000000000,
			0x00FF000000000000,
			0xFF00000000000000
		};

		public static ulong[] Files = {
			0x0101010101010101UL,
			0x0202020202020202UL,
			0x0404040404040404UL,
			0x0808080808080808UL,
			0x1010101010101010UL,
			0x2020202020202020UL,
			0x4040404040404040UL,
			0x8080808080808080UL
		};

		public const ulong notAFile = 0xfefefefefefefefeUL;
		public const ulong notHFile = 0x7f7f7f7f7f7f7f7fUL;
		public const ulong notABFile = 0xfcfcfcfcfcfcfcfcUL;
		public const ulong notGHFile = 0x3f3f3f3f3f3f3f3fUL;

		public const ulong wCastleKingSideOccupiedMask = 0x60UL;
		public const ulong wCastleKingSideAttackMask = 0x70UL;
		public const ulong wCastleQueenSideOccupiedMask = 0x0EUL;
		public const ulong wCastleQueenSideAttackMask = 0x1CUL;

		public const ulong bCastleKingSideOccupiedMask = 0x6000000000000000UL;
		public const ulong bCastleKingSideAttackMask = 0x7000000000000000UL;
		public const ulong bCastleQueenSideOccupiedMask = 0x0E00000000000000UL;
		public const ulong bCastleQueenSideAttackMask = 0x1C00000000000000UL;


		public static ulong[] GenerateKingAttacks() {
			ulong[] attacks = new ulong[64];

			for (int i = 0; i < 64; i++) {
				ulong mask = 1UL << i;

				ulong attack = EastOne(mask) | WestOne(mask);
				mask |= attack;
				attack |= NorthOne(mask) | SouthOne(mask);

				attacks[i] = attack;
			}

			return attacks;
		}

		public static ulong[] GenerateKnightAttacks() {
			ulong[] attacks = new ulong[64];

			for (int i = 0; i < 64; i++) {
				ulong mask = 1UL << i;

				ulong attack = noNoEa(mask) | noEaEa(mask) |
				               soEaEa(mask) | soSoEa(mask) |
				               noNoWe(mask) | noWeWe(mask) |
				               soWeWe(mask) | soSoWe(mask);
				attacks[i] = attack;
			}

			return attacks;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong SouthOne (ulong b) {return  b >> 8;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong NorthOne (ulong b) {return  b << 8;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong EastOne (ulong b) {return (b & notHFile) << 1;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong NoEaOne (ulong b) {return (b & notHFile) << 9;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong SoEaOne (ulong b) {return (b & notHFile) >> 7;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong WestOne (ulong b) {return (b & notAFile) >> 1;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong SoWeOne (ulong b) {return (b & notAFile) >> 9;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong NoWeOne (ulong b) {return (b & notAFile) << 7;}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong noNoEa(ulong b) {return (b << 17) & notAFile ;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong noEaEa(ulong b) {return (b << 10) & notABFile;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong soEaEa(ulong b) {return (b >>  6) & notABFile;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong soSoEa(ulong b) {return (b >> 15) & notAFile ;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong noNoWe(ulong b) {return (b << 15) & notHFile ;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong noWeWe(ulong b) {return (b <<  6) & notGHFile;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong soWeWe(ulong b) {return (b >> 10) & notGHFile;}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong soSoWe(ulong b) {return (b >> 17) & notHFile ;}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong ShiftOne (ulong b, int dir8) {
			return BitOperations.RotateLeft(b, Shift[dir8]) & AvoidWrap[dir8];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnsAble2Push(ulong wpawns, ulong empty) {
			return MoveGenHelpers.SouthOne(empty) & wpawns;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnsAble2DblPush(ulong wpawns, ulong empty) {
			const ulong rank4 = 0x00000000FF000000UL;
			ulong emptyRank3 = MoveGenHelpers.SouthOne(empty & rank4) & empty;
			return wPawnsAble2Push(wpawns, emptyRank3);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnsAble2Push(ulong wpawns, ulong empty) {
			return MoveGenHelpers.NorthOne(empty) & wpawns;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnsAble2DblPush(ulong wpawns, ulong empty) {
			const ulong rank5 = 0x000000FF00000000UL;
			ulong emptyRank6 = MoveGenHelpers.NorthOne(empty & rank5) & empty;
			return bPawnsAble2Push(wpawns, emptyRank6);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnEastAttacks(ulong wpawns) {return MoveGenHelpers.NoEaOne(wpawns);}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnWestAttacks(ulong wpawns) {return MoveGenHelpers.NoWeOne(wpawns);}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnEastAttacks(ulong bpawns) {return MoveGenHelpers.SoEaOne(bpawns);}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnWestAttacks(ulong bpawns) {return MoveGenHelpers.SoWeOne(bpawns);}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnAnyAttacks(ulong wpawns) {
			return wPawnEastAttacks(wpawns) | wPawnWestAttacks(wpawns);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnDblAttacks(ulong wpawns) {
			return wPawnEastAttacks(wpawns) & wPawnWestAttacks(wpawns);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnSingleAttacks(ulong wpawns) {
			return wPawnEastAttacks(wpawns) ^ wPawnWestAttacks(wpawns);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnAnyAttacks(ulong bpawns) {
			return bPawnEastAttacks(bpawns) | bPawnWestAttacks(bpawns);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnDblAttacks(ulong bpawns) {
			return bPawnEastAttacks(bpawns) & bPawnWestAttacks(bpawns);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnSingleAttacks(ulong bpawns) {
			return bPawnEastAttacks(bpawns) ^ bPawnWestAttacks(bpawns);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnsAble2CaptureEast(ulong wpawns, ulong bpieces) {
			return wpawns & bPawnWestAttacks(bpieces);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnsAble2CaptureWest(ulong wpawns, ulong bpieces) {
			return wpawns & bPawnEastAttacks(bpieces);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong wPawnsAble2CaptureAny(ulong wpawns, ulong bpieces) {
			return wpawns & bPawnAnyAttacks(bpieces);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnsAble2CaptureEast(ulong bpawns, ulong wpieces) {
			return bpawns & wPawnWestAttacks(wpieces);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnsAble2CaptureWest(ulong bpawns, ulong wpieces) {
			return bpawns & wPawnEastAttacks(wpieces);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong bPawnsAble2CaptureAny(ulong bpawns, ulong wpieces) {
			return bpawns & wPawnAnyAttacks(wpieces);
		}

		
	}
}
