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
    
}