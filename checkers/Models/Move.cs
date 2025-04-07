using System;

namespace checkers.Models;

/// <summary>
/// Represents a move in the checkers game using bitboard representation
/// </summary>
public class Move
{
    /// <summary>
    /// Bitboard mask for the starting position
    /// </summary>
    public UInt64 Start;

    /// <summary>
    /// Bitboard mask for the ending position
    /// </summary>
    public UInt64 End;

    /// <summary>
    /// Bitboard mask for captured pieces
    /// </summary>
    public UInt64 Captured;

    /// <summary>
    /// Creates a new move with the specified start, end, and captured positions
    /// </summary>
    public Move(UInt64 start, UInt64 end, UInt64 captured)
    {
        Start = start;
        End = end;
        Captured = captured;
    }

    /// <summary>
    /// Creates a deep copy of the move
    /// </summary>
    public Move Copy()
    {
        return new Move(Start, End, Captured);
    }

    /// <summary>
    /// Counts the number of pieces captured in this move
    /// </summary>
    public int CountCaptures()
    {
        int count = 0;
        UInt64 temp = Start;
        while (temp != End)
        {
            if ((temp & Captured) != 0)
            {
                count++;
            }
            temp <<= 1;
        }
        return count;
    }

    /// <summary>
    /// Indicates whether this move captures any pieces
    /// </summary>
    public bool IsCapture
    {
        get { return Captured != 0; }
    }

    /// <summary>
    /// Gets the total number of pieces captured in this move
    /// </summary>
    public int CaptureCount
    {
        get { return CountCaptures(); }
    }

    /// <summary>
    /// Gets the starting row coordinate
    /// </summary>
    public int FromRow
    {
        get { return Board.GetPositionFromMask(Start).Row; }
    }

    /// <summary>
    /// Gets the starting column coordinate
    /// </summary>
    public int FromCol
    {
        get { return Board.GetPositionFromMask(Start).Col; }
    }

    /// <summary>
    /// Gets the destination row coordinate
    /// </summary>
    public int ToRow
    {
        get { return Board.GetPositionFromMask(End).Row; }
    }

    /// <summary>
    /// Gets the destination column coordinate
    /// </summary>
    public int ToCol
    {
        get { return Board.GetPositionFromMask(End).Col; }
    }

    /// <summary>
    /// Executes the move on a given board and returns the new board state
    /// </summary>
    public Board Execute(Board board)
    {
        var newBoard = board.Copy();
        newBoard.ApplyMove(this);
        return newBoard;
    }
}
