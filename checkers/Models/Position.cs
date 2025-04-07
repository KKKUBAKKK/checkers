namespace checkers.Models;

/// <summary>
/// Represents a position on the checkers board using row and column coordinates
/// </summary>
public class Position
{
    /// <summary>
    /// Gets the row coordinate (0-7)
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Gets the column coordinate (0-7)
    /// </summary>
    public int Col { get; }

    /// <summary>
    /// Initializes a new position with specified row and column coordinates
    /// </summary>
    public Position(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
