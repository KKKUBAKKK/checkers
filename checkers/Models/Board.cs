using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace checkers.Models;

public class Board
{
    private const UInt64 InitialWhite   = 0x000000000055AA55;
    private const UInt64 InitialBlack   = 0xAA55AA0000000000;
    private const UInt64 InitialKings   = 0x0000000000000000;
    
    private const UInt64 FirstRow       = 0x00000000000000FF;
    private const UInt64 SecondRow      = 0x000000000000FF00;
    private const UInt64 SecondLastRow  = 0x00FF000000000000;
    private const UInt64 LastRow        = 0xFF00000000000000;
    
    private const UInt64 FirstCol       = 0x0101010101010101;
    private const UInt64 SecondCol      = 0x0202020202020202;
    private const UInt64 SecondLastCol  = 0x4040404040404040;
    private const UInt64 LastCol        = 0x8080808080808080;
    
    private const int BoardSize = 64;
    private const int BoardWidth = 8;
    
    private const int UpLeft = 7;
    private const int UpRight = 9;
    private const int DownLeft = 9;
    private const int DownRight = 7;
    
    private bool _isWhiteTurn;
    
    private UInt64 _white;
    private UInt64 _black;
    private UInt64 _kings;
    
    public bool IsWhiteTurn => _isWhiteTurn;
    public UInt64 White
    {
        get => _white;
        set => _white = value;
    }
    public UInt64 Black
    {
        get => _black;
        set => _black = value;
    }
    public UInt64 Kings
    {
        get => _kings;
        set => _kings = value;
    }
    
    public Board()
    {
        _isWhiteTurn = true;
        _white = InitialWhite;
        _black = InitialBlack;
        _kings = InitialKings;
    }
    
    public static UInt64 GetPositionMask(Position pos)
    {
        return 1UL << pos.Row * BoardWidth + pos.Col;
    }
    
    public static Position GetPositionFromMask(UInt64 mask)
    {
        int index = BitOperations.TrailingZeroCount(mask);
        return new Position(index / BoardWidth, index % BoardWidth);
    }
    
    public bool IsEmpty(Position pos)
    {
        return (_white | _black & GetPositionMask(pos)) == 0UL;
    }

    public bool IsWhite(Position pos)
    {
        return (_white & GetPositionMask(pos)) != 0UL;
    }

    public bool IsBlack(Position pos)
    {
        return (_black & GetPositionMask(pos)) != 0UL;
    }
    
    public bool IsKing(Position pos)
    {
        return (_kings & GetPositionMask(pos)) != 0UL;
    }

    public void ClearPiece(Position pos)
    {
        UInt64 piece = GetPositionMask(pos);
        _white &= ~piece;
        _black &= ~piece;
        _kings &= ~piece;
    }
    
    public void SetPiece(Position pos, bool isWhite = false)
    {
        UInt64 piece = GetPositionMask(pos);
        
        if (isWhite)
            _white |= piece;
        else
            _black |= piece;
        
        if (IsKing(pos))
            _kings |= piece;
    }

    public Board Copy()
    {
        var board = new Board();
        board._isWhiteTurn = _isWhiteTurn;
        board._white = _white;
        board._black = _black;
        board._kings = _kings;
        return board;
    }

    public List<Move> GetMoves()
    {
        List<Move> moves = new List<Move>();
        Stack<Move> moveStack = new Stack<Move>();
        
        UInt64 player = _isWhiteTurn ? _white : _black;
        UInt64 opponent = _isWhiteTurn ? _black : _white;
        UInt64 all_pieces = _white | _black;

        // Find all the maximum captures
        for (int i = 0; i < BoardSize; i++)
        {
            if (moveStack.Count >= 12)
                break;
            
            UInt64 start = 1UL << i;
            if ((player & start) != 0)
            {
                moveStack.Push(new Move(start, start, 0UL));
            }
        }
        
        while (moveStack.Count > 0)
        {
            var m = moveStack.Pop();
            var stackSize = moveStack.Count;

            var p = player ^ m.Start;
            var o = opponent ^ m.Captured;
            var a = all_pieces ^ m.Start;
            
            if ((m.End & (FirstCol | SecondCol)) == 0) {
                if ((_isWhiteTurn || (m.Start & _kings) != 0) && (m.End << UpLeft & o) != 0 && ((m.End << (2 * UpLeft)) & ~a) != 0) {
                    Move t = m.Copy();
                    t.End = m.End << (2 * UpLeft);
                    t.Captured |= (m.End << UpLeft);
                    moveStack.Push(t);
                }
                if ((!_isWhiteTurn || (m.Start & _kings) != 0) &&  ((m.End >> DownLeft) & o) != 0 && ((m.End >> (2 * DownLeft)) & ~a) != 0)
                {
                    Move t = m.Copy();
                    t.End = m.End >> (2 * DownLeft);
                    t.Captured |= (m.End >> DownLeft);
                    moveStack.Push(t);
                }
            }
            if ((m.End & (SecondLastCol | LastCol)) == 0) {
                if ((_isWhiteTurn || (m.Start & _kings) != 0) && ((m.End << UpRight) & o) != 0 && ((m.End << (2 * UpRight)) & ~a) != 0) {
                    Move t = m.Copy();
                    t.End = m.End << (2 * UpRight);
                    t.Captured |= (m.End << UpRight);
                    moveStack.Push(t);
                }
                if ((!_isWhiteTurn || (m.Start & _kings) != 0) && ((m.End >> DownRight) & o) != 0 && ((m.End >> (2 * DownRight)) & ~a) != 0) {
                    Move t = m.Copy();
                    t.End = m.End >> (2 * DownRight);
                    t.Captured |= (m.End >> DownRight);
                    moveStack.Push(t);
                }
            }
            
            if (stackSize == moveStack.Count && m.Captured != 0UL)
            {
                moves.Add(m);
            }
        }
        
        if (moves.Any()) return moves;
        
        // Find regular moves
        for (int i = 0; i < BoardSize; i++)
        {
            UInt64 start = 1UL << i;
            if ((player & start) != 0)
            {
                if ((start & FirstCol) == 0)
                {
                    if ((_isWhiteTurn || (start & _kings) != 0) && (start << UpLeft & ~all_pieces) != 0)
                    {
                        moves.Add(new Move(start, start << UpLeft, 0UL));
                    }
                    if ((!_isWhiteTurn || (start & _kings) != 0) && (start >> DownLeft & ~all_pieces) != 0)
                    {
                        moves.Add(new Move(start, start >> DownLeft, 0UL));
                    }
                }
                if ((start & LastCol) == 0)
                {
                    if ((_isWhiteTurn || (start & _kings) != 0) && (start << UpRight & ~all_pieces) != 0)
                    {
                        moves.Add(new Move(start, start << UpRight, 0UL));
                    }
                    if ((!_isWhiteTurn || (start & _kings) != 0) && (start >> DownRight & ~all_pieces) != 0)
                    {
                        moves.Add(new Move(start, start >> DownRight, 0UL));
                    }
                }
            }
        }

        return moves;
    }

    public void ApplyMove(Move move)
    {
        if (_isWhiteTurn)
        {
            _white ^= move.Start | move.End;
            _black ^= move.Captured;
        }
        else
        {
            _black ^= move.Start | move.End;
            _white ^= move.Captured;
        }
        
        if ((move.Start & _kings) != 0 || 
            ((move.End & LastRow) != 0 && _isWhiteTurn) || 
            ((move.End & FirstRow) != 0 && !_isWhiteTurn))
        {
            _kings &= _white | _black;
            _kings |= move.End;
        }
        
        _isWhiteTurn = !_isWhiteTurn;
    }
    
    public bool IfOver()
    {
        if (_white == 0UL || _black == 0UL)
        {
            return true;
        }

        return false;
    }

    public int Evaluate()
{
    UInt64 player = _isWhiteTurn ? _white : _black;
    UInt64 opponent = _isWhiteTurn ? _black : _white;
    UInt64 playerKings = player & _kings;
    UInt64 opponentKings = opponent & _kings;
    UInt64 playerRegular = player & ~_kings;
    UInt64 opponentRegular = opponent & ~_kings;
    
    // Game termination conditions
    if (player == 0UL || GetMoves().Count == 0)
        return -1000; // Loss is worse than any position
    if (opponent == 0UL)
        return 1000;  // Win is better than any position
    
    int score = 0;
    
    // Material count (kings worth 3x regular pieces)
    int playerRegularCount = BitOperations.PopCount(playerRegular);
    int opponentRegularCount = BitOperations.PopCount(opponentRegular);
    int playerKingCount = BitOperations.PopCount(playerKings);
    int opponentKingCount = BitOperations.PopCount(opponentKings);
    
    score += playerRegularCount * 100;
    score -= opponentRegularCount * 100;
    score += playerKingCount * 300;
    score -= opponentKingCount * 300;
    
    // Advancement for regular pieces
    for (int i = 0; i < BoardSize; i++)
    {
        UInt64 pos = 1UL << i;
        int row = i / BoardWidth;
        
        if ((playerRegular & pos) != 0)
        {
            // Advancement bonus (increasing as pieces get closer to promotion)
            if (_isWhiteTurn) // White pieces move upward
                score += row * 5;
            else // Black pieces move downward
                score += (7 - row) * 5;
                
            // Extra bonus for pieces close to promotion
            if ((_isWhiteTurn && row >= 5) || (!_isWhiteTurn && row <= 2))
                score += 15;
        }
        
        if ((opponentRegular & pos) != 0)
        {
            if (_isWhiteTurn) // Black pieces move downward
                score -= (7 - row) * 5;
            else // White pieces move upward
                score -= row * 5;
                
            if ((_isWhiteTurn && row <= 2) || (!_isWhiteTurn && row >= 5))
                score -= 15;
        }
    }
    
    // Center control (middle squares are strategically valuable)
    UInt64 centerMask = 0x00003C3C3C3C0000UL; // Middle 4x4 area
    score += BitOperations.PopCount(player & centerMask) * 10;
    score -= BitOperations.PopCount(opponent & centerMask) * 10;
    
    // Edge penalty (pieces on edges have limited mobility)
    UInt64 edgeMask = FirstCol | LastCol;
    score -= BitOperations.PopCount(player & edgeMask) * 5;
    score += BitOperations.PopCount(opponent & edgeMask) * 5;
    
    // Mobility (number of available moves)
    int moveCount = GetMoves().Count;
    score += moveCount * 5;
    
    // Back row defense bonus
    UInt64 playerBackRowMask = _isWhiteTurn ? FirstRow : LastRow;
    score += BitOperations.PopCount(playerRegular & playerBackRowMask) * 10;
    
    // Material advantage factor (encourage trades when ahead, avoid when behind)
    int materialDifference = 
        playerRegularCount + 3 * playerKingCount - 
        opponentRegularCount - 3 * opponentKingCount;
    
    score += materialDifference * 5;
    
    return score;
}

    public void ToKing(Position pos)
    {
        var piece = GetPositionMask(pos);
        piece &= _white | _black;
        _kings |= piece;
    }
    
    public int GetPieceAt(int row, int col)
    {
        UInt64 mask = GetPositionMask(new Position(row, col));
        UInt64 player = _isWhiteTurn ? _white : _black;
        UInt64 opponent = _isWhiteTurn ? _black : _white;
        
        if ((player & mask & _kings) != 0)
            return 2;
        if ((player & mask) != 0)
            return 1;
        if ((opponent & mask & _kings) != 0)
            return -2;
        if ((opponent & mask) != 0)
            return -1;
        
        return 0;
    }
    
    public override string ToString()
    {
        // Create board representation
        char[] board = new char[BoardSize];
        for (int i = 0; i < BoardSize; i++)
        {
            var pos = new Position(i / BoardWidth, i % BoardWidth);
            if (IsWhite(pos))
                board[i] = IsKing(pos) ? 'W' : 'w';
            else if (IsBlack(pos))
                board[i] = IsKing(pos) ? 'B' : 'b';
            else
                board[i] = '.';
        }
    
        // Build formatted output
        StringBuilder sb = new StringBuilder();
    
        // Process rows in reverse (from 7 to 0)
        for (int row = BoardWidth - 1; row >= 0; row--)
        {
            // Add row number label (8 to 1)
            sb.Append($"{row + 1} ");
        
            // Add pieces for this row with spaces between them
            for (int col = 0; col < BoardWidth; col++)
            {
                sb.Append(board[row * BoardWidth + col]);
                sb.Append(' ');
            }
        
            sb.AppendLine();
        }
    
        // Add column labels at the bottom
        sb.Append("  A B C D E F G H");
    
        return sb.ToString();
    }
}