using System;

namespace checkers.Models;

public class Bot
{
    private bool _isWhite;
    private TimeSpan _moveTime;
    private static int _maxDepth = 5;
    
    public Bot(bool isWhite, TimeSpan moveTime)
    {
        _isWhite = isWhite;
        _moveTime = moveTime;
    }

    public SmallMove? GetBestMove(SmallBoard board)
    {
        var moves = board.GetMoves();
        if (moves.Count == 0)
            return null;

        SmallMove bestMove = null;
        int bestScore = int.MinValue;
        foreach (var move in moves)
        {
            var newBoard = board.Copy();
            newBoard.ApplyMove(move);
            
            var score = AlphaBeta(newBoard, _maxDepth, int.MinValue, int.MaxValue, false);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        
        return bestMove;
    }

    private int AlphaBeta(SmallBoard board, int depth, int alpha, int beta, bool isMaximizing)
    {
        if (depth == 0 || board.IfOver())
            return board.Evaluate();

        int bestScore;
        if (isMaximizing)
        {
            bestScore = int.MinValue;
            foreach (var move in board.GetMoves())
            {
                var child = board.Copy();
                child.ApplyMove(move);
                int score = AlphaBeta(child, depth - 1, alpha, beta, false);
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, score);
                if (beta <= alpha)
                    break;
            }
        }
        else
        {
            bestScore = int.MaxValue;
            foreach (var move in board.GetMoves())
            {
                var child = board.Copy();
                child.ApplyMove(move);
                int score = AlphaBeta(child, depth - 1, alpha, beta, true);
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, score);
                if (beta <= alpha)
                    break;
            }
        }

        return bestScore;
    }
}