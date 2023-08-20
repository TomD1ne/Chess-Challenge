using ChessChallenge.API;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

public class TomBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool iAmWhite = true;
    Random rng = new();
    bool debug = true;

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        List<Move> viableMoves = new();
        if (!board.IsWhiteToMove)
            iAmWhite = false;

        Move bestMove = allMoves[0];
        int bestMoveScore = int.MinValue;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int moveScore = AlphaBeta(board);
            // Console.WriteLine(move + " " + moveScore);
            if (moveScore > bestMoveScore)
            {
                viableMoves.Clear();
                bestMoveScore = moveScore;
            }
            if (moveScore >= bestMoveScore)
                viableMoves.Add(move);
            board.UndoMove(move);
        }

        bestMove = viableMoves[rng.Next(viableMoves.Count)];
        // Console.WriteLine("best " + bestMove + " totalEval " + EvaluateBoard(board));
        // (int bestMoveScore, Move bestMove) = BestMove(board, 2);

        return bestMove;
    }
    int AlphaBeta(Board board, int depth = 2, int alpha = int.MinValue, int beta = int.MaxValue, bool isSelf = false)
    {
        Move[] allMoves = board.GetLegalMoves();

        if (depth == 0) return EvaluateBoard(board, allMoves);

        int value = isSelf ? int.MinValue : int.MaxValue;
        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
                return isSelf ? int.MaxValue : int.MinValue; ;
            board.MakeMove(move);
            // if (depth == 1) Console.WriteLine(move);
            value = isSelf
            ? Math.Max(value, AlphaBeta(board, depth - 1, alpha, beta, false))
            : Math.Min(value, AlphaBeta(board, depth - 1, alpha, beta, true));
            board.UndoMove(move);

            if (isSelf)
            {
                if (value >= beta) break;
                alpha = Math.Max(alpha, value);
            }
            else
            {
                if (value <= alpha) break;
                beta = Math.Min(beta, value);
            }
        }
        return value;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
    int EvaluateBoard(Board board, Move[] allMoves, bool debug = false)
    {
        bool myTurn = (board.IsWhiteToMove && iAmWhite) || (!board.IsWhiteToMove && !iAmWhite);
        // if (debug) Console.WriteLine("PieceScore: " + PieceScores() + " MoveScore: " + MoveScore());
        return PieceScores() + MoveScore();

        int MoveScore()
        {
            int amountOfMovesCurrent = allMoves.Length / 10;
            bool turnSkipped = board.TrySkipTurn();
            int amountOfMovesNext = turnSkipped ? amountOfMovesCurrent : 20;
            if (turnSkipped) board.UndoSkipTurn();
            return myTurn ? amountOfMovesCurrent - amountOfMovesNext : amountOfMovesNext - amountOfMovesCurrent;
        }

        int PieceScores()
        {
            (int whiteTotalPieceScores, int blackTotalPieceScores) = (0, 0);
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
            return iAmWhite ? whiteTotalPieceScores - blackTotalPieceScores : blackTotalPieceScores - whiteTotalPieceScores;
        }
    }
}