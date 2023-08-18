using System;
using System.IO.Pipes;
using ChessChallenge.API;

public class RuudBot : IChessBot
{
    readonly Random rng = new();

    readonly int[] weights = { 0, 1, 3, 3, 5, 9, 30 };
    readonly int captureFactor = 1000;
    readonly int attackFactor = 30;
    readonly int freeMovesFactor = 1;
    readonly bool debug = false;

    public Move Think(Board board, Timer timer)
    {
        return ThinkAhead(0).Item1;

        (Move, int) ThinkAhead(int depth)
        {
            if (debug && depth == 0) Console.WriteLine("Thinking for " + (board.IsWhiteToMove ? "White" : "Black"));
            Move[] moves = board.GetLegalMoves();
            Move moveToPlay = debug ? (moves.Length > 0 ? moves[0] : Move.NullMove) : RandomMove();
            int highScore = -1000000;
            foreach (Move move in moves)
            {
                // Always play checkmate in one
                if (WithMove(move, () => board.IsInCheckmate()))
                    return (move, captureFactor * weights[(int)PieceType.King]);

                // Find highest value capture
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int captureScore = captureFactor * weights[(int)capturedPiece.PieceType];
                int score = captureScore;

                // Attack score
                int attackScore = -AttackScore() + WithMove(move, () => AttackScore());
                score += attackScore;

                // Free move score
                int freeMoveScore = freeMovesFactor * (-moves.Length + WithMove(move, () => board.GetLegalMoves().Length));
                score += freeMoveScore;

                // Recursion
                if (depth < 1)
                {
                    var (nextMove, nextScore) = WithMove(move, () => ThinkAhead(depth + 1));
                    score -= nextScore;
                    if (debug) Console.WriteLine("" + move + "  score: " + score + "  capture: " + captureScore + "  attack: " + attackScore + "  freeMove: " + freeMoveScore + "  nextScore: " + nextScore + "  next" + nextMove);
                }

                if (score > highScore)
                {
                    highScore = score;
                    moveToPlay = move;
                }
            }
            return (moveToPlay, highScore);
        }

        T WithMove<T>(Move move, Func<T> method)
        {
            board.MakeMove(move);
            var result = method();
            board.UndoMove(move);
            return result;
        }

        Move RandomMove()
        {
            Move[] moves = board.GetLegalMoves();
            return moves[rng.Next(moves.Length)];
        }

        int AttackScore()
        {
            int score = 0;
            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                int attackValue = attackFactor * weights[(int)pieceList.TypeOfPieceInList];
                foreach (Piece piece in pieceList)
                {
                    bool myPiece = board.IsWhiteToMove == piece.IsWhite;
                    if (board.SquareIsAttackedByOpponent(piece.Square))
                    {
                        score += myPiece ? -attackValue : attackValue;
                    }
                }
            }
            return score;
        }
    }
}