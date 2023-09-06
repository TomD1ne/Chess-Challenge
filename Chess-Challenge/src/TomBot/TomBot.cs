using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class TomBot : IChessBot
{
    const sbyte EXACT = 0, LOWERBOUND = -1, UPPERBOUND = 1, INVALID = -2;
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool iAmWhite = true;
    Transposition[] TPTable = new Transposition[0x7FFFFF + 1];
    // Move bestMove = Move.NullMove;
    Random rng = new();
    // bool debug = true;

    public Move Think(Board board, Timer timer)
    {
        sbyte depth = 5;
        if (!board.IsWhiteToMove)
            iAmWhite = false;

        if (board.PlyCount < 6)
            depth = 3;

        (int moveScore, Move bestMove) = AlphaBeta(board, isSelf: true, depth: depth);
        // Console.WriteLine("best " + bestMove + " totalEval " + moveScore);
        Console.WriteLine(timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining);
        return bestMove;
    }
    (int, Move) AlphaBeta(Board board, int depth = 5, int alpha = int.MinValue, int beta = int.MaxValue, bool isSelf = true)
    {
        ref Transposition transposition = ref TPTable[board.ZobristKey & 0x7FFFFF];
        Move[] allMoves = OrderMoves(board, transposition);

        if (board.IsInCheckmate()) return isSelf ? (int.MinValue, Move.NullMove) : (int.MaxValue, Move.NullMove);

        if (transposition.zobristHash == board.ZobristKey && transposition.depth >= depth)
        {
            if (transposition.flag == EXACT) return (transposition.evaluation, transposition.move);
            if (transposition.flag == LOWERBOUND && transposition.evaluation >= beta) return (transposition.evaluation, transposition.move);
            if (transposition.flag == UPPERBOUND && transposition.evaluation <= alpha) return (transposition.evaluation, transposition.move);
        }

        if (depth == 0 || allMoves.Length == 0) return (transposition.zobristHash == board.ZobristKey) ? (transposition.evaluation, transposition.move) : (EvaluateBoard(board, allMoves), Move.NullMove);

        List<Move> viableMoves = new();
        int value = isSelf ? int.MinValue : int.MaxValue;
        int startingAlpha = alpha;
        int startingBeta = beta;

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
        Move bestMove = viableMoves[rng.Next(viableMoves.Count)];
        transposition = new Transposition
        {
            zobristHash = board.ZobristKey,
            move = bestMove,
            evaluation = value,
            depth = depth,
        };

        if (value < startingAlpha) transposition.flag = UPPERBOUND; //upper bound
        else if (value >= startingBeta) transposition.flag = LOWERBOUND; //lower bound
        else transposition.flag = EXACT; //"exact" score

        return (value, bestMove);
    }

    Move[] OrderMoves(Board board, Transposition transposition)
    {
        Move[] allMoves = board.GetLegalMoves();
        PriorityQueue<Move, int> orderedMovesQueue = new PriorityQueue<Move, int>(allMoves.Length);
        Move[] orderedMoves = new Move[allMoves.Length];

        // ref Transposition transposition = ref TPTable[board.ZobristKey & 0x7FFFFF];

        foreach (Move move in allMoves)
        {
            if (move == transposition.move)
            {
                orderedMovesQueue.Enqueue(move, int.MinValue);
                continue;
            }

            // Lower (negative) is higher priority
            int orderScore = 0;
            int capturedPieceValue = pieceValues[(int)move.CapturePieceType];
            int movePieceValue = pieceValues[(int)move.MovePieceType];

            if (move.IsCapture)
                orderScore -= capturedPieceValue - movePieceValue / 10;

            else orderScore += 100 / movePieceValue;

            if (move.IsPromotion)
                orderScore -= 500;

            if (board.IsInCheck())
                orderScore -= 500;

            orderedMovesQueue.Enqueue(move, orderScore);
        }

        for (int i = 0; i < allMoves.Length; i++)
            orderedMoves[i] = orderedMovesQueue.Dequeue();

        return orderedMoves;
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
    public struct Transposition
    {
        public ulong zobristHash;
        public Move move;
        public int evaluation;
        public int depth;
        public sbyte flag;
    };
}

