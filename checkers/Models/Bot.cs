using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace checkers.Models;

public class Bot
{
    private readonly bool _isWhite;
    private readonly TimeSpan _moveTime;
    private const int InitialDepth = 1;
    private const int MaxSearchDepth = 20;
    
    public bool IsWhite => _isWhite;
    public TimeSpan ThinkingTime => _moveTime;

    public Bot(bool isWhite = false, TimeSpan moveTime = default)
    {
        _isWhite = isWhite;
        _moveTime = moveTime == default ? TimeSpan.FromSeconds(5) : moveTime;
    }

    public Move? GetBestMove(Board board)
    {
        var moves = board.GetMoves();
        if (moves.Count == 0)
            return null;
            
        // Quick return for single moves
        if (moves.Count == 1)
            return moves[0];

        // Use iterative deepening with time limit
        var stopwatch = Stopwatch.StartNew();
        var bestMoveResult = new ConcurrentDictionary<int, Move>();
        var bestScoreResult = new ConcurrentDictionary<int, int>();
        var tokenSource = new CancellationTokenSource();

        try
        {
            // Set timer to cancel computation when move time is up
            tokenSource.CancelAfter(_moveTime);
            var token = tokenSource.Token;

            // Iterative deepening - keep searching deeper until time expires
            for (int depth = InitialDepth; depth <= MaxSearchDepth; depth++)
            {
                int searchDepth = depth; // Local copy for lambda expression
                Move currentBestMove = null;
                int currentBestScore = int.MinValue;

                try
                {
                    // Search in parallel
                    Parallel.ForEach(moves,
                        new ParallelOptions { CancellationToken = token },
                        () => (int.MinValue, null as Move), // Thread local state
                        (move, state, localState) => {
                            // Check for cancellation
                            token.ThrowIfCancellationRequested();

                            var (localBestScore, localBestMove) = localState;
                            var newBoard = board.Copy();
                            newBoard.ApplyMove(move);

                            // Bot is always the minimizing player if it's black (not white)
                            var score = AlphaBeta(newBoard, searchDepth - 1, int.MinValue, int.MaxValue, 
                                                 !board.IsWhiteTurn, token);

                            if (score > localBestScore)
                            {
                                localBestScore = score;
                                localBestMove = move;
                            }

                            return (localBestScore, localBestMove);
                        },
                        localState => {
                            // Combine results from all threads
                            var (score, move) = localState;
                            if (move != null && score > currentBestScore)
                            {
                                lock (bestMoveResult)
                                {
                                    if (score > currentBestScore)
                                    {
                                        currentBestScore = score;
                                        currentBestMove = move;
                                    }
                                }
                            }
                        });

                    // Save best move and score for this depth
                    if (currentBestMove != null)
                    {
                        bestMoveResult[depth] = currentBestMove;
                        bestScoreResult[depth] = currentBestScore;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Time's up, exit the loop
                    break;
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            tokenSource.Dispose();
        }

        // Return the best move from the deepest completed search
        int deepestCompleted = 0;
        foreach (var depth in bestMoveResult.Keys)
        {
            deepestCompleted = Math.Max(deepestCompleted, depth);
        }

        return deepestCompleted > 0 ? bestMoveResult[deepestCompleted] : moves[0];
    }

    private int AlphaBeta(Board board, int depth, int alpha, int beta, bool isMaximizing, CancellationToken token)
    {
        // Check if we should stop searching due to time limit
        token.ThrowIfCancellationRequested();

        if (depth == 0 || board.IfOver())
        {
            // Evaluate from the bot's perspective
            int score = board.Evaluate();
            return _isWhite ? score : -score; // Invert score if bot is black
        }

        var moves = board.GetMoves();
        if (moves.Count == 0)
        {
            // Game is over
            return isMaximizing ? int.MinValue : int.MaxValue;
        }

        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            foreach (var move in moves)
            {
                var child = board.Copy();
                child.ApplyMove(move);
                int score = AlphaBeta(child, depth - 1, alpha, beta, false, token);
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, score);
                if (beta <= alpha)
                    break;
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            foreach (var move in moves)
            {
                var child = board.Copy();
                child.ApplyMove(move);
                int score = AlphaBeta(child, depth - 1, alpha, beta, true, token);
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, score);
                if (beta <= alpha)
                    break;
            }
            return bestScore;
        }
    }
}