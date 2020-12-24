using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChessEngine
{
	public class StandardSearch : ISearch
	{
		public Action<string> Output { get; set; }
		public Thread RunningThread { get; set; }
		public bool DisableOutput { get; set; }


		public void Run(BitBoard startingPosition, SearchOptions options) {
			
		}

		public void Stop() {
			
		}
	}
}
