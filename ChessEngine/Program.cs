using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChessEngine
{
	public class Program
	{
		public const string engineName = "Sharp Fox";
		public const string engineAuthor = "Nominom";


		public static ConcurrentQueue<string> inputQueue = new ConcurrentQueue<string>();
		public static ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();
		public static bool initialized = false;
		public static UciEngine engine;

		static void Main(string[] args)
		{
			engine = new UciEngine(outputQueue);
			
			Task.Run(async () =>
			{
				while (true)
				{
					var text = await Console.In.ReadLineAsync();
					inputQueue.Enqueue(text);
				}
			});

			while (true)
			{
				if (inputQueue.TryDequeue(out string cmd))
				{

					var splitCmd = cmd.Split(" ");

					string cmd0 = splitCmd[0];

					if (cmd0 == "uci")
					{
						outputQueue.Enqueue($"id name {engineName}");
						outputQueue.Enqueue($"id author {engineAuthor}");
						outputQueue.Enqueue("option name Hash type spin default 32 min 1 max 4194304");
						outputQueue.Enqueue("option name Clear Hash type button");
						outputQueue.Enqueue("option name Ponder type check default false");
						outputQueue.Enqueue("uciok");
					}
					else if (cmd0 == "isready")
					{
						if (!initialized)
						{
							engine.Initialize();
							initialized = true;
						}
						outputQueue.Enqueue($"readyok");
					}
					else if (cmd0 == "debug") {
						if (splitCmd[1] == "on") {
							Options.DebugMode = true;
						}else if (splitCmd[1] == "off") {
							Options.DebugMode = false;
						}
					}
					else if (cmd0 == "setoption")
					{
						if (!(splitCmd.Length >= 3 && splitCmd[1] == "name"))
						{
							continue;
						}

						int i = 2;
						var name = "";
						for (; i < splitCmd.Length;)
						{
							if (splitCmd[i] == "value") break;
							name += splitCmd[i++];
						}

						if (name == "ClearHash")
						{
							//Clear hash table
						}

						if (!(splitCmd.Length >= i + 2 && splitCmd[i] == "value"))
						{
							continue;
						}

						var value = splitCmd[i + 1];

						if (name == "Hash")
						{
							Options.HashSize = int.Parse(value);
						}
						else if (name == "Ponder")
						{
							Options.Ponder = bool.Parse(value);
						}
					}
					else if (cmd0 == "ucinewgame") {
						engine.UciNewGame();
					}
					else if (cmd0 == "position") {
						string fen = "startpos";
						List<string> moves = new List<string>();
						bool listMoves = false;
						for (int i = 1; i < splitCmd.Length; i++) {
							if (listMoves) {
								moves.Add(splitCmd[i]);
							}else if (splitCmd[i] == "startpos") {
								fen = "startpos";
							}else if (splitCmd[i] == "fen") {
								fen = splitCmd[++i];
							}else if (splitCmd[i] == "moves") {
								listMoves = true;
							}
						}
						engine.SetPosition(fen, moves);
					}else if (cmd0 == "go") {
						List<string> options = new List<string>();
						for (int i = 1; i < splitCmd.Length; i++) {
							options.Add(splitCmd[i]);
						}
						engine.Go(options);
					}else if (cmd0 == "stop") {
						engine.Stop();
					}else if (cmd0 == "ponderhit") {
						engine.PonderHit();
					}else if (cmd0 == "quit") {
						break;
					}

				}
				else if (outputQueue.TryDequeue(out string outLine))
				{
					Console.WriteLine(outLine);
				}
				else
				{
					Thread.Sleep(10);
				}
			}
		}
	}
}
