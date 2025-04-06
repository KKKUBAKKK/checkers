using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using checkers.Models;

namespace checkers.controllers;

public class GameController
{
    private SmallBoard _board;
    private Bot _bot;
    private List<SmallMove> _availableMoves;
    private readonly HttpClient _httpClient;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";
    private readonly string _openAiApiKey;
    private const int BoardSize = 8;

    public SmallBoard Board => _board;
    public bool IsPlayerTurn { get; private set; }
    public List<SmallMove> AvailableMoves => _availableMoves;

    public event EventHandler<SmallBoard> BoardUpdated;
    public event EventHandler<string> HintReceived;

    public GameController(string openAiApiKey)
    {
        _board = new SmallBoard();
        _bot = new Bot();
        _httpClient = new HttpClient();
        _openAiApiKey = openAiApiKey;
        IsPlayerTurn = true;
        
        // Initialize the available moves
        _availableMoves = _board.GetMoves() ?? new List<SmallMove>();
        
        // Log initial state
        Console.WriteLine($"Initial board state: {_board}");
        Console.WriteLine($"Initial available moves: {_availableMoves.Count}");
        foreach (var move in _availableMoves)
        {
            Console.WriteLine($"Move: From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
        }
    }

    // Convert UI to board row coordinates
    public int UIToBoardRow(int uiRow)
    {
        return BoardSize - 1 - uiRow;
    }

    // Convert UI to board column
    public int UIToBoardCol(int uiCol)
    {
        return uiCol;
    }

    // Convert board to UI row
    public int BoardToUIRow(int boardRow)
    {
        return BoardSize - 1 - boardRow;
    }

    // Convert board to UI column
    public int BoardToUICol(int boardCol)
    {
        return boardCol;
    }

    // Get the piece at board coordinates
    public int GetPieceAt(int boardRow, int boardCol)
    {
        if (!IsPlayerTurn)
            return -1 * _board.GetPieceAt(boardRow, boardCol);
        
        return _board.GetPieceAt(boardRow, boardCol);
    }

    // Get available moves for a piece at the given board coordinates
    public List<SmallMove> GetMovesForPiece(int boardRow, int boardCol)
    {
        Console.WriteLine($"Checking moves for piece at board position ({boardRow},{boardCol})");
        
        // Find moves that start at this position
        var pieceMoves = _availableMoves
            .Where(m => m.FromRow == boardRow && m.FromCol == boardCol)
            .ToList();
        
        // Debug log all moves
        Console.WriteLine($"Found {pieceMoves.Count} moves for this piece");
        foreach (var move in pieceMoves)
        {
            Console.WriteLine($"Available move: From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
        }
        
        return pieceMoves;
    }

    private void UpdateAvailableMoves()
    {
        // Get all available moves - SmallBoard.GetMoves already returns only max captures if available
        _availableMoves = _board.GetMoves() ?? new List<SmallMove>();
        
        Console.WriteLine($"Updated available moves: {_availableMoves.Count}");
        foreach (var move in _availableMoves)
        {
            Console.WriteLine($"Move: From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
        }
    }

    public bool TryMakeMove(int fromBoardRow, int fromBoardCol, int toBoardRow, int toBoardCol)
    {
        Console.WriteLine($"TryMakeMove: From board ({fromBoardRow},{fromBoardCol}) to ({toBoardRow},{toBoardCol})");

        // Find the matching move in available moves
        var selectedMove = _availableMoves.FirstOrDefault(
            m => m.FromRow == fromBoardRow && m.FromCol == fromBoardCol && 
                 m.ToRow == toBoardRow && m.ToCol == toBoardCol);

        if (selectedMove == null)
        {
            Console.WriteLine("No matching move found in available moves");
            return false;
        }

        Console.WriteLine($"Selected move: From ({selectedMove.FromRow},{selectedMove.FromCol}) to ({selectedMove.ToRow},{selectedMove.ToCol}) - Capture: {selectedMove.IsCapture}");
        Console.WriteLine(_board);

        // Execute the move - this also changes whose turn it is
        _board = selectedMove.Execute(_board);
        Console.WriteLine(_board);

        // Toggle IsPlayerTurn since the board has switched turns
        IsPlayerTurn = !IsPlayerTurn;
        
        // Update available moves for the next player
        UpdateAvailableMoves();
        
        // Notify that the board has been updated
        BoardUpdated?.Invoke(this, _board);
        
        // If it's bot's turn, make a move after a short delay
        if (!IsPlayerTurn)
        {
            Task.Run(async () => {
                await Task.Delay(500); // Small delay for better UX
                MakeBotMove();
            });
        }
        
        return true;
    }

    private void MakeBotMove()
    {
        if (_availableMoves.Count == 0)
        {
            Console.WriteLine("Bot has no available moves");
            return;
        }

        // Using the GetBestMove method that only takes board as input
        var botMove = _bot.GetBestMove(_board);
        
        if (botMove != null)
        {
            Console.WriteLine($"Bot move: From ({botMove.FromRow},{botMove.FromCol}) to ({botMove.ToRow},{botMove.ToCol}) - Capture: {botMove.IsCapture}");
            
            // Execute the move - this also changes whose turn it is
            _board = botMove.Execute(_board);
            
            // Toggle IsPlayerTurn since the board has switched turns
            IsPlayerTurn = !IsPlayerTurn;
            
            // Update available moves for the player
            UpdateAvailableMoves();
            
            // Notify that the board has been updated
            BoardUpdated?.Invoke(this, _board);
        }
        else
        {
            Console.WriteLine("Bot GetBestMove returned null");
        }
    }

    public async Task<string> GetHintFromChatGpt()
    {
        try
        {
            // Convert current board state to a string representation
            string boardState = _board.ToString();
            
            // Create move descriptions for available moves
            var moveDescriptions = _availableMoves.Select(m => {
                string desc = $"From ({BoardToUIRow(m.FromRow)},{BoardToUICol(m.FromCol)}) to ({BoardToUIRow(m.ToRow)},{BoardToUICol(m.ToCol)})";
                if (m.IsCapture)
                {
                    desc += " - CAPTURE";
                }
                return desc;
            }).ToList();
            
            string availableMovesText = string.Join("\n", moveDescriptions);

            // Prepare the request to OpenAI
            var request = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a checkers expert assistant. Provide a helpful hint for the next move without being too obvious about the best move." },
                    new { role = "user", content = $"I'm playing checkers and need a hint. Here's my current board state:\n\n{boardState}\n\nThese are my available moves:\n{availableMovesText}\n\nGive me a short, helpful hint about what I should consider when making my next move. If you can suggest a specific move, describe the starting and ending position." }
                },
                max_tokens = 150
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            // Add API key to headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var response = await _httpClient.PostAsync(OpenAiApiUrl, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            // Parse the response
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

    public void Reset()
    {
        _board = new SmallBoard();
        IsPlayerTurn = true;
        
        UpdateAvailableMoves();
        BoardUpdated?.Invoke(this, _board);
    }
}
    