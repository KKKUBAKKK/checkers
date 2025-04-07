using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace checkers.Models;

/// <summary>
/// Represents an AI opponent using alpha-beta pruning algorithm for move selection
/// </summary>
public class Bot
{
    /// <summary>
    /// Indicates whether the bot plays as white pieces
    /// </summary>
    private readonly bool _isWhite;

    /// <summary>
    /// Maximum time allowed for move calculation
    /// </summary>
    private readonly TimeSpan _moveTime;

    /// <summary>
    ///
    /// </summary>
    private const int InitialDepth = 1;

    /// <summary>
    /// Maximum search depth for the alpha-beta algorithm
    /// </summary>
    private const int MaxSearchDepth = 20;

    /// <summary>
    /// Indicates whether the bot plays as white pieces
    /// </summary>
    public bool IsWhite => _isWhite;

    /// <summary>
    /// Maximum time allowed for move calculation
    /// </summary>
    public TimeSpan ThinkingTime => _moveTime;

    /// <summary>
    /// Initializes a new bot instance with specified color and thinking time
    /// </summary>
    public Bot(bool isWhite = false, TimeSpan moveTime = default)
    {
        _isWhite = isWhite;
        _moveTime = moveTime == default ? TimeSpan.FromSeconds(5) : moveTime;
    }

    /// <summary>
    /// Calculates and returns the best possible move using iterative deepening search
    /// </summary>
    public Move? GetBestMove(Board board)
    {
        var moves = board.GetMoves();
        if (moves.Count == 0)
            return null;

        if (moves.Count == 1)
            return moves[0];

        var stopwatch = Stopwatch.StartNew();
        var bestMoveResult = new ConcurrentDictionary<int, Move>();
        var tokenSource = new CancellationTokenSource();

        try
        {
            tokenSource.CancelAfter(_moveTime);
            var token = tokenSource.Token;

            for (int depth = InitialDepth; depth <= MaxSearchDepth; depth++)
            {
                int searchDepth = depth;
                Move currentBestMove = null;
                int currentBestScore = int.MinValue;

                try
                {
                    Parallel.ForEach(
                        moves,
                        new ParallelOptions { CancellationToken = token },
                        () => (int.MinValue, null as Move),
                        (move, state, localState) =>
                        {
                            token.ThrowIfCancellationRequested();

                            var (localBestScore, localBestMove) = localState;
                            var newBoard = board.Copy();
                            newBoard.ApplyMove(move);

                            var score = AlphaBeta(
                                newBoard,
                                searchDepth - 1,
                                int.MinValue,
                                int.MaxValue,
                                !board.IsWhiteTurn,
                                token
                            );

                            if (score > localBestScore)
                            {
                                localBestScore = score;
                                localBestMove = move;
                            }

                            return (localBestScore, localBestMove);
                        },
                        localState =>
                        {
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
                        }
                    );

                    if (currentBestMove != null)
                    {
                        bestMoveResult[depth] = currentBestMove;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            tokenSource.Dispose();
        }

        int deepestCompleted = 0;
        foreach (var depth in bestMoveResult.Keys)
        {
            deepestCompleted = Math.Max(deepestCompleted, depth);
        }

        return deepestCompleted > 0 ? bestMoveResult[deepestCompleted] : moves[0];
    }

    /// <summary>
    /// Implements the alpha-beta pruning algorithm for move evaluation
    /// </summary>
    private int AlphaBeta(
        Board board,
        int depth,
        int alpha,
        int beta,
        bool isMaximizing,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        if (depth == 0 || board.IfOver())
        {
            int score = board.Evaluate();
            return _isWhite ? score : -score;
        }

        var moves = board.GetMoves();
        if (moves.Count == 0)
        {
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
