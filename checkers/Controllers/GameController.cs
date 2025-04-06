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
    private SmallMove _lastJumpMove;
    private bool _isMultiJumpInProgress;
    private readonly HttpClient _httpClient;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";
    private readonly string _openAiApiKey;
    private const int BoardSize = 8;

    public SmallBoard Board => _board;
    public bool IsPlayerTurn { get; private set; }
    public bool IsMultiJumpInProgress => _isMultiJumpInProgress;
    public List<SmallMove> AvailableMoves => _availableMoves;
    public SmallMove LastJumpMove => _lastJumpMove;

    public event EventHandler<SmallBoard> BoardUpdated;
    public event EventHandler<string> HintReceived;

    public GameController(string openAiApiKey)
    {
        _board = new SmallBoard();
        _bot = new Bot();
        _httpClient = new HttpClient();
        _openAiApiKey = openAiApiKey;
        IsPlayerTurn = true;
        _isMultiJumpInProgress = false;
        
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

    // Convert UI row to board row
    public int UIToBoardRow(int uiRow)
    {
        return BoardSize - 1 - uiRow;
    }

    // Convert UI column to board column 
    public int UIToBoardCol(int uiCol)
    {
        return uiCol;
    }

    // Convert board row to UI row
    public int BoardToUIRow(int boardRow)
    {
        return BoardSize - 1 - boardRow;
    }

    // Convert board column to UI column
    public int BoardToUICol(int boardCol)
    {
        return boardCol;
    }

    // Get the piece at board coordinates
    public int GetPieceAt(int boardRow, int boardCol)
    {
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
        
        // Debug log all moves to see what's available
        Console.WriteLine($"Found {pieceMoves.Count} moves for this piece");
        foreach (var move in pieceMoves)
        {
            Console.WriteLine($"Available move: From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
        }
        
        return pieceMoves;
    }

    private void UpdateAvailableMoves()
    {
        if (_isMultiJumpInProgress && _lastJumpMove != null)
        {
            // During multi-jump, only get moves from the last position
            var allMoves = _board.GetMoves() ?? new List<SmallMove>();
            _availableMoves = allMoves
                .Where(m => m.FromRow == _lastJumpMove.ToRow && 
                            m.FromCol == _lastJumpMove.ToCol && 
                            m.IsCapture)
                .ToList();
            
            // If no more jumps available, end the multi-jump
            if (_availableMoves.Count == 0)
            {
                _isMultiJumpInProgress = false;
                _lastJumpMove = null;
                SwitchTurn();
            }
        }
        else
        {
            // Get all available moves
            _availableMoves = _board.GetMoves() ?? new List<SmallMove>();
            
            Console.WriteLine($"Updated available moves: {_availableMoves.Count}");
            foreach (var move in _availableMoves)
            {
                Console.WriteLine($"Move: From ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol}) - Capture: {move.IsCapture}");
            }
        }
    }

    public bool TryMakeMove(int fromBoardRow, int fromBoardCol, int toBoardRow, int toBoardCol)
    {
        Console.WriteLine($"TryMakeMove: From board ({fromBoardRow},{fromBoardCol}) to ({toBoardRow},{toBoardCol})");

        // If multi-jump in progress, validate starting position
        if (_isMultiJumpInProgress && _lastJumpMove != null)
        {
            if (fromBoardRow != _lastJumpMove.ToRow || fromBoardCol != _lastJumpMove.ToCol)
            {
                Console.WriteLine("Multi-jump validation failed: must start from last jump position");
                return false; // Must continue from last jump position
            }
        }

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

        // Execute the move
        _board = selectedMove.Execute(_board);

        // Check if it's a capture move
        if (selectedMove.IsCapture)
        {
            _lastJumpMove = selectedMove;
            
            // Check if more jumps are possible from this position
            var allMoves = _board.GetMoves() ?? new List<SmallMove>();
            var furtherCaptures = allMoves
                .Where(m => m.FromRow == toBoardRow && 
                            m.FromCol == toBoardCol && 
                            m.IsCapture)
                .ToList();
            
            Console.WriteLine($"Further captures possible: {furtherCaptures.Count}");
            
            if (furtherCaptures.Any())
            {
                _isMultiJumpInProgress = true;
                _availableMoves = furtherCaptures;
                BoardUpdated?.Invoke(this, _board);
                return true;
            }
        }

        // No further jumps or not a capture, switch turn
        _isMultiJumpInProgress = false;
        _lastJumpMove = null;
        SwitchTurn();
        return true;
    }

    private void SwitchTurn()
    {
        IsPlayerTurn = !IsPlayerTurn;
        
        UpdateAvailableMoves();
        BoardUpdated?.Invoke(this, _board);

        // If it's bot's turn, make a move
        if (!IsPlayerTurn)
        {
            Task.Run(async () => {
                await Task.Delay(500); // Small delay for better UX
                MakeBotMove();
            });
        }
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
            
            _board = botMove.Execute(_board);
            
            // Check if it's a capture and more captures are available
            if (botMove.IsCapture)
            {
                var allMoves = _board.GetMoves() ?? new List<SmallMove>();
                var furtherCaptures = allMoves
                    .Where(m => m.FromRow == botMove.ToRow && 
                                m.FromCol == botMove.ToCol && 
                                m.IsCapture)
                    .ToList();
                
                Console.WriteLine($"Bot further captures possible: {furtherCaptures.Count}");
                
                if (furtherCaptures.Any())
                {
                    // Continue capturing
                    _lastJumpMove = botMove;
                    _availableMoves = furtherCaptures;
                    BoardUpdated?.Invoke(this, _board);
                    
                    // Recursively make more bot moves until no more captures
                    Task.Run(async () => {
                        await Task.Delay(300); // Small delay between jumps
                        MakeBotMove();
                    });
                    return;
                }
            }

            // No further captures, switch turn
            IsPlayerTurn = true;
            _isMultiJumpInProgress = false;
            _lastJumpMove = null;
            
            UpdateAvailableMoves();
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
        _isMultiJumpInProgress = false;
        _lastJumpMove = null;
        IsPlayerTurn = true;
        
        UpdateAvailableMoves();
        BoardUpdated?.Invoke(this, _board);
    }
}
    