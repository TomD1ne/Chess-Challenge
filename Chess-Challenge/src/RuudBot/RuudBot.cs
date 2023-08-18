using System;
using ChessChallenge.API;

public class RuudBot : IChessBot
{
    Random rng = new();

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        return (Move)BestMove(board, 1).Item1;
    }

    public (Move?, int) BestMove(Board board, int depth)
    {
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0) return (null, 0);
        Move moveToPlay = moves[rng.Next(moves.Length)];
        int highestGain = 0;
        foreach (Move move in moves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            int gain = capturedPieceValue;

            if (depth > 0)
            {
                board.MakeMove(move);
                (Move?, int) nextMove = BestMove(board, depth - 1);
                board.UndoMove(move);
                gain -= nextMove.Item2;
            }

            if (gain > highestGain)
            {
                highestGain = gain;
                moveToPlay = move;
            }

        }
        return (moveToPlay, highestGain);
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}