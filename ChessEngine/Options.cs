using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine
{
	public static class Options {
		public static int HashSize { get; set; } = 32;
		public static bool Ponder { get; set; } = false;
	}
}
