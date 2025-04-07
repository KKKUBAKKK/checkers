using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using checkers.Models;
using checkers.ViewModels;

namespace checkers.views;

/// <summary>
/// Main window of the checkers game, handling UI and game interactions
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Size of the game board (8x8)
    /// </summary>
    private const int BoardSize = 8;

    /// <summary>
    /// Size of each square on the board in pixels
    /// </summary>
    private const int SquareSize = 60;

    /// <summary>
    /// Game ViewModel handling game logic and state
    /// </summary>
    private GameViewModel _gameViewModel;

    /// <summary>
    /// 2D array of board squares
    /// </summary>
    private Rectangle[,] _squares;

    /// <summary>
    /// 2D array of game pieces
    /// </summary>
    private Ellipse[,] _pieces;

    /// <summary>
    /// Currently selected square on the board
    /// </summary>
    private Rectangle? _selectedSquare;

    /// <summary>
    /// List of squares highlighted for valid moves
    /// </summary>
    private List<Rectangle> _highlightedMoves;

    /// <summary>
    /// Text block showing current game status
    /// </summary>
    private TextBlock _statusText;

    /// <summary>
    /// Text block showing AI hint
    /// </summary>
    private TextBlock _hintText;

    /// <summary>
    /// Button to request hint from ChatGPT
    /// </summary>
    private Button _hintButton;

    /// <summary>
    /// Selected piece's board row position
    /// </summary>
    private int _selectedBoardRow = -1;

    /// <summary>
    /// Selected piece's board column position
    /// </summary>
    private int _selectedBoardCol = -1;

    /// <summary>
    /// OpenAI API key for ChatGPT integration
    /// </summary>
    private string _openAiApiKey;

    /// <summary>
    /// Initializes the main window and its components
    /// </summary>
    public MainWindow()
    {
        this.TransparencyLevelHint = new List<WindowTransparencyLevel>
        {
            WindowTransparencyLevel.AcrylicBlur,
        };
        this.Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20));
        this.ExtendClientAreaToDecorationsHint = true;

        InitializeComponent();

        _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "your-api-key-here";

        InitializeGame();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    /// <summary>
    /// Initializes main window components and UI elements
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Initializes the game components, UI elements, and board layout
    /// </summary>
    private void InitializeGame()
    {
        _gameViewModel = new GameViewModel(_openAiApiKey);
        _gameViewModel.BoardUpdated += OnBoardUpdated;
        _gameViewModel.HintReceived += OnHintReceived;

        _squares = new Rectangle[BoardSize, BoardSize];
        _pieces = new Ellipse[BoardSize, BoardSize];
        _highlightedMoves = new List<Rectangle>();

        DockPanel rootPanel = new DockPanel
        {
            LastChildFill = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = null,
        };

        Grid mainGrid = new Grid
        {
            Margin = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Grid boardGrid = new Grid();

        boardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        boardGrid.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(BoardSize * SquareSize) }
        );
        boardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

        boardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        boardGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(BoardSize * SquareSize) }
        );
        boardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

        Canvas boardPanel = new Canvas
        {
            Width = BoardSize * SquareSize,
            Height = BoardSize * SquareSize,
            Background = new SolidColorBrush(Colors.LightGray),
        };

        Grid.SetRow(boardPanel, 1);
        Grid.SetColumn(boardPanel, 1);
        boardGrid.Children.Add(boardPanel);

        Grid topLabelsGrid = new Grid();
        Grid bottomLabelsGrid = new Grid();

        for (int i = 0; i < BoardSize; i++)
        {
            topLabelsGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(SquareSize) }
            );
            bottomLabelsGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(SquareSize) }
            );
        }

        for (int col = 0; col < BoardSize; col++)
        {
            TextBlock topLabel = new TextBlock
            {
                Text = ((char)('A' + col)).ToString(),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(topLabel, col);
            topLabelsGrid.Children.Add(topLabel);

            TextBlock bottomLabel = new TextBlock
            {
                Text = ((char)('A' + col)).ToString(),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(bottomLabel, col);
            bottomLabelsGrid.Children.Add(bottomLabel);
        }

        Grid.SetRow(topLabelsGrid, 0);
        Grid.SetColumn(topLabelsGrid, 1);
        boardGrid.Children.Add(topLabelsGrid);

        Grid.SetRow(bottomLabelsGrid, 2);
        Grid.SetColumn(bottomLabelsGrid, 1);
        boardGrid.Children.Add(bottomLabelsGrid);

        Grid leftLabelsGrid = new Grid();
        Grid rightLabelsGrid = new Grid();

        for (int i = 0; i < BoardSize; i++)
        {
            leftLabelsGrid.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(SquareSize) }
            );
            rightLabelsGrid.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(SquareSize) }
            );
        }

        for (int row = 0; row < BoardSize; row++)
        {
            TextBlock leftLabel = new TextBlock
            {
                Text = (BoardSize - row).ToString(),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(leftLabel, row);
            leftLabelsGrid.Children.Add(leftLabel);

            TextBlock rightLabel = new TextBlock
            {
                Text = (BoardSize - row).ToString(),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(rightLabel, row);
            rightLabelsGrid.Children.Add(rightLabel);
        }

        Grid.SetRow(leftLabelsGrid, 1);
        Grid.SetColumn(leftLabelsGrid, 0);
        boardGrid.Children.Add(leftLabelsGrid);

        Grid.SetRow(rightLabelsGrid, 1);
        Grid.SetColumn(rightLabelsGrid, 2);
        boardGrid.Children.Add(rightLabelsGrid);

        Border boardBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.DarkGray),
            BorderThickness = new Thickness(2),
            Margin = new Thickness(0, 0, 10, 0),
            Child = boardGrid,
        };

        Border controlBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.DarkGray),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(15),
            Margin = new Thickness(10, 0, 0, 0),
            Background = null,
            Child = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };
        StackPanel controlPanel = (StackPanel)controlBorder.Child;

        Border statusBorder = new Border
        {
            BorderBrush = null,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 10, 0, 10),
            Background = null,
            Child = new TextBlock
            {
                Text = "Your turn",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };
        _statusText = (TextBlock)statusBorder.Child;
        controlPanel.Children.Add(statusBorder);

        StackPanel thinkingTimePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 10, 0, 10),
        };

        TextBlock thinkingTimeLabel = new TextBlock
        {
            Text = "Bot thinking time: 5s",
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Slider thinkingTimeSlider = new Slider
        {
            Minimum = 1,
            Maximum = 20,
            Value = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            TickPlacement = TickPlacement.BottomRight,
        };

        thinkingTimeSlider.ValueChanged += (s, e) =>
        {
            int seconds = (int)e.NewValue;
            thinkingTimeLabel.Text = $"Bot thinking time: {seconds}s";
            _gameViewModel.BotThinkingTime = TimeSpan.FromSeconds(seconds);
        };

        thinkingTimePanel.Children.Add(thinkingTimeLabel);
        thinkingTimePanel.Children.Add(thinkingTimeSlider);
        controlPanel.Children.Add(thinkingTimePanel);

        Button newGameButton = new Button
        {
            Content = "New Game",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        newGameButton.Click += (s, e) => _gameViewModel.Reset();
        controlPanel.Children.Add(newGameButton);

        _hintButton = new Button
        {
            Content = "Get Hint from ChatGPT",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _hintButton.Click += async (s, e) => await GetHint();
        controlPanel.Children.Add(_hintButton);

        Border hintBorder = new Border
        {
            BorderBrush = null,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            MinHeight = 100,
            Background = null,
            Child = new TextBlock
            {
                Text = "No hint available yet",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
            },
        };
        _hintText = (TextBlock)hintBorder.Child;
        controlPanel.Children.Add(hintBorder);

        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                Rectangle square = new Rectangle
                {
                    Width = SquareSize,
                    Height = SquareSize,
                    Fill =
                        (row + col) % 2 == 0
                            ? new SolidColorBrush(Colors.Wheat)
                            : new SolidColorBrush(Colors.SaddleBrown),
                };

                square.SetValue(Grid.RowProperty, row);
                square.SetValue(Grid.ColumnProperty, col);

                Canvas.SetLeft(square, col * SquareSize);
                Canvas.SetTop(square, row * SquareSize);
                boardPanel.Children.Add(square);
                _squares[row, col] = square;

                square.PointerPressed += SquareClicked;

                Ellipse piece = new Ellipse
                {
                    Width = SquareSize * 0.8,
                    Height = SquareSize * 0.8,
                    IsVisible = false,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2,
                };

                piece.SetValue(Grid.RowProperty, row);
                piece.SetValue(Grid.ColumnProperty, col);

                Canvas.SetLeft(piece, col * SquareSize + SquareSize * 0.1);
                Canvas.SetTop(piece, row * SquareSize + SquareSize * 0.1);
                boardPanel.Children.Add(piece);
                _pieces[row, col] = piece;

                piece.PointerPressed += PieceClicked;
            }
        }

        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(boardBorder, 0);
        Grid.SetColumn(controlBorder, 1);

        mainGrid.Children.Add(boardBorder);
        mainGrid.Children.Add(controlBorder);

        rootPanel.Children.Add(mainGrid);

        this.Content = rootPanel;

        UpdateBoardDisplay();
    }

    /// <summary>
    /// Updates the UI when the game board state changes
    /// </summary>
    private void OnBoardUpdated(object sender, Board board)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateBoardDisplay();
            UpdateGameStatus();
        });
    }

    /// <summary>
    /// Updates the hint text when a new hint is received
    /// </summary>
    private void OnHintReceived(object sender, string hint)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _hintText.Text = hint;
            _hintButton.IsEnabled = true;
        });
    }

    /// <summary>
    /// Updates the visual representation of the game board
    /// </summary>
    private void UpdateBoardDisplay()
    {
        ClearSelection();

        for (int uiRow = 0; uiRow < BoardSize; uiRow++)
        {
            for (int uiCol = 0; uiCol < BoardSize; uiCol++)
            {
                int boardRow = _gameViewModel.UIToBoardRow(uiRow);
                int boardCol = _gameViewModel.UIToBoardCol(uiCol);

                int piece = _gameViewModel.GetPieceAt(boardRow, boardCol);
                Ellipse pieceUI = _pieces[uiRow, uiCol];

                if (piece == 0)
                {
                    pieceUI.IsVisible = false;
                }
                else
                {
                    pieceUI.IsVisible = true;

                    if (piece > 0)
                    {
                        pieceUI.Fill = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        pieceUI.Fill = new SolidColorBrush(Colors.Black);
                    }

                    if (Math.Abs(piece) == 2)
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

    /// <summary>
    /// Updates the game status text based on current game state
    /// </summary>
    private void UpdateGameStatus()
    {
        if (_gameViewModel.IsPlayerTurn)
        {
            if (_gameViewModel.AvailableMoves.Count == 0)
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
            if (_gameViewModel.AvailableMoves.Count == 0)
            {
                _statusText.Text = "Game over! You win!";
            }
            else
            {
                _statusText.Text = "Computer is thinking...";
            }
        }
    }

    /// <summary>
    /// Handles clicks on game pieces
    /// </summary>
    private void PieceClicked(object sender, PointerPressedEventArgs e)
    {
        if (!_gameViewModel.IsPlayerTurn)
            return;

        Ellipse clickedPiece = sender as Ellipse;

        int clickedUIRow = (int)clickedPiece.GetValue(Grid.RowProperty);
        int clickedUICol = (int)clickedPiece.GetValue(Grid.ColumnProperty);

        int clickedBoardRow = _gameViewModel.UIToBoardRow(clickedUIRow);
        int clickedBoardCol = _gameViewModel.UIToBoardCol(clickedUICol);

        int piece = _gameViewModel.GetPieceAt(clickedBoardRow, clickedBoardCol);

        HandlePositionClick(
            clickedBoardRow,
            clickedBoardCol,
            piece,
            _squares[clickedUIRow, clickedUICol]
        );

        e.Handled = true;
    }

    /// <summary>
    /// Handles clicks on board squares
    /// </summary>
    private void SquareClicked(object sender, PointerPressedEventArgs e)
    {
        if (!_gameViewModel.IsPlayerTurn)
            return;

        Rectangle clickedSquare = sender as Rectangle;

        int clickedUIRow = (int)clickedSquare.GetValue(Grid.RowProperty);
        int clickedUICol = (int)clickedSquare.GetValue(Grid.ColumnProperty);

        int clickedBoardRow = _gameViewModel.UIToBoardRow(clickedUIRow);
        int clickedBoardCol = _gameViewModel.UIToBoardCol(clickedUICol);

        int piece = _gameViewModel.GetPieceAt(clickedBoardRow, clickedBoardCol);

        HandlePositionClick(clickedBoardRow, clickedBoardCol, piece, clickedSquare);
    }

    /// <summary>
    /// Common logic for handling clicks on board positions
    /// </summary>
    private void HandlePositionClick(
        int clickedBoardRow,
        int clickedBoardCol,
        int piece,
        Rectangle clickedSquare
    )
    {
        if (_selectedSquare != null)
        {
            if (clickedSquare == _selectedSquare)
            {
                ClearSelection();
                return;
            }

            int fromBoardRow = _selectedBoardRow;
            int fromBoardCol = _selectedBoardCol;

            bool moveSuccessful = _gameViewModel.TryMakeMove(
                fromBoardRow,
                fromBoardCol,
                clickedBoardRow,
                clickedBoardCol
            );

            if (!moveSuccessful && piece > 0)
            {
                SelectPiece(clickedBoardRow, clickedBoardCol, clickedSquare);
            }

            return;
        }

        if (piece > 0)
        {
            SelectPiece(clickedBoardRow, clickedBoardCol, clickedSquare);
        }
    }

    /// <summary>
    /// Selects a piece and highlights its valid moves
    /// </summary>
    private void SelectPiece(int boardRow, int boardCol, Rectangle square)
    {
        var pieceMoves = _gameViewModel.GetMovesForPiece(boardRow, boardCol);

        if (pieceMoves.Any())
        {
            ClearSelection();

            _selectedSquare = square;
            _selectedBoardRow = boardRow;
            _selectedBoardCol = boardCol;

            square.Stroke = new SolidColorBrush(Colors.Yellow);
            square.StrokeThickness = 3;

            HighlightDestinations(pieceMoves);
        }
    }

    /// <summary>
    /// Highlights valid move destinations for the selected piece
    /// </summary>
    private void HighlightDestinations(List<Move> moves)
    {
        ClearHighlightedMoves();

        foreach (var move in moves)
        {
            if (move.IsCapture)
            {
                int uiRow = _gameViewModel.BoardToUIRow(move.ToRow);
                int uiCol = _gameViewModel.BoardToUICol(move.ToCol);

                Rectangle targetSquare = _squares[uiRow, uiCol];
                targetSquare.Stroke = new SolidColorBrush(Colors.LimeGreen);
                targetSquare.StrokeThickness = 3;
                _highlightedMoves.Add(targetSquare);
            }
            else
            {
                int uiRow = _gameViewModel.BoardToUIRow(move.ToRow);
                int uiCol = _gameViewModel.BoardToUICol(move.ToCol);

                Rectangle targetSquare = _squares[uiRow, uiCol];
                targetSquare.Stroke = new SolidColorBrush(Colors.LimeGreen);
                targetSquare.StrokeThickness = 3;
                _highlightedMoves.Add(targetSquare);
            }
        }
    }

    /// <summary>
    /// Clears all highlighted moves from the board
    /// </summary>
    private void ClearHighlightedMoves()
    {
        foreach (var square in _highlightedMoves)
        {
            square.Stroke = null;
            square.StrokeThickness = 0;
        }
        _highlightedMoves.Clear();
    }

    /// <summary>
    /// Clears the current piece selection and highlighted moves
    /// </summary>
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

    /// <summary>
    /// Requests and displays a hint from ChatGPT
    /// </summary>
    private async Task GetHint()
    {
        if (_gameViewModel.IsPlayerTurn)
        {
            _hintButton.IsEnabled = false;
            _hintText.Text = "Getting hint from ChatGPT...";
            await _gameViewModel.GetHintFromChatGpt();
        }
        else
        {
            _hintText.Text = "You can only get hints during your turn.";
        }
    }
}
