using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class TomBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool iAmWhite = true;

    public Move Think(Board board, Timer timer)
    {
        if (!board.IsWhiteToMove)
            iAmWhite = false;
        (int bestMoveScore, Move bestMove) = BestMove(board, 1);
        Console.WriteLine(bestMoveScore + " " + bestMove);
        return bestMove;
    }

    (int, Move) BestMove(Board board, int depth)
    {
        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length < 1) { return (0, Move.NullMove); }
        int currentEval = EvaluateBoard(board);
        Move bestMove = Move.NullMove;
        int bestMoveScore = -1000000;

        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                bestMove = move;
                bestMoveScore = 100000;
                break;
            }
            board.MakeMove(move);
            int newEval = currentEval - EvaluateBoard(board);
            int bestCounterEval = 0;
            if (depth > 0)
            {
                (bestCounterEval, Move counterMove) = BestMove(board, depth - 1);
                Console.WriteLine("bot " + move + " eval: " + newEval + " my " + bestMove + " eval: " + bestCounterEval);
            }
            board.UndoMove(move);
            newEval -= bestCounterEval;
            if (newEval >= bestMoveScore || bestMove == Move.NullMove)
            {
                bestMoveScore = newEval;
                bestMove = move;
            }
        }
        return (bestMoveScore, bestMove);
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
    bool CanBeRecaptured(Board board, Move move)
    {
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            return true;
        return false;
    }
    int EvaluateBoard(Board board)
    {
        return PieceScores();
        int PieceScores()
        {
            int whiteTotalPieceScores = 0;
            int blackTotalPieceScores = 0;
            PieceList[] allPieces = board.GetAllPieceLists();
            foreach (PieceList pieceList in allPieces)
            {
                foreach (Piece piece in pieceList)
                {
                    if (piece.IsWhite)
                        whiteTotalPieceScores += pieceValues[(int)piece.PieceType];
                    else
                        blackTotalPieceScores += pieceValues[(int)piece.PieceType];
                }
            }
            if (iAmWhite)
            {
                return whiteTotalPieceScores - blackTotalPieceScores;
            }
            return blackTotalPieceScores - whiteTotalPieceScores;
        }
    }
}