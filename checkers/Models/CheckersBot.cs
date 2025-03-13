using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace checkers.Models
{
    public class CheckersBot
    {
        private readonly Random _random = new Random();
        private readonly int _difficulty; // 1-3, where 3 is hardest
        
        public CheckersBot(int difficulty = 2)
        {
            _difficulty = Math.Clamp(difficulty, 1, 3);
        }
        
        public async Task<Move> GetBestMoveAsync(CheckersBoard board)
        {
            return await Task.Run(() => GetBestMove(board));
        }
        
        private Move GetBestMove(CheckersBoard board)
        {
            var validMoves = board.GetAllValidMoves();
            
            if (!validMoves.Any())
                return null;
                
            // Easy difficulty: pick a random move
            if (_difficulty == 1)
                return validMoves[_random.Next(validMoves.Count)];
                
            // Medium/Hard difficulties: Use minimax algorithm
            int depth = _difficulty == 2 ? 3 : 5;
            
            Move bestMove = null;
            int bestScore = int.MinValue;
            
            foreach (var move in validMoves)
            {
                var boardCopy = board.Clone();
                boardCopy.TryMove(move.From, move.To);
                
                int score = Minimax(boardCopy, depth - 1, false, int.MinValue, int.MaxValue);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            
            return bestMove;
        }
        
        private int Minimax(CheckersBoard board, int depth, bool isMaximizing, int alpha, int beta)
        {
            if (depth == 0 || board.IsGameOver)
                return EvaluateBoard(board);
                
            var validMoves = board.GetAllValidMoves();
            
            if (isMaximizing)
            {
                int maxScore = int.MinValue;
                
                foreach (var move in validMoves)
                {
                    var boardCopy = board.Clone();
                    boardCopy.TryMove(move.From, move.To);
                    
                    int score = Minimax(boardCopy, depth - 1, false, alpha, beta);
                    maxScore = Math.Max(maxScore, score);
                    
                    alpha = Math.Max(alpha, score);
                    if (beta <= alpha)
                        break;
                }
                
                return maxScore;
            }
            else
            {
                int minScore = int.MaxValue;
                
                foreach (var move in validMoves)
                {
                    var boardCopy = board.Clone();
                    boardCopy.TryMove(move.From, move.To);
                    
                    int score = Minimax(boardCopy, depth - 1, true, alpha, beta);
                    minScore = Math.Min(minScore, score);
                    
                    beta = Math.Min(beta, score);
                    if (beta <= alpha)
                        break;
                }
                
                return minScore;
            }
        }
        
        private int EvaluateBoard(CheckersBoard board)
        {
            int score = 0;
            
            for (int row = 0; row < CheckersBoard.BoardSize; row++)
            {
                for (int col = 0; col < CheckersBoard.BoardSize; col++)
                {
                    var piece = board.GetPieceAt(row, col);
                    
                    if (piece.Type == PieceType.Red)
                    {
                        score += piece.Rank == PieceRank.King ? 3 : 1;
                        // Bonus for advancing
                        score += (CheckersBoard.BoardSize - 1 - row) / 2;
                        
                        // Bonus for edge/corner pieces (harder to capture)
                        if (col == 0 || col == CheckersBoard.BoardSize - 1)
                            score += 1;
                    }
                    else if (piece.Type == PieceType.Black)
                    {
                        score -= piece.Rank == PieceRank.King ? 3 : 1;
                        // Bonus for advancing
                        score -= row / 2;
                        
                        // Bonus for edge/corner pieces (harder to capture)
                        if (col == 0 || col == CheckersBoard.BoardSize - 1)
                            score -= 1;
                    }
                }
            }
            
            // Bonus for winning
            if (board.IsGameOver)
            {
                if (board.Winner == PieceType.Red)
                    score += 100;
                else if (board.Winner == PieceType.Black)
                    score -= 100;
            }
            
            // Bonus for having more valid moves (mobility)
            if (board.CurrentTurn == PieceType.Red)
                score += board.GetAllValidMoves().Count / 2;
            else
                score -= board.GetAllValidMoves().Count / 2;
                
            return score;
        }
        
        // Get a move with a delay to simulate "thinking"
        public async Task<Move> GetMoveWithDelayAsync(CheckersBoard board, int delayMs = 500)
        {
            var move = await GetBestMoveAsync(board);
            await Task.Delay(delayMs);
            return move;
        }
        
        // Utility method to determine if bot can make another jump with the same piece
        public bool CanContinueJumping(CheckersBoard board, Position position)
        {
            var jumps = board.GetValidMoves(position).Where(m => m.Jumped != null).ToList();
            return jumps.Any();
        }
    }
}