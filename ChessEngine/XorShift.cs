using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine
{
	public class XorShift
	{
		private const ulong multiplier = 53;
		private ulong state;

		public XorShift(ulong seed) {
			state = seed;
			Next();
		}

		public XorShift() : this((ulong)DateTime.Now.Ticks) { }

		public ulong Next()
		{
			unchecked
			{
				ulong x = state;
				x ^= x << 13;
				x ^= x >> 7;
				x ^= x << 17;
				state = x * multiplier;
				return state;
			}
		}
	}
}
