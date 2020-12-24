using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChessEngine
{
	public interface ISearch
	{
		public Action<string> Output { get; set; }
		public Thread RunningThread { get; set; }
		public bool DisableOutput { get; set; }
		void Run(BitBoard startingPosition, SearchOptions options);
		void Stop();
	}
}
