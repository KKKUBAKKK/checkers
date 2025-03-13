using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;

namespace checkers.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<SquareViewModel> _boardSquares;
        private bool _isPlayer1Turn = true;
        private bool _rotateBoardAfterMove = false;
        private int _selectedTimeLimitIndex = 0;
        private SquareViewModel _selectedSquare;
        private Timer _moveTimer;
        private int _remainingSeconds;
        private string _timeDisplay = "Time: No Limit";
        private int[] _timeLimitsInSeconds = { 0, 30, 60, 180, 300 }; // No limit, 30s, 1m, 3m, 5m

        public ObservableCollection<SquareViewModel> BoardSquares
        {
            get => _boardSquares;
            set => this.RaiseAndSetIfChanged(ref _boardSquares, value);
        }

        public bool RotateBoardAfterMove
        {
            get => _rotateBoardAfterMove;
            set => this.RaiseAndSetIfChanged(ref _rotateBoardAfterMove, value);
        }

        public int SelectedTimeLimitIndex
        {
            get => _selectedTimeLimitIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTimeLimitIndex, value);
                ResetTimer();
                UpdateTimeDisplay();
            }
        }

        public string CurrentPlayerDisplay => $"Current Player: {(_isPlayer1Turn ? "Red" : "Black")}";

        public string TimeDisplay
        {
            get => _timeDisplay;
            private set => this.RaiseAndSetIfChanged(ref _timeDisplay, value);
        }

        public ICommand SquareClickCommand { get; }
        public ICommand NewGameCommand { get; }

        public MainWindowViewModel()
        {
            SquareClickCommand = ReactiveCommand.Create<SquareViewModel>(OnSquareClick);
            NewGameCommand = ReactiveCommand.Create(InitializeGame);

            _moveTimer = new Timer(1000);
            _moveTimer.Elapsed += OnTimerTick;

            InitializeGame();
        }

        private void InitializeGame()
        {
            _isPlayer1Turn = true;
            this.RaisePropertyChanged(nameof(CurrentPlayerDisplay));
            
            BoardSquares = new ObservableCollection<SquareViewModel>();
            
            // Create the board
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    bool isDark = (row + col) % 2 == 1;
                    
                    var square = new SquareViewModel
                    {
                        Row = row,
                        Column = col,
                        Color = isDark ? "#663300" : "#FFCC99",
                        HasPiece = isDark && (row < 3 || row > 4), // Pieces on dark squares
                        PieceColor = row < 3 ? "Black" : "Red", // Player 2 (black) at top, Player 1 (red) at bottom
                        IsKing = false
                    };
                    
                    BoardSquares.Add(square);
                }
            }

            ResetTimer();
        }

        private void OnSquareClick(SquareViewModel clickedSquare)
        {
            // If no square is selected, try to select one with a piece
            if (_selectedSquare == null)
            {
                if (clickedSquare.HasPiece && IsCurrentPlayersPiece(clickedSquare))
                {
                    _selectedSquare = clickedSquare;
                    clickedSquare.IsSelected = true;
                }
                return;
            }

            // If clicking on the same square, deselect it
            if (_selectedSquare == clickedSquare)
            {
                _selectedSquare.IsSelected = false;
                _selectedSquare = null;
                return;
            }

            // If clicking on another piece of the same color, select that one instead
            if (clickedSquare.HasPiece && IsCurrentPlayersPiece(clickedSquare))
            {
                _selectedSquare.IsSelected = false;
                _selectedSquare = clickedSquare;
                clickedSquare.IsSelected = true;
                return;
            }

            // Trying to make a move
            if (IsValidMove(_selectedSquare, clickedSquare))
            {
                // Move the piece
                MovePiece(_selectedSquare, clickedSquare);

                // Reset the selection
                _selectedSquare.IsSelected = false;
                _selectedSquare = null;

                // Change turn
                _isPlayer1Turn = !_isPlayer1Turn;
                this.RaisePropertyChanged(nameof(CurrentPlayerDisplay));

                // Rotate board if enabled
                if (RotateBoardAfterMove)
                {
                    RotateBoard();
                }

                // Reset the move timer
                ResetTimer();
            }
        }

        private bool IsCurrentPlayersPiece(SquareViewModel square)
        {
            return (_isPlayer1Turn && square.PieceColor == "Red") || 
                   (!_isPlayer1Turn && square.PieceColor == "Black");
        }

        private bool IsValidMove(SquareViewModel from, SquareViewModel to)
        {
            // Basic movement validation
            if (to.HasPiece) return false;
            
            int rowDiff = to.Row - from.Row;
            int colDiff = to.Column - from.Column;
            
            // Calculate absolute differences
            int absRowDiff = Math.Abs(rowDiff);
            int absColDiff = Math.Abs(colDiff);
            
            // Check if the square is one of the diagonal squares
            bool isDiagonal = absRowDiff == absColDiff;
            if (!isDiagonal) return false;
            
            // Regular move (1 square diagonally)
            if (absRowDiff == 1)
            {
                // Red pieces move up (negative row diff) unless kinged
                if (from.PieceColor == "Red" && rowDiff > 0 && !from.IsKing) return false;
                
                // Black pieces move down (positive row diff) unless kinged
                if (from.PieceColor == "Black" && rowDiff < 0 && !from.IsKing) return false;
                
                return true;
            }
            
            // Jump move (2 squares diagonally)
            if (absRowDiff == 2 && absColDiff == 2)
            {
                // Red pieces move up unless kinged
                if (from.PieceColor == "Red" && rowDiff > 0 && !from.IsKing) return false;
                
                // Black pieces move down unless kinged
                if (from.PieceColor == "Black" && rowDiff < 0 && !from.IsKing) return false;
                
                // Check if there's an opponent's piece in between
                int midRow = (from.Row + to.Row) / 2;
                int midCol = (from.Column + to.Column) / 2;
                
                var midSquare = BoardSquares.FirstOrDefault(s => s.Row == midRow && s.Column == midCol);
                
                return midSquare != null && 
                       midSquare.HasPiece && 
                       midSquare.PieceColor != from.PieceColor;
            }
            
            return false;
        }

        private void MovePiece(SquareViewModel from, SquareViewModel to)
        {
            // Check if this is a jump move
            int rowDiff = to.Row - from.Row;
            int colDiff = to.Column - from.Column;
            
            if (Math.Abs(rowDiff) == 2 && Math.Abs(colDiff) == 2)
            {
                // Remove the jumped piece
                int midRow = (from.Row + to.Row) / 2;
                int midCol = (from.Column + to.Column) / 2;
                
                var midSquare = BoardSquares.FirstOrDefault(s => s.Row == midRow && s.Column == midCol);
                if (midSquare != null)
                {
                    midSquare.HasPiece = false;
                    midSquare.PieceColor = null;
                    midSquare.IsKing = false;
                }
            }
            
            // Move the piece
            to.HasPiece = true;
            to.PieceColor = from.PieceColor;
            to.IsKing = from.IsKing;
            
            from.HasPiece = false;
            from.PieceColor = null;
            from.IsKing = false;
            
            // Check if the piece should be kinged
            if ((to.PieceColor == "Red" && to.Row == 0) || 
                (to.PieceColor == "Black" && to.Row == 7))
            {
                to.IsKing = true;
            }
        }

        private void RotateBoard()
        {
            var newBoard = new ObservableCollection<SquareViewModel>();
            
            // Create rotated board
            for (int row = 7; row >= 0; row--)
            {
                for (int col = 7; col >= 0; col--)
                {
                    var oldSquare = BoardSquares.First(s => s.Row == row && s.Column == col);
                    var newSquare = new SquareViewModel
                    {
                        Row = 7 - row,
                        Column = 7 - col,
                        Color = oldSquare.Color,
                        HasPiece = oldSquare.HasPiece,
                        PieceColor = oldSquare.PieceColor,
                        IsKing = oldSquare.IsKing,
                        IsSelected = oldSquare.IsSelected
                    };
                    
                    newBoard.Add(newSquare);
                }
            }
            
            // Replace the board
            BoardSquares = newBoard;
        }

        private void ResetTimer()
        {
            _moveTimer.Stop();
            int timeLimit = _timeLimitsInSeconds[SelectedTimeLimitIndex];
            
            if (timeLimit > 0)
            {
                _remainingSeconds = timeLimit;
                UpdateTimeDisplay();
                _moveTimer.Start();
            }
            else
            {
                TimeDisplay = "Time: No Limit";
            }
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            _remainingSeconds--;
            
            // Update the display on UI thread
            Dispatcher.UIThread.Post(UpdateTimeDisplay);
            
            // Check if time is up
            if (_remainingSeconds <= 0)
            {
                _moveTimer.Stop();
                
                Dispatcher.UIThread.Post(() => 
                {
                    // Time's up, change turn
                    _isPlayer1Turn = !_isPlayer1Turn;
                    this.RaisePropertyChanged(nameof(CurrentPlayerDisplay));
                    
                    // Reset selection if any
                    if (_selectedSquare != null)
                    {
                        _selectedSquare.IsSelected = false;
                        _selectedSquare = null;
                    }
                    
                    // Rotate board if enabled
                    if (RotateBoardAfterMove)
                    {
                        RotateBoard();
                    }
                    
                    // Reset timer for next player
                    ResetTimer();
                });
            }
        }

        private void UpdateTimeDisplay()
        {
            if (_timeLimitsInSeconds[SelectedTimeLimitIndex] > 0)
            {
                TimeDisplay = $"Time: {_remainingSeconds / 60:00}:{_remainingSeconds % 60:00}";
            }
            else
            {
                TimeDisplay = "Time: No Limit";
            }
        }
    }
}