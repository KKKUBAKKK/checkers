using System.Numerics;
using Xunit.Abstractions;

namespace checkersTests;
using checkers.Models;

public class BoardTests
{
    private readonly ITestOutputHelper _output;
    
    public BoardTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
   [Fact]
    public void InitialBoard_ShouldHaveCorrectSetup()
    {
        // Arrange & Act
        var board = new Board();
        _output.WriteLine(board.ToString());
        
        // Assert
        Assert.True(board.IsWhiteTurn);
        
        // Check initial piece counts
        Assert.Equal(12, BitOperations.PopCount(board.White));
        Assert.Equal(12, BitOperations.PopCount(board.Black));
        Assert.Equal(0, BitOperations.PopCount(board.Kings));
    }
    
    [Fact]
    public void GetMoves_InitialPosition_ShouldReturnCorrectMoves()
    {
        // Arrange
        var board = new Board();
        
        // Act
        var moves = board.GetMoves();
        
        // Assert
        Assert.Equal(7, moves.Count); // Initial white has 7 possible moves
    }
    
    [Fact]
    public void ApplyMove_ShouldChangePlayerTurn()
    {
        // Arrange
        var board = new Board();
        var moves = board.GetMoves();
        
        // Act
        board.ApplyMove(moves[0]);
        
        // Assert
        Assert.False(board.IsWhiteTurn);
    }
    
    [Fact]
    public void GetMoves_ShouldIncludeCaptures()
    {
        // Arrange
        var board = new Board();
        
        // Set up a position where white can capture
        board.ClearPiece(new Position(5, 7));
        board.SetPiece(new Position(3, 5)); // Black piece
        
        // Act
        var moves = board.GetMoves();
        
        // Assert
        // Should include a capture move where white jumps over black
        Assert.Contains(moves, m => m.Captured != 0);
    }
    
    [Fact]
    public void ApplyMove_WithCapture_ShouldRemoveCapturedPiece()
    {
        // Arrange
        var board = new Board();
        
        // Set up a position where white can capture
        board.ClearPiece(new Position(5, 7));
        board.SetPiece(new Position(3, 5)); // Black piece
        
        // Act
        var moves = board.GetMoves();
        var captureMove = moves.First(m => m.Captured != 0);
        board.ApplyMove(captureMove);
        
        // Assert
        Assert.Equal(11, BitOperations.PopCount(board.Black)); // One black piece should be captured
    }
    
    [Fact]
    public void Copy_ShouldCreateIndependentInstance()
    {
        // Arrange
        var originalBoard = new Board();
        
        // Act
        var copiedBoard = originalBoard.Copy();
        var moves = originalBoard.GetMoves();
        originalBoard.ApplyMove(moves[0]);
        
        // Assert
        Assert.True(copiedBoard.IsWhiteTurn);
        Assert.False(originalBoard.IsWhiteTurn);
        Assert.Equal(12, BitOperations.PopCount(copiedBoard.White));
    }
    
    [Fact]
    public void Evaluate_ShouldReturnPositiveScoreForWhiteAdvantage()
    {
        // Arrange
        var board = new Board();
        
        // Remove a black piece to give white an advantage
        board.ClearPiece(new Position(5, 1));
        
        // Act
        var score = board.Evaluate();
        
        // Assert
        Assert.True(score > 0);
    }
    
    [Fact]
    public void GetMoves_KingPiece_ShouldHaveMoreMoves()
    {
        // Arrange
        var board = new Board();
        
        // Set up a king in the middle of the board
        board = new Board();
        board.ClearPiece(new Position(3, 2));
        board.SetPiece(new Position(3, 2)); // White piece
        var kingPosition = Board.GetPositionMask(new Position(3, 2));
        board.Kings = kingPosition; // Make it a king (using reflection or internal field)
        
        // Act
        var moves = board.GetMoves();
        
        // Assert
        // A king in the middle would have more possible moves than a regular piece
        Assert.True(moves.Count >= 2);
    }
    
    [Fact]
    public void IsOver_EmptyBlackPieces_ShouldReturnTrue()
    {
        // Arrange
        var board = new Board();
        
        // Remove all black pieces
        for (int row = 5; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((row + col) % 2 == 0)
                {
                    board.ClearPiece(new Position(row, col));
                }
            }
        }
        
        // Act & Assert
        Assert.True(board.IfOver());
    }
    
    [Fact]
    public void GetMoves_WithMultipleCaptures_ShouldReturnCorrectMoves()
    {
        // Arrange
        var board = new Board();
        
        // Set up a position where white can capture multiple pieces
        board.ClearPiece(new Position(6, 6));
        board.ClearPiece(new Position(6, 4));
        board.ClearPiece(new Position(5, 7));
        board.SetPiece(new Position(3, 5), false); // Black piece
        
        // Act
        var moves = board.GetMoves();
        
        // Assert
        // Should include a capture move where white jumps over 2 black pieces
        Assert.Contains(moves, m => BitOperations.PopCount(m.Captured) == 2);
        Assert.Equal(moves.Count, 2);
    }
}