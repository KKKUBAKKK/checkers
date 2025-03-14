using System.Collections.Generic;
using System.Linq;

namespace checkers.Models
{
    public enum PieceType { None, White, Black }
    public enum PieceRank { Regular, King }

    public class CheckersPiece
    {
        public PieceType Type { get; set; }
        public PieceRank Rank { get; set; }

        public CheckersPiece(PieceType type = PieceType.None, PieceRank rank = PieceRank.Regular)
        {
            Type = type;
            Rank = rank;
        }

        public CheckersPiece Clone() => new CheckersPiece(Type, Rank);
    }

    public class Position
    {
        public int Row { get; }
        public int Col { get; }

        public Position(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    public class Move
    {
        public Position From { get; }
        public Position To { get; }
        public Position? Jumped { get; set; }

        public Move(Position from, Position to, Position? jumped = null)
        {
            From = from;
            To = to;
            Jumped = jumped;
        }
    }

    public class CheckersBoard
    {
        public const int BoardSize = 8;
        private CheckersPiece[,] _board;
        private PieceType _currentTurn;
        private List<Move> _validMoves;
        private bool _mustJump;

        public PieceType CurrentTurn => _currentTurn;
        public bool IsGameOver { get; private set; }
        public PieceType Winner { get; private set; }

        public CheckersBoard()
        {
            InitializeBoard();
        }

        public CheckersPiece GetPieceAt(int row, int col)
        {
            if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize)
                return new CheckersPiece();

            return _board[row, col].Clone();
        }

        public bool TryMove(Position from, Position to)
        {
            var move = _validMoves.FirstOrDefault(m => 
                m.From.Row == from.Row && m.From.Col == from.Col && 
                m.To.Row == to.Row && m.To.Col == to.Col);

            if (move == null)
                return false;

            ExecuteMove(move);
            return true;
        }

        public List<Move> GetValidMoves(Position position)
        {
            return _validMoves.Where(m => m.From.Row == position.Row && m.From.Col == position.Col).ToList();
        }

        public List<Move> GetAllValidMoves()
        {
            return _validMoves.ToList();
        }

        private void InitializeBoard()
        {
            _board = new CheckersPiece[BoardSize, BoardSize];

            // Initialize empty board
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    _board[row, col] = new CheckersPiece();
                }
            }

            // Place starting pieces
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        _board[row, col] = new CheckersPiece(PieceType.Black);
                    }
                }
            }

            for (int row = 5; row < 8; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        _board[row, col] = new CheckersPiece(PieceType.White);
                    }
                }
            }

            _currentTurn = PieceType.White;
            IsGameOver = false;
            Winner = PieceType.None;

            UpdateValidMoves();
        }

        private void UpdateValidMoves()
        {
            _validMoves = new List<Move>();
            _mustJump = false;

            // First check for jumps (required in most checkers variants)
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (_board[row, col].Type == _currentTurn)
                    {
                        var jumps = FindJumpMoves(row, col);
                        if (jumps.Any())
                        {
                            _validMoves.AddRange(jumps);
                            _mustJump = true;
                        }
                    }
                }
            }

            // If no jumps, add regular moves
            if (!_mustJump)
            {
                for (int row = 0; row < BoardSize; row++)
                {
                    for (int col = 0; col < BoardSize; col++)
                    {
                        if (_board[row, col].Type == _currentTurn)
                        {
                            _validMoves.AddRange(FindRegularMoves(row, col));
                        }
                    }
                }
            }

            // Check if the game is over (no valid moves)
            if (!_validMoves.Any())
            {
                IsGameOver = true;
                Winner = _currentTurn == PieceType.White ? PieceType.Black : PieceType.White;
            }
        }

        private List<Move> FindRegularMoves(int row, int col)
        {
            List<Move> moves = new List<Move>();
            var piece = _board[row, col];

            // Define valid directions based on piece type and rank
            List<(int dr, int dc)> directions = new List<(int, int)>();
            
            if (piece.Type == PieceType.White || piece.Rank == PieceRank.King)
                directions.AddRange(new[] { (-1, -1), (-1, 1) });  // White moves up

            if (piece.Type == PieceType.Black || piece.Rank == PieceRank.King)
                directions.AddRange(new[] { (1, -1), (1, 1) });    // Black moves down

            foreach (var dir in directions)
            {
                int newRow = row + dir.dr;
                int newCol = col + dir.dc;
                
                if (IsValidPosition(newRow, newCol) && _board[newRow, newCol].Type == PieceType.None)
                {
                    moves.Add(new Move(
                        new Position(row, col),
                        new Position(newRow, newCol)
                    ));
                }
            }
            
            return moves;
        }

        private List<Move> FindJumpMoves(int row, int col)
        {
            List<Move> moves = new List<Move>();
            var piece = _board[row, col];

            // Define valid directions based on piece type and rank
            List<(int dr, int dc)> directions = new List<(int, int)>();
            
            if (piece.Type == PieceType.White || piece.Rank == PieceRank.King)
                directions.AddRange(new[] { (-1, -1), (-1, 1) });  // White moves up

            if (piece.Type == PieceType.Black || piece.Rank == PieceRank.King)
                directions.AddRange(new[] { (1, -1), (1, 1) });    // Black moves down

            foreach (var dir in directions)
            {
                int jumpRow = row + dir.dr;
                int jumpCol = col + dir.dc;
                int landRow = row + 2 * dir.dr;
                int landCol = col + 2 * dir.dc;
                
                if (IsValidPosition(jumpRow, jumpCol) && IsValidPosition(landRow, landCol))
                {
                    var jumpPiece = _board[jumpRow, jumpCol];
                    if (jumpPiece.Type != PieceType.None && jumpPiece.Type != piece.Type &&
                        _board[landRow, landCol].Type == PieceType.None)
                    {
                        moves.Add(new Move(
                            new Position(row, col),
                            new Position(landRow, landCol),
                            new Position(jumpRow, jumpCol)
                        ));
                    }
                }
            }
            
            return moves;
        }

        private bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
        }

        private void ExecuteMove(Move move)
        {
            // Move piece
            _board[move.To.Row, move.To.Col] = _board[move.From.Row, move.From.Col];
            _board[move.From.Row, move.From.Col] = new CheckersPiece();

            // Handle jumps
            if (move.Jumped != null)
            {
                _board[move.Jumped.Row, move.Jumped.Col] = new CheckersPiece();
                
                // Check if multiple jumps are possible
                var additionalJumps = FindJumpMoves(move.To.Row, move.To.Col);
                if (additionalJumps.Any())
                {
                    _validMoves = additionalJumps;
                    return; // Don't change turn - multiple jumps
                }
            }

            // Check for promotion to king
            var piece = _board[move.To.Row, move.To.Col];
            if (piece.Rank == PieceRank.Regular)
            {
                if ((piece.Type == PieceType.White && move.To.Row == 0) ||
                    (piece.Type == PieceType.Black && move.To.Row == BoardSize - 1))
                {
                    piece.Rank = PieceRank.King;
                }
            }

            // Switch turns
            _currentTurn = _currentTurn == PieceType.White ? PieceType.Black : PieceType.White;
            
            // Update valid moves for next player
            UpdateValidMoves();
        }

        public CheckersBoard Clone()
        {
            var clone = new CheckersBoard();
            clone._board = new CheckersPiece[BoardSize, BoardSize];
            
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    clone._board[row, col] = _board[row, col].Clone();
                }
            }
            
            clone._currentTurn = _currentTurn;
            clone.IsGameOver = IsGameOver;
            clone.Winner = Winner;
            clone._validMoves = _validMoves.ToList();
            clone._mustJump = _mustJump;
            
            return clone;
        }
    }
}