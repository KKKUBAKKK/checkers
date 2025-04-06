using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using checkers.controllers;
using checkers.Models;
using Avalonia.Input;


namespace checkers.views;
public partial class MainWindow : Window
{
    private const int BoardSize = 8;
    private const int SquareSize = 60;
        
    private GameController _gameController;
    private Rectangle[,] _squares;
    private Ellipse[,] _pieces;
    private Rectangle _selectedSquare;
    private List<Rectangle> _highlightedMoves;
    private TextBlock _statusText;
    private TextBlock _hintText;
    private Button _hintButton;
        
    // Store the board coordinates of the selected piece
    private int _selectedBoardRow = -1;
    private int _selectedBoardCol = -1;
        
    // Configuration
    private string _openAiApiKey = "your-api-key-here"; // Replace with your OpenAI API key
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeGame();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void InitializeGame()
    {
        // Initialize the game controller
        _gameController = new GameController(_openAiApiKey);
        _gameController.BoardUpdated += OnBoardUpdated;
        _gameController.HintReceived += OnHintReceived;
        
        // Initialize UI
        _squares = new Rectangle[BoardSize, BoardSize];
        _pieces = new Ellipse[BoardSize, BoardSize];
        _highlightedMoves = new List<Rectangle>();
        
        // Create main grid
        Grid mainGrid = new Grid();
        
        // Create board panel
        Canvas boardPanel = new Canvas
        {
            Width = BoardSize * SquareSize,
            Height = BoardSize * SquareSize,
            Background = new SolidColorBrush(Colors.LightGray)
        };
        
        // Create control panel
        StackPanel controlPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(20)
        };
        
        // Add new game button
        Button newGameButton = new Button
        {
            Content = "New Game",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 10)
        };
        newGameButton.Click += (s, e) => _gameController.Reset();
        controlPanel.Children.Add(newGameButton);
        
        // Add get hint button
        _hintButton = new Button
        {
            Content = "Get Hint from ChatGPT",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 10)
        };
        _hintButton.Click += async (s, e) => await GetHint();
        controlPanel.Children.Add(_hintButton);
        
        // Add status text
        _statusText = new TextBlock
        {
            Text = "Your turn",
            Margin = new Thickness(0, 10, 0, 10),
            FontSize = 16
        };
        controlPanel.Children.Add(_statusText);
        
        // Add hint text
        _hintText = new TextBlock
        {
            Text = "No hint available yet",
            Margin = new Thickness(0, 10, 0, 10),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300
        };
        controlPanel.Children.Add(_hintText);
        
        // Create the board squares and pieces
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                // Create square
                Rectangle square = new Rectangle
                {
                    Width = SquareSize,
                    Height = SquareSize,
                    Fill = (row + col) % 2 == 0 
                        ? new SolidColorBrush(Colors.Wheat) 
                        : new SolidColorBrush(Colors.SaddleBrown)
                };
                
                // Store coordinates as attached property
                square.SetValue(Grid.RowProperty, row);
                square.SetValue(Grid.ColumnProperty, col);
                
                Canvas.SetLeft(square, col * SquareSize);
                Canvas.SetTop(square, row * SquareSize);
                boardPanel.Children.Add(square);
                _squares[row, col] = square;
                
                // Add pointer pressed event to squares
                square.PointerPressed += SquareClicked;
                
                // Create piece (initially invisible)
                Ellipse piece = new Ellipse
                {
                    Width = SquareSize * 0.8,
                    Height = SquareSize * 0.8,
                    IsVisible = false,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2
                };
                
                // Store coordinates in the piece
                piece.SetValue(Grid.RowProperty, row);
                piece.SetValue(Grid.ColumnProperty, col);
                
                Canvas.SetLeft(piece, col * SquareSize + SquareSize * 0.1);
                Canvas.SetTop(piece, row * SquareSize + SquareSize * 0.1);
                boardPanel.Children.Add(piece);
                _pieces[row, col] = piece;
                
                // Add pointer pressed event to pieces as well
                piece.PointerPressed += PieceClicked;
            }
        }
        
        // Add board and controls to main grid
        Grid.SetColumn(boardPanel, 0);
        Grid.SetColumn(controlPanel, 1);
        
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(BoardSize * SquareSize) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        
        mainGrid.Children.Add(boardPanel);
        mainGrid.Children.Add(controlPanel);
        
        // Set the main content
        this.Content = mainGrid;
        
        // Add debug logging of available moves
        LogAvailableMoves();
        
        // Update the board
        UpdateBoardDisplay();
    }

    private void LogAvailableMoves()
    {
        Console.WriteLine("Available moves at start:");
        foreach (var move in _gameController.AvailableMoves)
        {
            Console.WriteLine($"From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
        }
    }

    private void OnBoardUpdated(object sender, Board board)
    {
        Dispatcher.UIThread.InvokeAsync(() => {
            UpdateBoardDisplay();
            UpdateGameStatus();
            
            // Log available moves after board update
            Console.WriteLine("Available moves after update:");
            foreach (var move in _gameController.AvailableMoves)
            {
                Console.WriteLine($"From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
            }
        });
    }

    private void OnHintReceived(object sender, string hint)
    {
        Dispatcher.UIThread.InvokeAsync(() => {
            _hintText.Text = hint;
            _hintButton.IsEnabled = true;
        });
    }

    private void UpdateBoardDisplay()
    {
        // Clear any previous selections
        ClearSelection();
        
        // Update pieces based on the board state
        for (int uiRow = 0; uiRow < BoardSize; uiRow++)
        {
            for (int uiCol = 0; uiCol < BoardSize; uiCol++)
            {
                // Convert UI to board coordinates
                int boardRow = _gameController.UIToBoardRow(uiRow);
                int boardCol = _gameController.UIToBoardCol(uiCol);
                
                // Get piece using board coordinates
                int piece = _gameController.GetPieceAt(boardRow, boardCol);
                Ellipse pieceUI = _pieces[uiRow, uiCol];
                
                if (piece == 0)
                {
                    pieceUI.IsVisible = false;
                }
                else
                {
                    pieceUI.IsVisible = true;
                    
                    // Set the piece appearance consistently
                    if (piece > 0) // White/positive pieces are player's (WHITE)
                    {
                        pieceUI.Fill = new SolidColorBrush(Colors.White);
                    }
                    else // Black/negative pieces are opponent's (BLACK)
                    {
                        pieceUI.Fill = new SolidColorBrush(Colors.Black);
                    }
                    
                    // Show crown for kings
                    if (Math.Abs(piece) == 2) // King
                    {
                        pieceUI.Stroke = new SolidColorBrush(Colors.Gold);
                        pieceUI.StrokeThickness = 3;
                    }
                    else
                    {
                        pieceUI.Stroke = new SolidColorBrush(Colors.Black);
                        pieceUI.StrokeThickness = 2;
                    }
                }
            }
        }
    }

    private void UpdateGameStatus()
    {
        if (_gameController.IsPlayerTurn)
        {
            if (_gameController.AvailableMoves.Count == 0)
            {
                _statusText.Text = "Game over! You lose.";
            }
            else
            {
                _statusText.Text = "Your turn";
            }
        }
        else
        {
            if (_gameController.AvailableMoves.Count == 0)
            {
                _statusText.Text = "Game over! You win!";
            }
            else
            {
                _statusText.Text = "Computer is thinking...";
            }
        }
    }

    // Handle clicks on pieces
    private void PieceClicked(object sender, PointerPressedEventArgs e)
    {
        if (!_gameController.IsPlayerTurn)
            return;
        
        Ellipse clickedPiece = sender as Ellipse;
        
        // Get UI coordinates
        int clickedUIRow = (int)clickedPiece.GetValue(Grid.RowProperty);
        int clickedUICol = (int)clickedPiece.GetValue(Grid.ColumnProperty);
        
        // Immediately convert to board coordinates
        int clickedBoardRow = _gameController.UIToBoardRow(clickedUIRow);
        int clickedBoardCol = _gameController.UIToBoardCol(clickedUICol);
        
        Console.WriteLine($"Piece clicked at UI ({clickedUIRow},{clickedUICol}) = Board ({clickedBoardRow},{clickedBoardCol})");
        
        // Get the piece at the clicked position using board coordinates
        int piece = _gameController.GetPieceAt(clickedBoardRow, clickedBoardCol);
        Console.WriteLine($"Piece value: {piece}");
        
        // Delegate to HandlePositionClick for common logic (using board coordinates)
        HandlePositionClick(clickedBoardRow, clickedBoardCol, piece, _squares[clickedUIRow, clickedUICol]);
        
        // Mark event as handled to prevent it propagating to the square underneath
        e.Handled = true;
    }

    private void SquareClicked(object sender, PointerPressedEventArgs e)
    {
        if (!_gameController.IsPlayerTurn)
            return;
        
        Rectangle clickedSquare = sender as Rectangle;
        
        // Get UI coordinates
        int clickedUIRow = (int)clickedSquare.GetValue(Grid.RowProperty);
        int clickedUICol = (int)clickedSquare.GetValue(Grid.ColumnProperty);
        
        // Immediately convert to board coordinates
        int clickedBoardRow = _gameController.UIToBoardRow(clickedUIRow);
        int clickedBoardCol = _gameController.UIToBoardCol(clickedUICol);
        
        Console.WriteLine($"Square clicked at UI ({clickedUIRow},{clickedUICol}) = Board ({clickedBoardRow},{clickedBoardCol})");
        
        // Get the piece at the clicked square using board coordinates
        int piece = _gameController.GetPieceAt(clickedBoardRow, clickedBoardCol);
        Console.WriteLine($"Piece value: {piece}");
        
        // Delegate to HandlePositionClick for common logic (using board coordinates)
        HandlePositionClick(clickedBoardRow, clickedBoardCol, piece, clickedSquare);
    }
    
    // Common logic for handling clicks on board positions
    // All coordinates here are board coordinates, not UI coordinates
    private void HandlePositionClick(int clickedBoardRow, int clickedBoardCol, int piece, Rectangle clickedSquare)
    {
        // If a piece is selected, attempt to make a move
        if (_selectedSquare != null)
        {
            // If clicking on the same square, deselect it
            if (clickedSquare == _selectedSquare)
            {
                ClearSelection();
                return;
            }
            
            // Get the coordinates of the selected piece
            int fromBoardRow = _selectedBoardRow;
            int fromBoardCol = _selectedBoardCol;
            
            // Attempt to make a move
            bool moveSuccessful = _gameController.TryMakeMove(fromBoardRow, fromBoardCol, clickedBoardRow, clickedBoardCol);
            Console.WriteLine($"Tried move from ({fromBoardRow},{fromBoardCol}) to ({clickedBoardRow},{clickedBoardCol}): {(moveSuccessful ? "SUCCESS" : "FAILED")}");
            
            if (!moveSuccessful && piece > 0)
            {
                // If we couldn't move, but clicked on another player piece, select that piece instead
                SelectPiece(clickedBoardRow, clickedBoardCol, clickedSquare);
            }
            
            return;
        }
        
        // If no piece is selected yet, try to select one
        if (piece > 0) // Only allow selecting player pieces (white/positive values)
        {
            SelectPiece(clickedBoardRow, clickedBoardCol, clickedSquare);
        }
    }
    
    // Helper method to select a piece and highlight its moves
    private void SelectPiece(int boardRow, int boardCol, Rectangle square)
    {
        // Get moves for this piece
        var pieceMoves = _gameController.GetMovesForPiece(boardRow, boardCol);
        
        if (pieceMoves.Any())
        {
            // Clear any existing selection
            ClearSelection();
            
            // Select this piece
            _selectedSquare = square;
            _selectedBoardRow = boardRow;
            _selectedBoardCol = boardCol;
            
            // Highlight the selected piece
            square.Stroke = new SolidColorBrush(Colors.Yellow);
            square.StrokeThickness = 3;
            
            // Highlight the valid destinations
            HighlightDestinations(pieceMoves);
        }
        else
        {
            Console.WriteLine("This piece has no available moves");
        }
    }
    
    // Highlight only the final destinations of the moves (for multi-captures, only show the end position)
    private void HighlightDestinations(List<Move> moves)
    {
        ClearHighlightedMoves();
        
        foreach (var move in moves)
        {
            // For multi-captures, connect the dots to show the path
            if (move.IsCapture)
            {
                // For captures, we want to show the path and then the final destination
                // This is where you would draw the path line if desired
                
                // For now, we'll just highlight the final destination
                int uiRow = _gameController.BoardToUIRow(move.ToRow);
                int uiCol = _gameController.BoardToUICol(move.ToCol);
                
                Rectangle targetSquare = _squares[uiRow, uiCol];
                targetSquare.Stroke = new SolidColorBrush(Colors.LimeGreen);
                targetSquare.StrokeThickness = 3;
                _highlightedMoves.Add(targetSquare);
            }
            else
            {
                // For normal moves, just highlight the destination
                int uiRow = _gameController.BoardToUIRow(move.ToRow);
                int uiCol = _gameController.BoardToUICol(move.ToCol);
                
                Rectangle targetSquare = _squares[uiRow, uiCol];
                targetSquare.Stroke = new SolidColorBrush(Colors.LimeGreen);
                targetSquare.StrokeThickness = 3;
                _highlightedMoves.Add(targetSquare);
            }
        }
    }

    private void ClearHighlightedMoves()
    {
        foreach (var square in _highlightedMoves)
        {
            square.Stroke = null;
            square.StrokeThickness = 0;
        }
        _highlightedMoves.Clear();
    }

    private void ClearSelection()
    {
        if (_selectedSquare != null)
        {
            _selectedSquare.Stroke = null;
            _selectedSquare.StrokeThickness = 0;
            _selectedSquare = null;
            _selectedBoardRow = -1;
            _selectedBoardCol = -1;
        }
        
        ClearHighlightedMoves();
    }

    private async Task GetHint()
    {
        if (_gameController.IsPlayerTurn)
        {
            _hintButton.IsEnabled = false;
            _hintText.Text = "Getting hint from ChatGPT...";
            await _gameController.GetHintFromChatGpt();
        }
        else
        {
            _hintText.Text = "You can only get hints during your turn.";
        }
    }
}
