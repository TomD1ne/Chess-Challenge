using ChessChallenge.API;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

public class TomBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool iAmWhite = true;
    // Move bestMove = Move.NullMove;
    Random rng = new();
    bool debug = true;

    public Move Think(Board board, Timer timer)
    {
        if (!board.IsWhiteToMove)
            iAmWhite = false;

        (int moveScore, Move bestMove) = AlphaBeta(board, isSelf: true);
        // Console.WriteLine("best " + bestMove + " totalEval " + moveScore);
        // Console.WriteLine(" ");

        return bestMove;
    }
    (int, Move) AlphaBeta(Board board, int depth = 4, int alpha = int.MinValue, int beta = int.MaxValue, bool isSelf = true)
    {

        Move[] allMoves = OrderMoves(board);
        if (board.IsInCheckmate()) return isSelf ? (int.MinValue, Move.NullMove) : (int.MaxValue, Move.NullMove);
        if (depth == 0 || allMoves.Length == 0) return (EvaluateBoard(board, allMoves), Move.NullMove);

        List<Move> viableMoves = new();

        int value = isSelf ? int.MinValue : int.MaxValue;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            if (isSelf)
            {
                int moveValue = AlphaBeta(board, depth - 1, alpha, beta, false).Item1;
                board.UndoMove(move);
                if (moveValue > value)
                {
                    value = moveValue;
                    viableMoves.Clear();
                }

                if (moveValue == value) viableMoves.Add(move);

                if (value > beta) break;

                alpha = Math.Max(alpha, value);
            }
            else
            {
                int moveValue = AlphaBeta(board, depth - 1, alpha, beta, true).Item1;
                board.UndoMove(move);
                if (moveValue < value)
                {
                    value = moveValue;
                    viableMoves.Clear();
                }
                if (moveValue == value) viableMoves.Add(move);
                if (value < alpha) break;
                beta = Math.Min(beta, value);
            }
        }
        return (value, viableMoves[rng.Next(viableMoves.Count)]);
    }

    // Test if this move gives checkmate
    // bool MoveIsCheckmate(Board board, Move move)
    // {
    //     board.MakeMove(move);
    //     bool isMate = board.IsInCheckmate();
    //     board.UndoMove(move);
    //     return isMate;
    // }
    Move[] OrderMoves(Board board)
    {
        Move[] allMoves = board.GetLegalMoves();
        List<Move> moves = new(allMoves.Length);
        List<Move> nonCaptures = new(allMoves.Length);
        foreach (Move move in allMoves)
        {
            if (move.IsCapture)
                moves.Add(move);

            else
                nonCaptures.Add(move);
        }
        moves.AddRange(nonCaptures);
        return moves.ToArray();
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
            int amountOfMovesNext = turnSkipped ? board.GetLegalMoves().Length / 10 : 8;
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