﻿using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Piece values: pawn, knight, bishop, rook, queen, king
    private readonly int[] pieceValues = { 100, 300, 325, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        return moves[0];
    }

    private int Evaluate(Board board)
    {
        int score = 0;

        // Material Advantage
        PieceList[] pieceLists = board.GetAllPieceLists();
        for (int i = 0; i < 12; i++)
        {
            int pieceValue = pieceValues[(int)pieceLists[i].TypeOfPieceInList - 1];
            if (pieceLists[i].IsWhitePieceList)
            {
                score -= pieceValue * pieceLists[i].Count;
            }
            else
            {
                score += pieceValue * pieceLists[i].Count;
            }
        }

        // King Safety
        if (board.IsInCheck())
        {
            if (board.IsWhiteToMove)
            {
                score += 50; // Black has an advantage
            }
            else
            {
                score -= 50; // White has an advantage
            }
        }

        // Checkmate
        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
            {
                score += 10000; // Black wins
            }
            else
            {
                score -= 10000; // White wins
            }
        }

        // Pawn Structure - Penalty for Doubled and Isolated Pawns
        ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
        ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);
        for (int file = 0; file < 8; file++)
        {
            ulong fileMask = 0x0101010101010101UL << file;
            int whitePawnsInFile = BitboardHelper.GetNumberOfSetBits(whitePawns & fileMask);
            int blackPawnsInFile = BitboardHelper.GetNumberOfSetBits(blackPawns & fileMask);
                
            // Penalize Doubled pawns
            if (whitePawnsInFile > 1) score += 10 * (whitePawnsInFile - 1);
            if (blackPawnsInFile > 1) score -= 10 * (blackPawnsInFile - 1);
                
            // Penalize Isolated pawns
            ulong adjFilesMask = fileMask;
            if (file > 0) adjFilesMask |= fileMask >> 1;
            if (file < 7) adjFilesMask |= fileMask << 1;
            if ((whitePawns & adjFilesMask) == 0) score += 20;
            if ((blackPawns & adjFilesMask) == 0) score -= 20;
        }

        // Pawn Structure - Reward Passed pawns
        ulong passedWhitePawns = whitePawns & ~((blackPawns >> 8) | (blackPawns >> 7) | (blackPawns >> 9));
        ulong passedBlackPawns = blackPawns & ~((whitePawns << 8) | (whitePawns << 7) | (whitePawns << 9));
        score += 30 * BitboardHelper.GetNumberOfSetBits(passedWhitePawns);
        score -= 30 * BitboardHelper.GetNumberOfSetBits(passedBlackPawns);

        // Pawn Structure - Reward Supported pawns
        ulong supportedWhitePawns = whitePawns & ((whitePawns << 7) | (whitePawns << 9) | (whitePawns << 8));
        ulong supportedBlackPawns = blackPawns & ((blackPawns >> 7) | (blackPawns >> 9) | (blackPawns >> 8));
        score += 15 * BitboardHelper.GetNumberOfSetBits(supportedWhitePawns);
        score -= 15 * BitboardHelper.GetNumberOfSetBits(supportedBlackPawns);


        // Piece Mobility
        Move[] whiteMoves = board.GetLegalMoves(true);
        Move[] blackMoves = board.GetLegalMoves(false);
        score += (blackMoves.Length - whiteMoves.Length) * 10;

        // Reward for captures
        foreach (var move in whiteMoves)
        {
            if (move.IsCapture)
            {
                score -= pieceValues[(int)move.CapturePieceType - 1];
            }
        }
        foreach (var move in blackMoves)
        {
            if (move.IsCapture)
            {
                score += pieceValues[(int)move.CapturePieceType - 1];
            }
        }

        // Center Control
        ulong centerSquares = 0x0000001818000000UL;
        score += BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard & centerSquares) * 20;
        score -= BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard & centerSquares) * 20;

        return score;
    }
}
