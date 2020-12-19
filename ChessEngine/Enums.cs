using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using ChessEngine;
using Microsoft.VisualBasic.CompilerServices;

namespace ChessEngine
{
	public enum Color : byte {
		White = 0,
		Black = 1
	}

	public enum Piece : byte {
		WhiteAll,
		BlackAll,
		WPawn,
		BPawn,
		WKnight,
		BKnight,
		WBishop,
		BBishop,
		WRook,
		BRook,
		WQueen,
		BQueen,
		WKing,
		BKing,
		Empty
	}

	[Flags]
	public enum MoveType : byte {
		Quiet = 0,
		Capture = 1,
		DoublePush = 1 << 1,
		EnPassant = (1 << 2),
		Castling = (1 << 3),
		KingCastle = (1 << 4) | Castling,
		QueenCastle = (1 << 5) | Castling,
		Promotion = (1 << 6),
		PromotionCap = Capture | Promotion
	}

	public static class SquareMask {
		public const ulong H8 = 1UL << 63;
		public const ulong G8 = 1UL << 62;
		public const ulong F8 = 1UL << 61;
		public const ulong E8 = 1UL << 60;
		public const ulong D8 = 1UL << 59;
		public const ulong C8 = 1UL << 58;
		public const ulong B8 = 1UL << 57;
		public const ulong A8 = 1UL << 56;
		public const ulong H7 = 1UL << 55;
		public const ulong G7 = 1UL << 54;
		public const ulong F7 = 1UL << 53;
		public const ulong E7 = 1UL << 52;
		public const ulong D7 = 1UL << 51;
		public const ulong C7 = 1UL << 50;
		public const ulong B7 = 1UL << 49;
		public const ulong A7 = 1UL << 48;
		public const ulong H6 = 1UL << 47;
		public const ulong G6 = 1UL << 46;
		public const ulong F6 = 1UL << 45;
		public const ulong E6 = 1UL << 44;
		public const ulong D6 = 1UL << 43;
		public const ulong C6 = 1UL << 42;
		public const ulong B6 = 1UL << 41;
		public const ulong A6 = 1UL << 40;
		public const ulong H5 = 1UL << 39;
		public const ulong G5 = 1UL << 38;
		public const ulong F5 = 1UL << 37;
		public const ulong E5 = 1UL << 36;
		public const ulong D5 = 1UL << 35;
		public const ulong C5 = 1UL << 34;
		public const ulong B5 = 1UL << 33;
		public const ulong A5 = 1UL << 32;
		public const ulong H4 = 1UL << 31;
		public const ulong G4 = 1UL << 30;
		public const ulong F4 = 1UL << 29;
		public const ulong E4 = 1UL << 28;
		public const ulong D4 = 1UL << 27;
		public const ulong C4 = 1UL << 26;
		public const ulong B4 = 1UL << 25;
		public const ulong A4 = 1UL << 24;
		public const ulong H3 = 1UL << 23;
		public const ulong G3 = 1UL << 22;
		public const ulong F3 = 1UL << 21;
		public const ulong E3 = 1UL << 20;
		public const ulong D3 = 1UL << 19;
		public const ulong C3 = 1UL << 18;
		public const ulong B3 = 1UL << 17;
		public const ulong A3 = 1UL << 16;
		public const ulong H2 = 1UL << 15;
		public const ulong G2 = 1UL << 14;
		public const ulong F2 = 1UL << 13;
		public const ulong E2 = 1UL << 12;
		public const ulong D2 = 1UL << 11;
		public const ulong C2 = 1UL << 10;
		public const ulong B2 = 1UL << 9;
		public const ulong A2 = 1UL << 8;
		public const ulong H1 = 1UL << 7;
		public const ulong G1 = 1UL << 6;
		public const ulong F1 = 1UL << 5;
		public const ulong E1 = 1UL << 4;
		public const ulong D1 = 1UL << 3;
		public const ulong C1 = 1UL << 2;
		public const ulong B1 = 1UL << 1;
		public const ulong A1 = 1UL;
		public const ulong WKingCastlingSquares = E1 | H1;
		public const ulong WQueenCastlingSquares = E1 | A1;
		public const ulong BKingCastlingSquares = E8 | H8;
		public const ulong BQueenCastlingSquares = E8 | A8;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool Any(this ulong mask, ulong value) {
			return (mask & value) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool None(this ulong mask, ulong value) {
			return (mask & value) == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool All(this ulong mask, ulong value) {
			return (mask & value) == mask;
		}
	}

	public struct SquareIndex : IEquatable<SquareIndex> {
		public byte value;

		public SquareIndex(int value) => this.value = (byte)value;
		public SquareIndex(int rank, int file) => this.value = (byte)(8 * rank + file);
		public SquareIndex(byte value) => this.value = value;

		public static implicit operator int(SquareIndex sq) => sq.value;
		public static implicit operator SquareIndex(int val) => new SquareIndex(val);
		public static implicit operator SquareIndex(byte val) => new SquareIndex(val);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public ulong ToMask() {
			return 1UL << value;
		}

		public bool IsNoneSquare => value == None;
		public int Rank => (int)Math.Floor(value / 8.0);
		public int File => value % 8;

		public const int H8 = 63;
		public const int G8 = 62;
		public const int F8 = 61;
		public const int E8 = 60;
		public const int D8 = 59;
		public const int C8 = 58;
		public const int B8 = 57;
		public const int A8 = 56;
		public const int H7 = 55;
		public const int G7 = 54;
		public const int F7 = 53;
		public const int E7 = 52;
		public const int D7 = 51;
		public const int C7 = 50;
		public const int B7 = 49;
		public const int A7 = 48;
		public const int H6 = 47;
		public const int G6 = 46;
		public const int F6 = 45;
		public const int E6 = 44;
		public const int D6 = 43;
		public const int C6 = 42;
		public const int B6 = 41;
		public const int A6 = 40;
		public const int H5 = 39;
		public const int G5 = 38;
		public const int F5 = 37;
		public const int E5 = 36;
		public const int D5 = 35;
		public const int C5 = 34;
		public const int B5 = 33;
		public const int A5 = 32;
		public const int H4 = 31;
		public const int G4 = 30;
		public const int F4 = 29;
		public const int E4 = 28;
		public const int D4 = 27;
		public const int C4 = 26;
		public const int B4 = 25;
		public const int A4 = 24;
		public const int H3 = 23;
		public const int G3 = 22;
		public const int F3 = 21;
		public const int E3 = 20;
		public const int D3 = 19;
		public const int C3 = 18;
		public const int B3 = 17;
		public const int A3 = 16;
		public const int H2 = 15;
		public const int G2 = 14;
		public const int F2 = 13;
		public const int E2 = 12;
		public const int D2 = 11;
		public const int C2 = 10;
		public const int B2 = 9;
		public const int A2 = 8;
		public const int H1 = 7;
		public const int G1 = 6;
		public const int F1 = 5;
		public const int E1 = 4;
		public const int D1 = 3;
		public const int C1 = 2;
		public const int B1 = 1;
		public const int A1 = 0;
		public const byte None = 255;

		public static SquareIndex Parse(string s) => Parse(s.AsSpan());

		public static SquareIndex Parse(ReadOnlySpan<char> s) {
			if (s.Length != 2) {
				return None;
			}
			else {
				var file = s[0] - 'a';
				var rank = s[1] - '1';

				return new SquareIndex(rank * 8 + file);
			}
		}

		public override string ToString() {
			if (value == None) return "-";
			var rank = (char)(Rank + '1');
			var file = (char)(File + 'a');
			return $"{file}{rank}";
		}

		public bool Equals(SquareIndex other) {
			return value == other.value;
		}

		public override bool Equals(object obj) {
			return obj is SquareIndex other && Equals(other);
		}

		public override int GetHashCode() {
			return value.GetHashCode();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool operator ==(SquareIndex left, SquareIndex right) {
			return left.Equals(right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool operator !=(SquareIndex left, SquareIndex right) {
			return !left.Equals(right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public SquareIndex MoveOne(Direction dir) {
			return new SquareIndex((byte)(value + MoveGenHelpers.Shift[(int) dir]));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public SquareIndex OneNorth() {
			 return new SquareIndex((byte)(value + 8));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public SquareIndex TwoNorth() {
			return new SquareIndex((byte)(value + 16));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public SquareIndex OneSouth() {
			 return new SquareIndex((byte)(value - 8));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public SquareIndex TwoSouth() {
			return new SquareIndex((byte)(value - 16));
		}
	}

	public enum Direction : byte {
		NorthEast,
		East,
		SouthEast,
		South, 
		SouthWest,
		West,
		NorthWest,
		North
	}

	public static class DirShift {
		public const int No = 8;
		public const int NoEa = 9;
		public const int NoWe = 7;
		public const int West = -1;
		public const int East = 1;
		public const int So = -8;
		public const int SoEa = -7;
		public const int SoWe = -9;


		public const int NoNoWe = 15;
		public const int NoNoEa = 17;
		public const int NoWeWe = 6;
		public const int NoEaEa = 10;
		public const int SoSoWe = -17;
		public const int SoSoEa = -15;
		public const int SoWeWe = -10;
		public const int SoEaEa = -6;
	}
}



public static class EnumExtensions {

	public static Color Color(this Piece piece) {
		return (Color) ((int) piece & 1);
	}

	public static Color Invert(this Color col) {
		return (Color) (((int)col + 1) & 1);
	}

	public static string ToUtf8Piece(this Piece piece) {
		switch (piece) {
			case Piece.WPawn:
				return "\u265F";
			case Piece.WKnight:
				return "\u265E";
			case Piece.WBishop:
				return "\u265D";
			case Piece.WRook:
				return "\u265C";
			case Piece.WQueen:
				return "\u265B";
			case Piece.WKing:
				return "\u265A";
			case Piece.BPawn:
				return "\u2659";
			case Piece.BKnight:
				return "\u2658";
			case Piece.BBishop:
				return "\u2657";
			case Piece.BRook:
				return "\u2656";
			case Piece.BQueen:
				return "\u2655";
			case Piece.BKing:
				return "\u2654";
			case Piece.Empty:
				return "";
			default:
				throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
		}
	}

	public static string ToLowerCasePieceName(this Piece piece) {
		switch (piece) {
			case Piece.WPawn:
				return "p";
			case Piece.WKnight:
				return "n";
			case Piece.WBishop:
				return "b";
			case Piece.WRook:
				return "r";
			case Piece.WQueen:
				return "q";
			case Piece.WKing:
				return "k";
			case Piece.BPawn:
				return "p";
			case Piece.BKnight:
				return "n";
			case Piece.BBishop:
				return "b";
			case Piece.BRook:
				return "r";
			case Piece.BQueen:
				return "q";
			case Piece.BKing:
				return "k";
			case Piece.Empty:
				return "";
			default:
				throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
		}
	}
}