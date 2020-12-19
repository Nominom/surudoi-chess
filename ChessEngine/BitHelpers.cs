using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace ChessEngine
{
	public static class BitHelpers
	{
		private const ulong DEBRUIJN64 = 0x03f79d71b4cb0a89;
		private static readonly byte[] INDEX64 = 
		{
			0, 47,  1, 56, 48, 27,  2, 60,
			57, 49, 41, 37, 28, 16,  3, 61,
			54, 58, 35, 52, 50, 42, 21, 44,
			38, 32, 29, 23, 17, 11,  4, 62,
			46, 55, 26, 59, 40, 36, 15, 53,
			34, 51, 20, 43, 31, 22, 10, 45,
			25, 39, 14, 33, 19, 30,  9, 24,
			13, 18,  8, 12,  7,  6,  5, 63
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte BitScanForward(ulong bitmap)
		{
			Debug.Assert(bitmap != 0);
			return INDEX64[((bitmap ^ (bitmap - 1)) * DEBRUIJN64) >> 58];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong PopCount(ulong bitmap) {
			if (Popcnt.X64.IsSupported) {
				return Popcnt.X64.PopCount(bitmap);
			}
			else {
				ulong result = bitmap - ((bitmap >> 1) & 0x5555555555555555UL);
				result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
				return (byte)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
			}
		}
	}
}
