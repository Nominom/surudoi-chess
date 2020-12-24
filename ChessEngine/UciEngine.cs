using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChessEngine
{
	public class SearchOptions {
		public bool ponder = false;
		public bool infinite = false;
		public bool perft = false;
		public bool limitMoves = false;
		public int wTime = 0;
		public int bTime = 0;
		public int wInc = 0;
		public int bInc = 0;
		public int movesToGo = 0;
		public int depth = 0;
		public ulong nodes = 0;
		public int mate = 0;
		public int moveTime = 0;
		public List<string> limitedMovesList;
	}

	public class UciEngine {
		public BitBoard currentPosition;
		public ConcurrentQueue<string> outputQueue;
		public ISearch currentSearch;

		public UciEngine(ConcurrentQueue<string> outputQueue) {
			this.outputQueue = outputQueue;
		}

		public void Initialize() {
			currentPosition = BitBoard.InitialPosition();
		}

		public void UciNewGame() {
			//Clear transposition table and stuff
			currentPosition = BitBoard.InitialPosition();
		}

		public void SetPosition(string fen, List<string> moves) {

			currentPosition = BitBoard.FromFen(fen);

			foreach (string move in moves) {
				Move mv = Move.FromUciString(move, currentPosition);
				currentPosition.MakeMove(mv);
			}
		}

		public void Go(List<string> options) {
			SearchOptions searchOptions = new SearchOptions();

			for (int i = 0; i < options.Count; i++) {
				if (options[i] == "perft") {
					searchOptions.perft = true;
					searchOptions.depth = int.Parse(options[++i]);
				}else if (options[i] == "searchmoves") {
					searchOptions.limitMoves = true;
					searchOptions.limitedMovesList = new List<string>(options.Count - i);
					for (; i < options.Count; i++) {
						searchOptions.limitedMovesList.Add(options[i]);
					}
				}else if (options[i] == "ponder") {
					searchOptions.ponder = true;
				}else if (options[i] == "infinite") {
					searchOptions.infinite = true;
				}else if (options[i] == "wtime") {
					searchOptions.wTime = int.Parse(options[++i]);
				}else if (options[i] == "btime") {
					searchOptions.bTime = int.Parse(options[++i]);
				}else if (options[i] == "winc") {
					searchOptions.wInc = int.Parse(options[++i]);
				}else if (options[i] == "binc") {
					searchOptions.bInc = int.Parse(options[++i]);
				}else if (options[i] == "movestogo") {
					searchOptions.movesToGo = int.Parse(options[++i]);
				}else if (options[i] == "depth") {
					searchOptions.depth = int.Parse(options[++i]);
				}else if (options[i] == "nodes") {
					searchOptions.nodes = ulong.Parse(options[++i]);
				}else if (options[i] == "mate") {
					searchOptions.mate = int.Parse(options[++i]);
				}else if (options[i] == "movetime") {
					searchOptions.moveTime = int.Parse(options[++i]);
				}
			}

			if (searchOptions.perft) {
				currentSearch = new PerftSearch();
				currentSearch.Output = Output;
				
				Thread t = new Thread(() => {
					currentSearch.Run(currentPosition.Copy(), searchOptions);
				});

				t.IsBackground = true;
				currentSearch.RunningThread = t;
				currentSearch.DisableOutput = false;

				t.Start();
			}
			else {
				// Normal search
			}
		}

		private void Output(string str) {
			outputQueue.Enqueue(str);
		}

		public void Stop() {
			currentSearch.Stop();
		}

		public void PonderHit() {

		}
	}
}
