using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Timers;
using ReactiveUI;
using Avalonia.Threading;
using checkers.Models;

namespace checkers.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private CheckersBoard _gameBoard;
        private CheckersBot _bot;
        private bool _isPlayerTurn = true;
        private bool _isBotThinking;
        private Timer _moveTimer;
        private int _remainingSeconds;
        private int _selectedTimeLimitIndex;
        private bool _rotateBoardAfterMove;
        private ObservableCollection<SquareViewModel> _boardSquares;
        private SquareViewModel _selectedSquare;
        private string _statusMessage;
        private int[] _timeLimitsInSeconds = { 0, 30, 60, 180, 300 }; // No limit, 30s, 1m, 3m, 5m
        private bool _isShowingHint;
        private Position _hintFromPosition;
        private Position _hintToPosition;
        
        public bool IsShowingHint
        {
            get => _isShowingHint;
            set
            {
                this.RaiseAndSetIfChanged(ref _isShowingHint, value);
                UpdateBoardSquares(); // Refresh squares to show/hide hint
            }
        }

        public bool CanRequestHint => _isPlayerTurn && !_gameBoard.IsGameOver;

        public ICommand GetHintCommand { get; }

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
            }
        }

        public int SelectedDifficulty { get; set; } = 1;

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public string TimeDisplay => _timeLimitsInSeconds[SelectedTimeLimitIndex] > 0 
            ? $"Time: {_remainingSeconds / 60:00}:{_remainingSeconds % 60:00}" 
            : "Time: No Limit";

        public string CurrentTurn => _gameBoard.IsGameOver 
            ? $"Game Over - {(_gameBoard.Winner == PieceType.White ? "You" : "Bot")} Won!" 
            : $"Current Turn: {(_isPlayerTurn ? "Your Turn" : "Bot's Turn")}";

        public ICommand SquareClickCommand { get; }
        public ICommand NewGameCommand { get; }
        public ICommand ChangeDifficultyCommand { get; }

        public GameViewModel()
        {
            _gameBoard = new CheckersBoard();
            _bot = new CheckersBot(SelectedDifficulty);
            
            SquareClickCommand = ReactiveCommand.Create<SquareViewModel>(OnSquareClick);
            NewGameCommand = ReactiveCommand.Create(InitializeGame);
            ChangeDifficultyCommand = ReactiveCommand.Create<string>(difficultyStr => 
            {
                if (int.TryParse(difficultyStr, out int difficulty))
                {
                    SelectedDifficulty = difficulty;
                    _bot = new CheckersBot(difficulty);
                    StatusMessage = $"Difficulty set to {GetDifficultyName(difficulty)}";
                }
            });
            
            GetHintCommand = ReactiveCommand.CreateFromTask(
                GetHintAsync, 
                this.WhenAnyValue(x => x.CanRequestHint));

            _moveTimer = new Timer(1000);
            _moveTimer.Elapsed += OnTimerTick;

            InitializeGame();
        }

        private string GetDifficultyName(int difficulty) => difficulty switch
        {
            1 => "Easy",
            2 => "Medium",
            3 => "Hard",
            _ => "Medium"
        };

        public void InitializeGame()
        {
            try
            {
                // Create a new game board
                _gameBoard = new CheckersBoard();
        
                // Set initial game state
                _isPlayerTurn = true;
                _isBotThinking = false;
        
                // Update the UI with the initial board state
                UpdateBoardSquares();
                ResetTimer();
        
                // Update status messages
                StatusMessage = "New game started.";
        
                // Notify UI of property changes
                this.RaisePropertyChanged(nameof(CurrentTurn));
                this.RaisePropertyChanged(nameof(BoardSquares));
                this.RaisePropertyChanged(nameof(TimeDisplay));
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                StatusMessage = $"Error starting new game: {ex.Message}";
            }
        }

        private void UpdateBoardSquares()
        {
            BoardSquares = new ObservableCollection<SquareViewModel>();
    
            for (int row = 0; row < CheckersBoard.BoardSize; row++)
            {
                for (int col = 0; col < CheckersBoard.BoardSize; col++)
                {
                    var piece = _gameBoard.GetPieceAt(row, col);
                    bool isDark = (row + col) % 2 == 1;
            
                    var squareVM = new SquareViewModel
                    {
                        Row = row,
                        Column = col,
                        Color = isDark ? "#663300" : "#FFCC99",
                        HasPiece = piece.Type != PieceType.None,
                        PieceColor = piece.Type == PieceType.White ? "White" : 
                            piece.Type == PieceType.Black ? "Black" : null,
                        IsKing = piece.Rank == PieceRank.King,
                        IsHighlighted = false,
                
                        // Add hint highlighting
                        IsHintSource = IsShowingHint && _hintFromPosition != null && 
                                       row == _hintFromPosition.Row && col == _hintFromPosition.Col,
                              
                        IsHintTarget = IsShowingHint && _hintToPosition != null && 
                                       row == _hintToPosition.Row && col == _hintToPosition.Col
                    };
            
                    BoardSquares.Add(squareVM);
                }
            }
        }

        private void HighlightValidMoves(Position from)
        {
            // Reset all highlights
            foreach (var square in BoardSquares)
            {
                square.IsHighlighted = false;
            }

            // Get valid moves and highlight destination squares
            var validMoves = _gameBoard.GetValidMoves(from);
            foreach (var move in validMoves)
            {
                var squareVM = BoardSquares.FirstOrDefault(s => 
                    s.Row == move.To.Row && s.Column == move.To.Col);
                    
                if (squareVM != null)
                {
                    squareVM.IsHighlighted = true;
                }
            }
        }

        private async void OnSquareClick(SquareViewModel clickedSquare)
        {
            // Clear any showing hint when user interacts with the board
            if (IsShowingHint)
            {
                IsShowingHint = false;
            }
            
            if (!_isPlayerTurn || _gameBoard.IsGameOver || _isBotThinking)
                return;

            var position = new Position(clickedSquare.Row, clickedSquare.Column);
            var piece = _gameBoard.GetPieceAt(position.Row, position.Col);

            // If no square is selected, try to select one with a piece
            if (_selectedSquare == null)
            {
                if (piece.Type == PieceType.White) // Player's pieces are red
                {
                    _selectedSquare = clickedSquare;
                    clickedSquare.IsSelected = true;
                    HighlightValidMoves(position);
                }
                return;
            }

            // If clicking on the same square, deselect it
            if (_selectedSquare == clickedSquare)
            {
                _selectedSquare.IsSelected = false;
                _selectedSquare = null;
                
                // Remove highlights
                foreach (var square in BoardSquares)
                {
                    square.IsHighlighted = false;
                }
                return;
            }

            // If clicking on another piece of the same color, select that one instead
            if (piece.Type == PieceType.White)
            {
                _selectedSquare.IsSelected = false;
                _selectedSquare = clickedSquare;
                clickedSquare.IsSelected = true;
                HighlightValidMoves(position);
                return;
            }

            // Try to make a move
            var fromPosition = new Position(_selectedSquare.Row, _selectedSquare.Column);
            var toPosition = position;
            
            if (_gameBoard.TryMove(fromPosition, toPosition))
            {
                // Move succeeded
                _selectedSquare.IsSelected = false;
                _selectedSquare = null;
                
                // Remove highlights
                foreach (var square in BoardSquares)
                {
                    square.IsHighlighted = false;
                }
                
                // Update the visual board
                UpdateBoardSquares();
                
                // Check if the game is over
                if (_gameBoard.IsGameOver)
                {
                    StatusMessage = $"Game over! {(_gameBoard.Winner == PieceType.White ? "You" : "Bot")} won!";
                    this.RaisePropertyChanged(nameof(CurrentTurn));
                    return;
                }
                
                // If we need to check for additional jumps, don't switch turns yet
                var additionalJumps = _gameBoard.GetValidMoves(toPosition)
                    .Where(m => m.Jumped != null).ToList();
                
                if (additionalJumps.Any())
                {
                    // Select the piece for continued jumps
                    var jumperSquare = BoardSquares.FirstOrDefault(s => 
                        s.Row == toPosition.Row && s.Column == toPosition.Col);
                        
                    if (jumperSquare != null)
                    {
                        _selectedSquare = jumperSquare;
                        _selectedSquare.IsSelected = true;
                        HighlightValidMoves(toPosition);
                        StatusMessage = "Jump again with the same piece!";
                        return;
                    }
                }
                
                // Rotate board if enabled
                if (RotateBoardAfterMove)
                {
                    RotateBoard();
                }
                
                // Switch to bot's turn
                _isPlayerTurn = false;
                this.RaisePropertyChanged(nameof(CurrentTurn));
                ResetTimer();
                
                StatusMessage = "Bot is thinking...";
                _isBotThinking = true;
                
                // Have the bot make its move after a short delay
                await Task.Delay(500); // Give the UI time to update
                await MakeBotMoveAsync();
            }
        }

        private async Task MakeBotMoveAsync()
        {
            if (_gameBoard.IsGameOver)
                return;
                
            // Get the bot's move
            var botMove = await _bot.GetMoveWithDelayAsync(_gameBoard);
            
            if (botMove == null)
            {
                _isPlayerTurn = true;
                _isBotThinking = false;
                StatusMessage = "Bot couldn't move. Your turn.";
                this.RaisePropertyChanged(nameof(CurrentTurn));
                return;
            }

            // Execute the move
            _gameBoard.TryMove(botMove.From, botMove.To);
            
            // Update the board
            UpdateBoardSquares();
            
            // Check if game is over after bot's move
            if (_gameBoard.IsGameOver)
            {
                _isBotThinking = false;
                StatusMessage = $"Game over! {(_gameBoard.Winner == PieceType.White ? "You" : "Bot")} won!";
                this.RaisePropertyChanged(nameof(CurrentTurn));
                return;
            }
            
            // Check for additional jumps by the bot
            Position lastPosition = botMove.To;
            bool continuedJump = false;
            
            // Continue jumps if possible
            while (_bot.CanContinueJumping(_gameBoard, lastPosition))
            {
                continuedJump = true;
                await Task.Delay(500); // Pause between consecutive jumps
                
                var nextJump = await _bot.GetMoveWithDelayAsync(_gameBoard);
                if (nextJump == null) break;
                
                _gameBoard.TryMove(nextJump.From, nextJump.To);
                lastPosition = nextJump.To;
                UpdateBoardSquares();
                
                // Check if game ended after continued jump
                if (_gameBoard.IsGameOver)
                {
                    _isBotThinking = false;
                    StatusMessage = $"Game over! {(_gameBoard.Winner == PieceType.White ? "You" : "Bot")} won!";
                    this.RaisePropertyChanged(nameof(CurrentTurn));
                    return;
                }
            }
            
            if (continuedJump)
            {
                StatusMessage = "Bot made multiple jumps!";
            }
            
            // Rotate board if enabled
            if (RotateBoardAfterMove)
            {
                RotateBoard();
            }
            
            // Return to player's turn
            _isPlayerTurn = true;
            _isBotThinking = false;
            
            StatusMessage = "Your turn";
            this.RaisePropertyChanged(nameof(CurrentTurn));
            
            // Reset the timer for player
            ResetTimer();
        }
        
        private void RotateBoard()
        {
            var newBoard = new ObservableCollection<SquareViewModel>();
            
            // Create a rotated board
            for (int row = 7; row >= 0; row--)
            {
                for (int col = 7; col >= 0; col--)
                {
                    var oldSquare = BoardSquares.First(s => 
                        s.Row == row && s.Column == col);
                        
                    var newSquare = new SquareViewModel
                    {
                        Row = 7 - row,
                        Column = 7 - col,
                        Color = oldSquare.Color,
                        HasPiece = oldSquare.HasPiece,
                        PieceColor = oldSquare.PieceColor,
                        IsKing = oldSquare.IsKing,
                        IsSelected = oldSquare.IsSelected,
                        IsHighlighted = oldSquare.IsHighlighted
                    };
                    
                    newBoard.Add(newSquare);
                }
            }
            
            // Update the board
            BoardSquares = newBoard;
        }
        
        private async Task GetHintAsync()
        {
            if (!CanRequestHint) return;
    
            StatusMessage = "Thinking about the best move...";
    
            // Create a temporary bot for generating hints
            var hintBot = new CheckersBot(3); // Use max difficulty for best hints
    
            try
            {
                // Get the best move from the bot
                var bestMove = await hintBot.GetBestMoveAsync(_gameBoard);
        
                if (bestMove != null)
                {
                    // Store the hint positions
                    _hintFromPosition = bestMove.From;
                    _hintToPosition = bestMove.To;
            
                    // Show the hint
                    IsShowingHint = true;
            
                    StatusMessage = "Hint: Move highlighted piece";
                }
                else
                {
                    StatusMessage = "No good moves available!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error getting hint: {ex.Message}";
            }
        }
        
        private void ResetTimer()
        {
            _moveTimer.Stop();
            int timeLimit = _timeLimitsInSeconds[SelectedTimeLimitIndex];
            
            if (timeLimit > 0)
            {
                _remainingSeconds = timeLimit;
                this.RaisePropertyChanged(nameof(TimeDisplay));
                _moveTimer.Start();
            }
            else
            {
                this.RaisePropertyChanged(nameof(TimeDisplay));
            }
        }
        
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            _remainingSeconds--;
            
            // Update the display on UI thread
            Dispatcher.UIThread.Post(() => {
                this.RaisePropertyChanged(nameof(TimeDisplay));
            });
            
            // Check if time is up
            if (_remainingSeconds <= 0)
            {
                _moveTimer.Stop();
                
                Dispatcher.UIThread.Post(() => {
                    if (_isPlayerTurn)
                    {
                        // Time's up for player, switch to bot
                        StatusMessage = "Time's up! Bot's turn now.";
                        _isPlayerTurn = false;
                        this.RaisePropertyChanged(nameof(CurrentTurn));
                        
                        // Reset selection if any
                        if (_selectedSquare != null)
                        {
                            _selectedSquare.IsSelected = false;
                            _selectedSquare = null;
                            
                            // Remove highlights
                            foreach (var square in BoardSquares)
                            {
                                square.IsHighlighted = false;
                            }
                        }
                        
                        // Bot makes its move
                        _isBotThinking = true;
                        Task.Run(async () => {
                            await Task.Delay(500); // Give the UI time to update
                            await MakeBotMoveAsync();
                        });
                    }
                });
            }
        }
    }
}