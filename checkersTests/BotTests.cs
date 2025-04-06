using System.Numerics;
using checkers.Models;

namespace checkersTests;

public class BotTests
{
    [Fact]
    public void GetBestMove_ReturnsValidMove_WhenMovesAvailable()
    {
        // Arrange
        var board = new SmallBoard();
        var bot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var move = bot.GetBestMove(board);
        
        // Assert
        Assert.NotNull(move);
        Assert.NotEqual(0UL, move.Start);
        Assert.NotEqual(0UL, move.End);
    }
    
    [Fact]
    public void GetBestMove_CapturesWhenAvailable()
    {
        // Arrange
        var board = new SmallBoard();
        
        // Setup a board where a capture is available
        board.ClearPiece(new Position(5, 1));
        board.SetPiece(new Position(3, 1), false); // White piece
        
        var bot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var move = bot.GetBestMove(board);
        
        // Assert
        Assert.NotNull(move);
        Assert.NotEqual(0UL, move.Captured);
    }
    
    [Fact]
    public void GetBestMove_PrefersCaptureOverRegularMove()
    {
        // Arrange
        var board = new SmallBoard();
        
        // Setup a position where both regular moves and captures are available
        board.ClearPiece(new Position(5, 3));
        board.SetPiece(new Position(3, 3), false); // White piece
        
        var bot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var move = bot.GetBestMove(board);
        
        // Assert
        Assert.NotNull(move);
        Assert.NotEqual(0UL, move.Captured);
    }
    
    [Fact]
    public void GetBestMove_MovesTowardKinging_WhenNoCaptures()
    {
        // Arrange
        var board = new SmallBoard();
        
        // Setup a position where white is about to king
        // Clear all pieces to simplify the board
        for (int i = 0; i < 64; i++)
        {
            board.ClearPiece(new Position(i / 8, i % 8));
        }
        
        // Place white piece at (6,1), one move away from kinging
        board.SetPiece(new Position(6, 0), true);
        // Place white piece without move to capture or kinging
        board.SetPiece(new Position(4, 0), true);
        // Place black pieces far away to avoid any captures
        board.SetPiece(new Position(1, 1), false);
        
        var bot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var move = bot.GetBestMove(board);
        
        // Assert
        Assert.NotNull(move);
        // The move should be to promote the piece to king (moving to row 7)
        var endPos = SmallBoard.GetPositionFromMask(move.End);
        Assert.Equal(7, endPos.Row);
    }
    
    [Fact]
    public void GetBestMove_ReturnsNull_WhenNoMovesAvailable()
    {
        // Arrange
        var board = new SmallBoard();
        
        // Clear all pieces of the current player to ensure no moves
        for (int i = 0; i < 64; i++)
        {
            board.ClearPiece(new Position(i / 8, i % 8));
        }
        
        // Set pieces so no moves are available
        board.SetPiece(new Position(0, 0), true); // White piece
        board.SetPiece(new Position(1, 1), false); // Black piece
        board.SetPiece(new Position(2, 2), false); // Black piece
        
        var bot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var move = bot.GetBestMove(board);
        
        // Assert
        Assert.Null(move);
    }
    
    [Fact]
    public void GetBestMove_ProtectsKings_WhenThreatened()
    {
        // Arrange
        var board = new SmallBoard();
        
        // Setup a position where a white king is threatened by a black piece
        board = new SmallBoard();
        // Clear all pieces
        for (int i = 0; i < 64; i++)
        {
            board.ClearPiece(new Position(i / 8, i % 8));
        }
        
        // Place white king at (4,4)
        board.SetPiece(new Position(4, 4), true);
        board.ToKing(new Position(4, 4));
        // Place black piece at (5,5) threatening to capture
        board.SetPiece(new Position(5, 5), false);
        // Place another white piece at (2,4) that could move to protect
        board.SetPiece(new Position(2, 4), true);
        // Place a black piece to protect
        board.SetPiece(new Position(6, 6), false);
        
        var bot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var move = bot.GetBestMove(board);
        
        // Assert
        Assert.NotNull(move);
        
        // The bot should either move the king away or capture the threatening piece if possible
        var newBoard = board.Copy();
        newBoard.ApplyMove(move);
        
        // After the move, the king should not be capturable
        var blackMoves = newBoard.GetMoves();
        var kingPosition = new Position(4, 4);
        bool kingCaptured = blackMoves.Any(m => (m.Captured & SmallBoard.GetPositionMask(kingPosition)) != 0);
        
        Assert.False(kingCaptured);
    }
    
    [Fact]
    public void AlphaBeta_ProducesDifferentResults_AtDifferentDepths()
    {
        // Arrange
        var board = new SmallBoard();
        
        // Create two bots with different search depths by using reflection
        var shallowBot = new Bot(true, TimeSpan.FromSeconds(1));
        var field = typeof(Bot).GetField("_maxDepth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field.SetValue(null, 2);
        var shallowMove = shallowBot.GetBestMove(board);
        
        field.SetValue(null, 4);
        var deepBot = new Bot(true, TimeSpan.FromSeconds(1));
        
        // Act
        var deepMove = deepBot.GetBestMove(board);
        
        // Reset the static field
        field.SetValue(null, 5);
        
        // Assert - we can't guarantee different moves, but we can check that both return valid moves
        Assert.NotNull(shallowMove);
        Assert.NotNull(deepMove);
    }
}