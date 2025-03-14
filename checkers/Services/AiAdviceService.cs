using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using checkers.Models;

namespace checkers.Services
{
    public class AiAdviceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;
        
        public AiAdviceService(string apiKey, string apiEndpoint = "https://api.openai.com/v1/chat/completions")
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _apiEndpoint = apiEndpoint;
            
            // Set up headers
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        
        public async Task<string> GetBestMoveAdvice(CheckersBoard board)
        {
            try
            {
                // Create a string representation of the board
                string boardState = SerializeBoardState(board);
                
                // Create the prompt for the AI
                string prompt = $@"
                I'm playing a game of checkers. The board state is:
                {boardState}
                
                I'm playing as White. What's the best move I should make? 
                Please respond with the coordinates from and to, like 'Move from (2,1) to (3,2)'.
                ";
                
                // Create request payload
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a checkers expert. Analyze the board and suggest the best move." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3,
                    max_tokens = 150
                };
                
                // Serialize request
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");
                
                // Send request
                var response = await _httpClient.PostAsync(_apiEndpoint, content);
                response.EnsureSuccessStatusCode();
                
                // Parse response
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);
                
                // Extract the AI's answer
                var aiResponse = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                return aiResponse;
            }
            catch (Exception ex)
            {
                return $"Error getting AI advice: {ex.Message}";
            }
        }
        
        private string SerializeBoardState(CheckersBoard board)
        {
            StringBuilder sb = new StringBuilder();
            
            for (int row = 0; row < CheckersBoard.BoardSize; row++)
            {
                for (int col = 0; col < CheckersBoard.BoardSize; col++)
                {
                    var piece = board.GetPieceAt(row, col);
                    
                    if (piece.Type == PieceType.None)
                        sb.Append(".");
                    else if (piece.Type == PieceType.White)
                        sb.Append(piece.Rank == PieceRank.King ? "R" : "r");
                    else if (piece.Type == PieceType.Black)
                        sb.Append(piece.Rank == PieceRank.King ? "B" : "b");
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        // Method to parse AI response into a move
        public Move ParseMoveFromAiResponse(string aiResponse, CheckersBoard board)
        {
            try
            {
                // Basic regex to extract coordinates
                // This is a simple implementation - you'll want to make it more robust
                var match = System.Text.RegularExpressions.Regex.Match(
                    aiResponse, 
                    @"from\s*\((\d+)\s*,\s*(\d+)\)\s*to\s*\((\d+)\s*,\s*(\d+)\)"
                );
                
                if (match.Success)
                {
                    int fromRow = int.Parse(match.Groups[1].Value);
                    int fromCol = int.Parse(match.Groups[2].Value);
                    int toRow = int.Parse(match.Groups[3].Value);
                    int toCol = int.Parse(match.Groups[4].Value);
                    
                    return new Move(
                        new Position(fromRow, fromCol),
                        new Position(toRow, toCol)
                    );
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}