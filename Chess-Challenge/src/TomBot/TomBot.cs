using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class TomBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        List<Move> viableMoves = new();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestMoveValue = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }
            // Find highest value capture

            if (!canBeRecaptured(board, move))
            {
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
                if (capturedPieceValue > highestMoveValue)
                {
                    highestMoveValue = capturedPieceValue;
                    moveToPlay = move;
                }
                if (highestMoveValue == 0 || highestMoveValue == 10)
                {
                    highestMoveValue = 10;
                    viableMoves.Add(move);
                    // moveToPlay = move;
                }
            }
        }
        if (highestMoveValue == 10)
            moveToPlay = viableMoves[rng.Next(viableMoves.Count)];
        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool canBeRecaptured(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves(true);
        board.UndoMove(move);
        foreach (Move capture in allMoves)
        {
            if (capture.TargetSquare == move.TargetSquare) return true;
        }
        return false;
    }
}