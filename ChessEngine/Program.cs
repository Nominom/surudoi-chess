using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChessEngine
{
	public class Program {

		public const string engineName = "Sharp Fox";
		public const string engineAuthor = "Nominom";


		public static ConcurrentQueue<string> inputQueue = new ConcurrentQueue<string>();
		public static ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();
		public static bool initialized = false;

		static void Main(string[] args)
		{
			BitBoard board = BitBoard.FromFen("startpos");

			var perftList = board.PerftList(6);
			ulong perftResult = 0UL;

			foreach((var mv, var ul) in perftList)
			{
				Console.WriteLine($"{mv}: {ul}");
				perftResult += ul;
			}
			Console.WriteLine("Nodes searched: " + perftResult);

			Task.Run(async () =>
			{
				while (true)
				{
					var text = await Console.In.ReadLineAsync();
					inputQueue.Enqueue(text);
				}
			});

			while (true) {
				if (inputQueue.TryDequeue(out string cmd)) {

					var splitCmd = cmd.Split(" ");

					string cmd0 = splitCmd[0];

					if (cmd0 == "uci") {
						outputQueue.Enqueue($"id name {engineName}");
						outputQueue.Enqueue($"id name {engineAuthor}");
						outputQueue.Enqueue("option name Hash type spin default 32 min 1 max 33554432");
						outputQueue.Enqueue("option name Clear Hash type button");
						outputQueue.Enqueue("option name Ponder type check default false");
						outputQueue.Enqueue("uciok");
					}else if (cmd0 == "isready") {
						if (!initialized) {
							//Intialize
							initialized = true;
						}
						outputQueue.Enqueue($"readyok");
					}else if (cmd0 == "setoption") {
						if (!(splitCmd.Length >= 3 && splitCmd[1] == "name")) {
							continue;
						}

						int i = 2;
						var name = "";
						for (; i < splitCmd.Length;) {
							if (splitCmd[i] == "value") break;
							name += splitCmd[i++];
						}

						if (name == "ClearHash") {
							//Clear hash table
						}

						if (!(splitCmd.Length >= i + 2 && splitCmd[i] == "value")) {
							continue;
						}

						var value = splitCmd[i + 1];

						if (name == "Hash") {
							Options.HashSize = int.Parse(value);
						}
						else if (name == "Ponder") {
							Options.Ponder = bool.Parse(value);
						}
					}

				}else if (outputQueue.TryDequeue(out string outLine)) {
					Console.WriteLine(outLine);
				}
				else {
					Thread.Sleep(10);
				}
			}
		}
	}
}
