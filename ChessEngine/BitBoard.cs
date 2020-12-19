using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessEngine
{


	public struct BitBoard
	{
		public ulong[] pieces;
		public Piece[] mailbox;

		public string Fen => ToFen();
		public bool IsValid => CheckState();

		public ulong occupied;
		public ulong empty;

		public ulong wAttacks;
		public ulong bAttacks;
		public ulong wSliderAttacks;
		public ulong bSliderAttacks;
		public ulong wStaticAttacks;
		public ulong bStaticAttacks;

		public bool wKingSideAllowed;
		public bool wQueenSideAllowed;
		public bool bKingSideAllowed;
		public bool bQueenSideAllowed;

		public Color sideToMove;
		public SquareIndex enPassantTargetSquare;
		public int halfMovesSinceCapOrPawn;
		public int fullMoveNum;

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public UnMove MakeMove(Move move)
		{
			UnMove unMove = new UnMove()
			{
				move = move,
				wKingSideAllowed = wKingSideAllowed,
				wQueenSideAllowed = wQueenSideAllowed,
				bKingSideAllowed = bKingSideAllowed,
				bQueenSideAllowed = bQueenSideAllowed,
				enPassantTargetSquare = enPassantTargetSquare,
				halfMovesSinceCapOrPawn = halfMovesSinceCapOrPawn,
				wStaticAttacks = wStaticAttacks,
				wSliderAttacks = wSliderAttacks,
				bStaticAttacks = bStaticAttacks,
				bSliderAttacks = bSliderAttacks
			};

			int cI = (int)sideToMove; // color index
			int ocI = (cI + 1) & 1; // opponent color index
			ulong selfSliders = pieces[(int)Piece.WRook + cI] | pieces[(int)Piece.WBishop + cI] |
								pieces[(int)Piece.WQueen + cI];
			ulong selfStatics = pieces[cI] & ~selfSliders;
			ulong oppSliders = pieces[(int)Piece.WRook + ocI] | pieces[(int)Piece.WBishop + ocI] |
							   pieces[(int)Piece.WQueen + ocI];
			ulong oppStatics = pieces[ocI] & ~oppSliders;

			halfMovesSinceCapOrPawn++;

			ulong startMask = move.GetStartSquareMask();
			ulong endMask = move.GetEndSquareMask();
			ulong bothMask = startMask | endMask;

			pieces[(int)move.piece] ^= bothMask;
			pieces[(int)move.GetColor()] ^= bothMask;
			occupied ^= bothMask;
			empty ^= bothMask;
			mailbox[move.startSquareIdx] = Piece.Empty;
			mailbox[move.endSquareIdx] = move.piece;

			if (move.IsEnPassant())
			{
				var eatenSquare = new SquareIndex(move.startSquareIdx.Rank, move.endSquareIdx.File);
				var enPassantMask = eatenSquare.ToMask();

				pieces[(int)move.cPiece] ^= enPassantMask;
				pieces[(int)move.GetCapColor()] ^= enPassantMask;
				occupied ^= enPassantMask;
				empty ^= enPassantMask;
				mailbox[eatenSquare] = Piece.Empty;
			}
			else if (move.IsPromotion())
			{
				pieces[(int)move.piece] ^= endMask;
				pieces[(int)move.promoteTo] ^= endMask;
				mailbox[move.endSquareIdx] = move.promoteTo;
			}
			if (move.IsCapture())
			{
				pieces[(int)move.cPiece] ^= endMask;
				pieces[(int)move.GetCapColor()] ^= endMask;
				occupied ^= endMask;
				empty ^= endMask;
				halfMovesSinceCapOrPawn = 0;
			}
			if (move.IsCastling())
			{
				if (move.IsKingSideCastle())
				{
					if (move.GetColor() == Color.White)
					{
						var rookMask = SquareMask.H1 | SquareMask.F1;
						pieces[(int)Piece.WRook] ^= rookMask;
						pieces[(int)Piece.WhiteAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.H1] = Piece.Empty;
						mailbox[SquareIndex.F1] = Piece.WRook;
					}
					else
					{
						var rookMask = SquareMask.H8 | SquareMask.F8;
						pieces[(int)Piece.BRook] ^= rookMask;
						pieces[(int)Piece.BlackAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.H8] = Piece.Empty;
						mailbox[SquareIndex.F8] = Piece.BRook;
					}
				}
				else
				{
					if (move.GetColor() == Color.White)
					{
						var rookMask = SquareMask.A1 | SquareMask.D1;
						pieces[(int)Piece.WRook] ^= rookMask;
						pieces[(int)Piece.WhiteAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.A1] = Piece.Empty;
						mailbox[SquareIndex.D1] = Piece.WRook;
					}
					else
					{
						var rookMask = SquareMask.A8 | SquareMask.D8;
						pieces[(int)Piece.BRook] ^= rookMask;
						pieces[(int)Piece.BlackAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.A8] = Piece.Empty;
						mailbox[SquareIndex.D8] = Piece.BRook;
					}
				}
			}

			if (move.piece == Piece.WPawn || move.piece == Piece.BPawn)
			{
				halfMovesSinceCapOrPawn = 0;
			}

			if (move.IsDoublePush())
			{
				int startRank = move.startSquareIdx.Rank;
				int endRank = move.endSquareIdx.Rank;
				int file = move.startSquareIdx.File;

				enPassantTargetSquare = new SquareIndex((startRank + endRank) / 2, file);
			}
			else
			{
				enPassantTargetSquare = SquareIndex.None;
			}

			wKingSideAllowed = wKingSideAllowed && (bothMask.None(SquareMask.WKingCastlingSquares));
			wQueenSideAllowed = wQueenSideAllowed && (bothMask.None(SquareMask.WQueenCastlingSquares));
			bKingSideAllowed = bKingSideAllowed && (bothMask.None(SquareMask.BKingCastlingSquares));
			bQueenSideAllowed = bQueenSideAllowed && (bothMask.None(SquareMask.BQueenCastlingSquares));

			if (sideToMove == Color.White)
			{
				bool rebuildSelfStatics = (startMask & selfStatics) > 0;
				bool rebuildSelfSliders = ((startMask & selfSliders) | 
				                           (bothMask & wSliderAttacks)) > 0 || 
				                          (move.moveType & (MoveType.Castling | MoveType.Promotion)) != 0;

				bool rebuildOppStatics = (endMask & oppStatics) > 0 || (move.moveType & MoveType.EnPassant) != 0;
				bool rebuildOppSliders = ((endMask & oppSliders) |
										   (bothMask & bSliderAttacks)) > 0;

				if (rebuildSelfStatics) wStaticAttacks = MoveGen.GenerateWhiteStaticAttackMaps(this);
				if (rebuildSelfSliders) wSliderAttacks = MoveGen.GenerateWhiteSliderAttackMaps(this);
				if (rebuildOppStatics) bStaticAttacks = MoveGen.GenerateBlackStaticAttackMaps(this);
				if (rebuildOppSliders) bSliderAttacks = MoveGen.GenerateBlackSliderAttackMaps(this);
			}
			else
			{
				bool rebuildSelfStatics = (startMask & selfStatics) > 0;
				bool rebuildSelfSliders = ((startMask & selfSliders) | 
				                           (bothMask & bSliderAttacks)) > 0 || 
				                          (move.moveType & (MoveType.Castling | MoveType.Promotion)) != 0;
				bool rebuildOppStatics = (endMask & oppStatics) > 0 || (move.moveType & MoveType.EnPassant) != 0;
				bool rebuildOppSliders = ((endMask & oppSliders) |
										  (bothMask & wSliderAttacks)) > 0;

				if (rebuildSelfStatics) bStaticAttacks = MoveGen.GenerateBlackStaticAttackMaps(this);
				if (rebuildSelfSliders) bSliderAttacks = MoveGen.GenerateBlackSliderAttackMaps(this);
				if (rebuildOppStatics) wStaticAttacks = MoveGen.GenerateWhiteStaticAttackMaps(this);
				if (rebuildOppSliders) wSliderAttacks = MoveGen.GenerateWhiteSliderAttackMaps(this);
			}
			wAttacks = wStaticAttacks | wSliderAttacks;
			bAttacks = bStaticAttacks | bSliderAttacks;
#if DEBUG
			if (Debugger.IsAttached) {
				var exwStaticAttacks = MoveGen.GenerateWhiteStaticAttackMaps(this);
				var exwSliderAttacks = MoveGen.GenerateWhiteSliderAttackMaps(this);
				var exbStaticAttacks = MoveGen.GenerateBlackStaticAttackMaps(this);
				var exbSliderAttacks = MoveGen.GenerateBlackSliderAttackMaps(this);
				if (exwSliderAttacks != wSliderAttacks || exwStaticAttacks != wStaticAttacks ||
				    exbSliderAttacks != bSliderAttacks || exbStaticAttacks != bStaticAttacks) {
					Debugger.Break();
				}
			}
#endif

			fullMoveNum += (int)sideToMove; // Increment full move num if changing from black to white
			sideToMove = (Color)(((int)sideToMove + 1) & 1); // Change side to Move


			if (sideToMove == Color.White && wAttacks.Any(pieces[(int)Piece.BKing]))
			{
				unMove.wasLegal = false;
			}
			else if (sideToMove == Color.Black && bAttacks.Any(pieces[(int)Piece.WKing]))
			{
				unMove.wasLegal = false;
			}
			else
			{
				unMove.wasLegal = true;
			}

			return unMove;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public void UnMakeMove(UnMove unMove)
		{
			Move move = unMove.move;

			wKingSideAllowed = unMove.wKingSideAllowed;
			wQueenSideAllowed = unMove.wQueenSideAllowed;
			bKingSideAllowed = unMove.bKingSideAllowed;
			bQueenSideAllowed = unMove.bQueenSideAllowed;
			halfMovesSinceCapOrPawn = unMove.halfMovesSinceCapOrPawn;
			enPassantTargetSquare = unMove.enPassantTargetSquare;

			wStaticAttacks = unMove.wStaticAttacks;
			wSliderAttacks = unMove.wSliderAttacks;
			bStaticAttacks = unMove.bStaticAttacks;
			bSliderAttacks = unMove.bSliderAttacks;
			wAttacks = wStaticAttacks | wSliderAttacks;
			bAttacks = bStaticAttacks | bSliderAttacks;

			var startMask = move.GetStartSquareMask();
			var endMask = move.GetEndSquareMask();
			var bothMask = startMask | endMask;

			mailbox[move.endSquareIdx] = Piece.Empty;

			if (move.IsEnPassant())
			{
				var eatenSquare = new SquareIndex(move.startSquareIdx.Rank, move.endSquareIdx.File);
				var enPassantMask = eatenSquare.ToMask();

				pieces[(int)move.cPiece] ^= enPassantMask;
				pieces[(int)move.GetCapColor()] ^= enPassantMask;
				occupied ^= enPassantMask;
				empty ^= enPassantMask;
				mailbox[eatenSquare] = move.cPiece;
			}
			else if (move.IsPromotion())
			{
				pieces[(int)move.piece] ^= endMask;
				pieces[(int)move.promoteTo] ^= endMask;
			}
			if (move.IsCapture())
			{
				pieces[(int)move.cPiece] ^= endMask;
				pieces[(int)move.GetCapColor()] ^= endMask;
				mailbox[move.endSquareIdx] = move.cPiece;
				occupied ^= endMask;
				empty ^= endMask;
			}
			if (move.IsCastling())
			{
				if (move.IsKingSideCastle())
				{
					if (move.GetColor() == Color.White)
					{
						var rookMask = SquareMask.H1 | SquareMask.F1;
						pieces[(int)Piece.WRook] ^= rookMask;
						pieces[(int)Piece.WhiteAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.H1] = Piece.WRook;
						mailbox[SquareIndex.F1] = Piece.Empty;
					}
					else
					{
						var rookMask = SquareMask.H8 | SquareMask.F8;
						pieces[(int)Piece.BRook] ^= rookMask;
						pieces[(int)Piece.BlackAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.H8] = Piece.BRook;
						mailbox[SquareIndex.F8] = Piece.Empty;
					}
				}
				else
				{
					if (move.GetColor() == Color.White)
					{
						var rookMask = SquareMask.A1 | SquareMask.D1;
						pieces[(int)Piece.WRook] ^= rookMask;
						pieces[(int)Piece.WhiteAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.A1] = Piece.WRook;
						mailbox[SquareIndex.D1] = Piece.Empty;
					}
					else
					{
						var rookMask = SquareMask.A8 | SquareMask.D8;
						pieces[(int)Piece.BRook] ^= rookMask;
						pieces[(int)Piece.BlackAll] ^= rookMask;
						occupied ^= rookMask;
						empty ^= rookMask;
						mailbox[SquareIndex.A8] = Piece.BRook;
						mailbox[SquareIndex.D8] = Piece.Empty;
					}
				}
			}

			pieces[(int)move.piece] ^= bothMask;
			pieces[(int)move.GetColor()] ^= bothMask;
			occupied ^= bothMask;
			empty ^= bothMask;
			mailbox[move.startSquareIdx] = move.piece;


			sideToMove = (Color)(((int)sideToMove + 1) & 1); // Change side to Move
			fullMoveNum -= (int)sideToMove; // Increment full move num if changed from white to black

		}

		public string ToFen()
		{
			string ss = "";

			int consecutiveEmpties = 0;
			for (int i = 0; i < 64; i++)
			{

				int rank = 7 - (int)Math.Floor(i / 8.0);
				int file = i % 8;

				ulong mask = 1UL << (rank * 8 + file);

				if (i > 0 && (i % 8) == 0)
				{
					if (consecutiveEmpties > 0)
					{
						ss += consecutiveEmpties.ToString();
						consecutiveEmpties = 0;
					}
					ss += '/';
				}
				if (mask.Any(this.occupied) && consecutiveEmpties != 0)
				{
					ss += consecutiveEmpties.ToString();
					consecutiveEmpties = 0;
				}
				if (mask.Any(this.empty))
				{
					consecutiveEmpties++;
				}
				else
				{
					if (mask.Any(pieces[(int)Piece.WhiteAll]))
					{
						//we know it's white

						if (mask.Any(pieces[(int)Piece.WPawn]))
						{
							ss += "P";
						}
						else if (mask.Any(pieces[(int)Piece.WRook]))
						{
							ss += "R";
						}
						else if (mask.Any(pieces[(int)Piece.WBishop]))
						{
							ss += "B";
						}
						else if (mask.Any(pieces[(int)Piece.WKnight]))
						{
							ss += "N";
						}
						else if (mask.Any(pieces[(int)Piece.WQueen]))
						{
							ss += "Q";
						}
						else if (mask.Any(pieces[(int)Piece.WKing]))
						{
							ss += "K";
						}

					}
					else
					{
						//we know it's black

						if (mask.Any(pieces[(int)Piece.BPawn]))
						{
							ss += "p";
						}
						else if (mask.Any(pieces[(int)Piece.BRook]))
						{
							ss += "r";
						}
						else if (mask.Any(pieces[(int)Piece.BBishop]))
						{
							ss += "b";
						}
						else if (mask.Any(pieces[(int)Piece.BKnight]))
						{
							ss += "n";
						}
						else if (mask.Any(pieces[(int)Piece.BQueen]))
						{
							ss += "q";
						}
						else if (mask.Any(pieces[(int)Piece.BKing]))
						{
							ss += "k";
						}
					}
				}
			}
			if (consecutiveEmpties > 0)
			{
				ss += consecutiveEmpties.ToString();
			}

			ss += sideToMove == Color.White ? " w" : " b";

			if (wKingSideAllowed || wQueenSideAllowed || bKingSideAllowed || bQueenSideAllowed)
			{
				ss += " ";
				if (wKingSideAllowed)
				{
					ss += 'K';
				}
				if (wQueenSideAllowed)
				{
					ss += 'Q';
				}
				if (bKingSideAllowed)
				{
					ss += 'k';
				}
				if (bQueenSideAllowed)
				{
					ss += 'q';
				}
			}
			else
			{
				ss += " -";
			}

			ss += $" {enPassantTargetSquare} {halfMovesSinceCapOrPawn} {fullMoveNum}";

			return ss;
		}
		public override string ToString() => Fen;
		public static BitBoard FromFen(string fen)
		{
			if (fen == "startpos")
			{
				return InitialPosition();
			}

			var fenParts = fen.Split(" ");

			BitBoard b = new BitBoard();
			b.pieces = new ulong[14];
			b.mailbox = new Piece[64];
			int i = 0;

			foreach (char c in fenParts[0])
			{
				if (c == ' ' || c == '/') continue;
				if (char.IsDigit(c))
				{
					int skip = c - '0';
					i += skip;
					continue;
				}

				int rank = 7 - (int)Math.Floor(i / 8.0);
				int file = i % 8;

				ulong mask = 1UL << (rank * 8 + file);

				switch (c)
				{
					case 'P':
						b.pieces[(int)Piece.WPawn] |= mask;
						break;
					case 'R':
						b.pieces[(int)Piece.WRook] |= mask;
						break;
					case 'B':
						b.pieces[(int)Piece.WBishop] |= mask;
						break;
					case 'N':
						b.pieces[(int)Piece.WKnight] |= mask;
						break;
					case 'Q':
						b.pieces[(int)Piece.WQueen] |= mask;
						break;
					case 'K':
						b.pieces[(int)Piece.WKing] |= mask;
						break;
					case 'p':
						b.pieces[(int)Piece.BPawn] |= mask;
						break;
					case 'r':
						b.pieces[(int)Piece.BRook] |= mask;
						break;
					case 'b':
						b.pieces[(int)Piece.BBishop] |= mask;
						break;
					case 'n':
						b.pieces[(int)Piece.BKnight] |= mask;
						break;
					case 'q':
						b.pieces[(int)Piece.BQueen] |= mask;
						break;
					case 'k':
						b.pieces[(int)Piece.BKing] |= mask;
						break;
				}

				i++;
			}

			b.pieces[(int)Color.White] = 0;
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WPawn];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WRook];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WKnight];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WBishop];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WQueen];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WKing];

			b.pieces[(int)Color.Black] = 0;
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BPawn];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BRook];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BKnight];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BBishop];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BQueen];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BKing];

			b.occupied = b.pieces[(int)Color.White] | b.pieces[(int)Color.Black];
			b.empty = ~b.occupied;

			b.wAttacks = 0;
			b.bAttacks = 0;

			b.sideToMove = fenParts[1] == "w" ? Color.White : Color.Black;

			if (fenParts.Length == 6)
			{
				string castlings = fenParts[2];

				b.wKingSideAllowed = castlings.Contains('K');
				b.wQueenSideAllowed = castlings.Contains('Q');
				b.bKingSideAllowed = castlings.Contains('k');
				b.bQueenSideAllowed = castlings.Contains('q');

				if (fenParts[3] == "-")
				{
					b.enPassantTargetSquare = SquareIndex.None;
				}
				else
				{
					b.enPassantTargetSquare = SquareIndex.Parse(fenParts[3]);
				}

				b.halfMovesSinceCapOrPawn = int.Parse(fenParts[4]);
				b.fullMoveNum = int.Parse(fenParts[5]);
			}
			else if (fenParts.Length == 5)
			{
				if (fenParts[2] == "-")
				{
					b.enPassantTargetSquare = SquareIndex.None;
				}
				else
				{
					b.enPassantTargetSquare = SquareIndex.Parse(fenParts[2]);
				}

				b.halfMovesSinceCapOrPawn = int.Parse(fenParts[3]);
				b.fullMoveNum = int.Parse(fenParts[4]);
			}

			b.RebuildMailbox();

			(b.wAttacks, b.wStaticAttacks, b.wSliderAttacks) = MoveGen.GenerateWhiteAttackMaps(b);
			(b.bAttacks, b.bStaticAttacks, b.bSliderAttacks) = MoveGen.GenerateBlackAttackMaps(b);

			return b;
		}
		public static BitBoard InitialPosition()
		{
			BitBoard b = new BitBoard();
			b.pieces = new ulong[14];
			b.mailbox = new Piece[64];

			b.pieces[(int)Piece.WPawn] = MoveGenHelpers.Ranks[1];
			b.pieces[(int)Piece.BPawn] = MoveGenHelpers.Ranks[6];

			b.pieces[(int)Piece.WRook] = SquareMask.A1 | SquareMask.H1;
			b.pieces[(int)Piece.BRook] = SquareMask.A8 | SquareMask.H8;

			b.pieces[(int)Piece.WKnight] = SquareMask.B1 | SquareMask.G1;
			b.pieces[(int)Piece.BKnight] = SquareMask.B8 | SquareMask.G8;

			b.pieces[(int)Piece.WBishop] = SquareMask.C1 | SquareMask.F1;
			b.pieces[(int)Piece.BBishop] = SquareMask.C8 | SquareMask.F8;

			b.pieces[(int)Piece.WQueen] = SquareMask.D1;
			b.pieces[(int)Piece.BQueen] = SquareMask.D8;

			b.pieces[(int)Piece.WKing] = SquareMask.E1;
			b.pieces[(int)Piece.BKing] = SquareMask.E8;

			b.wKingSideAllowed = true;
			b.wQueenSideAllowed = true;
			b.bKingSideAllowed = true;
			b.bQueenSideAllowed = true;

			b.sideToMove = Color.White;

			b.pieces[(int)Color.White] = 0;
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WPawn];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WRook];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WKnight];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WBishop];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WQueen];
			b.pieces[(int)Color.White] |= b.pieces[(int)Piece.WKing];

			b.pieces[(int)Color.Black] = 0;
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BPawn];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BRook];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BKnight];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BBishop];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BQueen];
			b.pieces[(int)Color.Black] |= b.pieces[(int)Piece.BKing];

			b.occupied = b.pieces[(int)Color.White] | b.pieces[(int)Color.Black];
			b.empty = ~b.occupied;

			b.wAttacks = 0;
			b.bAttacks = 0;

			b.halfMovesSinceCapOrPawn = 0;
			b.fullMoveNum = 1;
			b.enPassantTargetSquare = SquareIndex.None;
			b.RebuildMailbox();

			(b.wAttacks, b.wStaticAttacks, b.wSliderAttacks) = MoveGen.GenerateWhiteAttackMaps(b);
			(b.bAttacks, b.bStaticAttacks, b.bSliderAttacks) = MoveGen.GenerateBlackAttackMaps(b);

			return b;
		}
		private void RebuildMailbox()
		{
			for (int i = 0; i < 64; i++)
			{
				ulong mask = 1UL << i;

				if (mask.Any(occupied))
				{
					for (int j = 2; j < 14; j++)
					{
						if (mask.Any(pieces[j]))
						{
							mailbox[i] = (Piece)j;
							break;
						}
					}
				}
				else
				{
					mailbox[i] = Piece.Empty;
				}
			}
		}
		public bool CheckState()
		{
			if ((pieces[(int)Piece.WhiteAll] & pieces[(int)Piece.BlackAll]) != 0) return false;
			if ((pieces[(int)Piece.WhiteAll] | pieces[(int)Piece.BlackAll]) != occupied) return false;
			if ((pieces[(int)Piece.WhiteAll] & empty) != 0) return false;
			if ((pieces[(int)Piece.BlackAll] & empty) != 0) return false;
			if (~occupied != empty) return false;

			for (int i = 2; i < 14; i++)
			{
				for (int j = i + 1; j < 14; j++)
				{
					if ((pieces[i] & pieces[j]) != 0) return false;
				}

				if ((pieces[i] & empty) != 0) return false; // None of the pieces in empty
				if ((pieces[i] & occupied) != pieces[i]) return false;  // All of the pieces in occupied
				if ((pieces[i] & pieces[i % 2]) != pieces[i]) return false; // All of the pieces are in their own color collection
			}

			int countAllPieces = 0;
			int countWhitePieces = 0;
			int countBlackPieces = 0;

			int expectedCountAllPieces = 0;
			int expectedCountWhitePieces = 0;
			int expectedCountBlackPieces = 0;

			for (int i = 0; i < 64; i++)
			{
				ulong mask = 1UL << i;
				var expectedPiece = mailbox[i];
				if (expectedPiece == Piece.Empty)
				{
					if (mask.Any(occupied))
					{
						return false;
					}
					if (!mask.Any(empty))
					{
						return false;
					}
				}
				else
				{
					if (expectedPiece == Piece.WhiteAll || expectedPiece == Piece.BlackAll)
					{
						return false;
					}
					if (!mask.Any(pieces[(int)expectedPiece]))
					{
						return false;
					}
				}

				if (mask.Any(pieces[(int)Piece.WhiteAll]))
				{
					expectedCountWhitePieces++;
				}
				if (mask.Any(pieces[(int)Piece.BlackAll]))
				{
					expectedCountBlackPieces++;
				}
				if (mask.Any(occupied))
				{
					expectedCountAllPieces++;
				}

				for (int j = 2; j < 14; j++)
				{
					if (mask.Any(pieces[j]))
					{
						if (j % 2 == 0)
						{
							countWhitePieces++;
						}
						else
						{
							countBlackPieces++;
						}
						countAllPieces++;
					}
				}
			}

			if (expectedCountAllPieces != countAllPieces) return false;
			if (expectedCountWhitePieces != countWhitePieces) return false;
			if (expectedCountBlackPieces != countBlackPieces) return false;
			if (expectedCountWhitePieces + expectedCountBlackPieces != expectedCountAllPieces) return false;

			return true;
		}

		public BitBoard Copy()
		{
			BitBoard board = new BitBoard()
			{
				wAttacks = wAttacks,
				bAttacks = bAttacks,
				bKingSideAllowed = bKingSideAllowed,
				bQueenSideAllowed = bQueenSideAllowed,
				wKingSideAllowed = wKingSideAllowed,
				wQueenSideAllowed = wQueenSideAllowed,
				empty = empty,
				occupied = occupied,
				enPassantTargetSquare = enPassantTargetSquare,
				fullMoveNum = fullMoveNum,
				halfMovesSinceCapOrPawn = halfMovesSinceCapOrPawn,
				sideToMove = sideToMove,
				mailbox = new Piece[64],
				pieces = new ulong[14]
			};

			mailbox.CopyTo(new Span<Piece>(board.mailbox));
			pieces.CopyTo(new Span<ulong>(board.pieces));
			return board;
		}

		public ulong Perft(int depth)
		{
			ulong nodes = 0;

			if (depth == 0)
				return 1UL;

			var moves = MoveGen.GeneratePseudoLegalMoves(this);

			for (int i = 0; i < moves.Count; i++)
			{
				var unMove = MakeMove(moves[i]);
				if (unMove.wasLegal)
					nodes += Perft(depth - 1);
				UnMakeMove(unMove);
			}
			return nodes;
		}

		public IEnumerable<(Move, ulong)> PerftList(int depth)
		{
			var moves = MoveGen.GeneratePseudoLegalMoves(this);

			for (int i = 0; i < moves.Count; i++)
			{
				var unMove = MakeMove(moves[i]);
				if (unMove.wasLegal)
				{
					ulong nodes = Perft(depth - 1);
					yield return (moves[i], nodes);
				}
				UnMakeMove(unMove);
			}
		}
	}


}
