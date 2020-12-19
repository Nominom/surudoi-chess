using System;
using System.Collections.Generic;
using System.Text;
using ChessEngine;
using Xunit;

namespace ChessTests
{
	public class MoveTests
	{
		[Fact]
		public void MakeMoves() {
			BitBoard board = BitBoard.FromFen("startpos");
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.Fen);

			board.MakeMove(Move.FromUciString("e2e4", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", board.Fen);

			board.MakeMove(Move.FromUciString("e7e5", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2", board.Fen);

			board.MakeMove(Move.FromUciString("f1c4", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR b KQkq - 1 2", board.Fen);

			board.MakeMove(Move.FromUciString("d8g5", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/pppp1ppp/8/4p1q1/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 2 3", board.Fen);

			board.MakeMove(Move.FromUciString("g1h3", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/pppp1ppp/8/4p1q1/2B1P3/7N/PPPP1PPP/RNBQK2R b KQkq - 3 3", board.Fen);

			board.MakeMove(Move.FromUciString("b7b5", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/p1pp1ppp/8/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQK2R w KQkq b6 0 4", board.Fen);

			board.MakeMove(Move.FromUciString("e1g1", board));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/p1pp1ppp/8/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQ1RK1 b kq - 1 4", board.Fen);

			board.MakeMove(Move.FromUciString("b8c6", board));
			Assert.True(board.IsValid);
			Assert.Equal("r1b1kbnr/p1pp1ppp/2n5/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQ1RK1 w kq - 2 5", board.Fen);

			board.MakeMove(Move.FromUciString("d2d4", board));
			Assert.True(board.IsValid);
			Assert.Equal("r1b1kbnr/p1pp1ppp/2n5/1p2p1q1/2BPP3/7N/PPP2PPP/RNBQ1RK1 b kq d3 0 5", board.Fen);

			board.MakeMove(Move.FromUciString("c8a6", board));
			Assert.True(board.IsValid);
			Assert.Equal("r3kbnr/p1pp1ppp/b1n5/1p2p1q1/2BPP3/7N/PPP2PPP/RNBQ1RK1 w kq - 1 6", board.Fen);

			board.MakeMove(Move.FromUciString("h3g5", board));
			Assert.True(board.IsValid);
			Assert.Equal("r3kbnr/p1pp1ppp/b1n5/1p2p1N1/2BPP3/8/PPP2PPP/RNBQ1RK1 b kq - 0 6", board.Fen);

			board.MakeMove(Move.FromUciString("e8c8", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/1p2p1N1/2BPP3/8/PPP2PPP/RNBQ1RK1 w - - 1 7", board.Fen);

			board.MakeMove(Move.FromUciString("f2f4", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/1p2p1N1/2BPPP2/8/PPP3PP/RNBQ1RK1 b - f3 0 7", board.Fen);

			board.MakeMove(Move.FromUciString("b5c4", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/2pPPP2/8/PPP3PP/RNBQ1RK1 w - - 0 8", board.Fen);

			board.MakeMove(Move.FromUciString("b2b4", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/1PpPPP2/8/P1P3PP/RNBQ1RK1 b - b3 0 8", board.Fen);

			board.MakeMove(Move.FromUciString("c4b3", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/1p6/P1P3PP/RNBQ1RK1 w - - 0 9", board.Fen);

			board.MakeMove(Move.FromUciString("b1c3", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/1pN5/P1P3PP/R1BQ1RK1 b - - 1 9", board.Fen);

			board.MakeMove(Move.FromUciString("b3b2", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/2N5/PpP3PP/R1BQ1RK1 w - - 0 10", board.Fen);

			board.MakeMove(Move.FromUciString("h2h4", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP1P/2N5/PpP3P1/R1BQ1RK1 b - h3 0 10", board.Fen);

			board.MakeMove(Move.FromUciString("b2b1q", board));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP1P/2N5/P1P3P1/RqBQ1RK1 w - - 0 11", board.Fen);

		}

		[Fact]
		public void UnMakeMoves() {
			Stack<UnMove> moves = new Stack<UnMove>();

			BitBoard board = BitBoard.FromFen("startpos");
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("e2e4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("e7e5", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("f1c4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR b KQkq - 1 2", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("d8g5", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/pppp1ppp/8/4p1q1/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 2 3", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("g1h3", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/pppp1ppp/8/4p1q1/2B1P3/7N/PPPP1PPP/RNBQK2R b KQkq - 3 3", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b7b5", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/p1pp1ppp/8/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQK2R w KQkq b6 0 4", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("e1g1", board)));
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/p1pp1ppp/8/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQ1RK1 b kq - 1 4", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b8c6", board)));
			Assert.True(board.IsValid);
			Assert.Equal("r1b1kbnr/p1pp1ppp/2n5/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQ1RK1 w kq - 2 5", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("d2d4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("r1b1kbnr/p1pp1ppp/2n5/1p2p1q1/2BPP3/7N/PPP2PPP/RNBQ1RK1 b kq d3 0 5", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("c8a6", board)));
			Assert.True(board.IsValid);
			Assert.Equal("r3kbnr/p1pp1ppp/b1n5/1p2p1q1/2BPP3/7N/PPP2PPP/RNBQ1RK1 w kq - 1 6", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("h3g5", board)));
			Assert.True(board.IsValid);
			Assert.Equal("r3kbnr/p1pp1ppp/b1n5/1p2p1N1/2BPP3/8/PPP2PPP/RNBQ1RK1 b kq - 0 6", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("e8c8", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/1p2p1N1/2BPP3/8/PPP2PPP/RNBQ1RK1 w - - 1 7", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("f2f4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/1p2p1N1/2BPPP2/8/PPP3PP/RNBQ1RK1 b - f3 0 7", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b5c4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/2pPPP2/8/PPP3PP/RNBQ1RK1 w - - 0 8", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b2b4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/1PpPPP2/8/P1P3PP/RNBQ1RK1 b - b3 0 8", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("c4b3", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/1p6/P1P3PP/RNBQ1RK1 w - - 0 9", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b1c3", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/1pN5/P1P3PP/R1BQ1RK1 b - - 1 9", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b3b2", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/2N5/PpP3PP/R1BQ1RK1 w - - 0 10", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("h2h4", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP1P/2N5/PpP3P1/R1BQ1RK1 b - h3 0 10", board.Fen);

			moves.Push(board.MakeMove(Move.FromUciString("b2b1q", board)));
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP1P/2N5/P1P3P1/RqBQ1RK1 w - - 0 11", board.Fen);



			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP1P/2N5/PpP3P1/R1BQ1RK1 b - h3 0 10", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/2N5/PpP3PP/R1BQ1RK1 w - - 0 10", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/1pN5/P1P3PP/R1BQ1RK1 b - - 1 9", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/3PPP2/1p6/P1P3PP/RNBQ1RK1 w - - 0 9", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/1PpPPP2/8/P1P3PP/RNBQ1RK1 b - b3 0 8", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/4p1N1/2pPPP2/8/PPP3PP/RNBQ1RK1 w - - 0 8", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/1p2p1N1/2BPPP2/8/PPP3PP/RNBQ1RK1 b - f3 0 7", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("2kr1bnr/p1pp1ppp/b1n5/1p2p1N1/2BPP3/8/PPP2PPP/RNBQ1RK1 w - - 1 7", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("r3kbnr/p1pp1ppp/b1n5/1p2p1N1/2BPP3/8/PPP2PPP/RNBQ1RK1 b kq - 0 6", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("r3kbnr/p1pp1ppp/b1n5/1p2p1q1/2BPP3/7N/PPP2PPP/RNBQ1RK1 w kq - 1 6", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("r1b1kbnr/p1pp1ppp/2n5/1p2p1q1/2BPP3/7N/PPP2PPP/RNBQ1RK1 b kq d3 0 5", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("r1b1kbnr/p1pp1ppp/2n5/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQ1RK1 w kq - 2 5", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/p1pp1ppp/8/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQ1RK1 b kq - 1 4", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/p1pp1ppp/8/1p2p1q1/2B1P3/7N/PPPP1PPP/RNBQK2R w KQkq b6 0 4", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/pppp1ppp/8/4p1q1/2B1P3/7N/PPPP1PPP/RNBQK2R b KQkq - 3 3", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnb1kbnr/pppp1ppp/8/4p1q1/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 2 3", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR b KQkq - 1 2", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", board.Fen);

			board.UnMakeMove(moves.Pop());
			Assert.True(board.IsValid);
			Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.Fen);
		}
	}
}
