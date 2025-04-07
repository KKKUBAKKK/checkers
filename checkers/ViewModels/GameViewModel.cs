using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using checkers.Models;

namespace checkers.ViewModels;

/// <summary>
/// ViewModel for managing the checkers game state, logic, and interactions
/// </summary>
public class GameViewModel
{
    private Board _board;
    private Bot _bot;
    private List<Move> _availableMoves;
    private readonly HttpClient _httpClient;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";
    private readonly string _openAiApiKey;
    private const int BoardSize = 8;

    /// <summary>
    /// Current game board instance
    /// </summary>
    public Board Board => _board;

    /// <summary>
    /// Indicates whether it's currently the player's turn
    /// </summary>
    public bool IsPlayerTurn { get; private set; }

    /// <summary>
    /// List of currently available legal moves
    /// </summary>
    public List<Move> AvailableMoves => _availableMoves;

    /// <summary>
    /// Event triggered when the board state is updated
    /// </summary>
    public event EventHandler<Board> BoardUpdated;

    /// <summary>
    /// Event triggered when a hint is received from ChatGPT
    /// </summary>
    public event EventHandler<string> HintReceived;

    /// <summary>
    /// Initializes a new game with the specified OpenAI API key
    /// </summary>
    public GameViewModel(string openAiApiKey)
    {
        _board = new Board();
        _bot = new Bot();
        _httpClient = new HttpClient();
        _openAiApiKey = openAiApiKey;
        IsPlayerTurn = true;

        _availableMoves = _board.GetMoves() ?? new List<Move>();

        Console.WriteLine($"Initial board state: {_board}");
        Console.WriteLine($"Initial available moves: {_availableMoves.Count}");
        foreach (var move in _availableMoves)
        {
            Console.WriteLine(
                $"Move: From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}"
            );
        }
    }

    /// <summary>
    /// Gets or sets the AI opponent's thinking time
    /// </summary>
    public TimeSpan BotThinkingTime
    {
        get => _bot.ThinkingTime;
        set
        {
            _bot = new Bot(_bot.IsWhite, value);
            Console.WriteLine($"Bot thinking time set to {value.TotalSeconds} seconds");
        }
    }

    /// <summary>
    /// Converts UI row coordinate to board row coordinate
    /// </summary>
    public int UIToBoardRow(int uiRow)
    {
        return BoardSize - 1 - uiRow;
    }

    /// <summary>
    /// Converts UI column coordinate to board column coordinate
    /// </summary>
    public int UIToBoardCol(int uiCol)
    {
        return uiCol;
    }

    /// <summary>
    /// Converts board row coordinate to UI row coordinate
    /// </summary>
    public int BoardToUIRow(int boardRow)
    {
        return BoardSize - 1 - boardRow;
    }

    /// <summary>
    /// Converts board row coordinate to GPT notation
    /// </summary>
    public string BoardToGptRow(int boardRow)
    {
        return (boardRow + 1).ToString();
    }

    /// <summary>
    /// Converts board column coordinate to GPT notation
    /// </summary>
    public string BoardToGptCol(int boardCol)
    {
        return ((char)('A' + boardCol)).ToString();
    }

    /// <summary>
    /// Converts board column coordinate to UI column coordinate
    /// </summary>
    public int BoardToUICol(int boardCol)
    {
        return boardCol;
    }

    /// <summary>
    /// Gets the piece value at the specified board coordinates
    /// </summary>
    public int GetPieceAt(int boardRow, int boardCol)
    {
        if (!IsPlayerTurn)
            return -1 * _board.GetPieceAt(boardRow, boardCol);

        return _board.GetPieceAt(boardRow, boardCol);
    }

    /// <summary>
    /// Returns available moves for a piece at the specified board coordinates
    /// </summary>
    public List<Move> GetMovesForPiece(int boardRow, int boardCol)
    {
        var pieceMoves = _availableMoves
            .Where(m => m.FromRow == boardRow && m.FromCol == boardCol)
            .ToList();

        return pieceMoves;
    }

    /// <summary>
    /// Updates the list of available legal moves for the current turn
    /// </summary>
    private void UpdateAvailableMoves()
    {
        // Get all available moves - Board.GetMoves already returns only max captures if available
        _availableMoves = _board.GetMoves() ?? new List<Move>();
    }

    /// <summary>
    /// Attempts to make a move from the specified coordinates
    /// </summary>
    public bool TryMakeMove(int fromBoardRow, int fromBoardCol, int toBoardRow, int toBoardCol)
    {
        var selectedMove = _availableMoves.FirstOrDefault(m =>
            m.FromRow == fromBoardRow
            && m.FromCol == fromBoardCol
            && m.ToRow == toBoardRow
            && m.ToCol == toBoardCol
        );

        if (selectedMove == null)
        {
            return false;
        }

        _board = selectedMove.Execute(_board);

        IsPlayerTurn = !IsPlayerTurn;

        UpdateAvailableMoves();

        BoardUpdated?.Invoke(this, _board);

        if (!IsPlayerTurn)
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                MakeBotMove();
            });
        }

        return true;
    }

    /// <summary>
    /// Executes the AI opponent's move
    /// </summary>
    private void MakeBotMove()
    {
        if (_availableMoves.Count == 0)
        {
            return;
        }

        var botMove = _bot.GetBestMove(_board);

        if (botMove != null)
        {
            _board = botMove.Execute(_board);

            IsPlayerTurn = !IsPlayerTurn;

            UpdateAvailableMoves();

            BoardUpdated?.Invoke(this, _board);
        }
    }

    /// <summary>
    /// Requests and retrieves a strategic hint from ChatGPT
    /// </summary>
    public async Task<string> GetHintFromChatGpt()
    {
        try
        {
            string boardState = _board.ToString();

            var moveDescriptions = _availableMoves
                .Select(m =>
                {
                    string desc =
                        $"From ({BoardToGptRow(m.FromRow)},{BoardToGptCol(m.FromCol)}) to ({BoardToGptRow(m.ToRow)},{BoardToGptCol(m.ToCol)})";
                    if (m.IsCapture)
                    {
                        desc += " - CAPTURE";
                    }
                    return desc;
                })
                .ToList();

            string availableMovesText = string.Join("\n", moveDescriptions);

            var request = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are an english draughts expert assistant. Provide concise, actionable hints. Board coordinates are (row,column) starting from bottom-left corner (1,A) where white pieces begin. In the board representation, small w or b are regular pieces, and W or B are kings, which can move just like regular ones, but both forwards and backwards. Limit your response to 2-3 sentences.",
                    },
                    new
                    {
                        role = "user",
                        content = $"I'm playing as white and need a hint for my next move. Current board:\n\n{boardState}\n\nAvailable moves:\n{availableMovesText}\n\nWhat's my best move? If suggesting a specific move, use UI coordinates shown in the available moves list.",
                    },
                },
                max_tokens = 150,
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var response = await _httpClient.PostAsync(OpenAiApiUrl, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"API Response: {responseJson}");

            using JsonDocument doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            var message = choices[0].GetProperty("message");
            var hintText = message.GetProperty("content").GetString();

            HintReceived?.Invoke(this, hintText);
            return hintText;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error getting hint: {ex.Message}";
            HintReceived?.Invoke(this, errorMessage);
            return errorMessage;
        }
    }

    /// <summary>
    /// Resets the game to its initial state
    /// </summary>
    public void Reset()
    {
        _board = new Board();
        IsPlayerTurn = true;

        UpdateAvailableMoves();
        BoardUpdated?.Invoke(this, _board);
    }
}
