using System;

namespace checkers.Models;

public class SmallMove
{
    public UInt64 Start;
    public UInt64 End;
    public UInt64 Captured;
    
    public SmallMove(UInt64 start, UInt64 end, UInt64 captured)
    {
        Start = start;
        End = end;
        Captured = captured;
    }

    public SmallMove Copy()
    {
        return new SmallMove(Start, End, Captured);
    }
    
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

    public bool IsCapture
    {
        get
        {
            return Captured != 0;
        }
    }
    
    public int CaptureCount
    {
        get
        {
            return CountCaptures();
        }
    }

    public int FromRow
    {
        get
        {
            return SmallBoard.GetPositionFromMask(Start).Row;
        }
    }

    public int FromCol
    {
        get
        {
            return SmallBoard.GetPositionFromMask(Start).Col;
        }
    }

    public int ToRow
    {
        get
        {
            return SmallBoard.GetPositionFromMask(End).Row;
        }
    }
    public int ToCol
    {
        get
        {
            return SmallBoard.GetPositionFromMask(End).Col;
        }
    }

    public SmallBoard Execute(SmallBoard board)
    {
        var newBoard = board.Copy();
        newBoard.ApplyMove(this);
        return newBoard;
    }
}