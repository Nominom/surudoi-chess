using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine
{
	public static class Options {
		private static int hashSize = 32;

		public static int HashSize {
			get => hashSize;
			set {
				hashSize = value;
			}
		}
		public static bool Ponder { get; set; } = false;
		public static bool DebugMode { get; set; } = false;
	}
}
