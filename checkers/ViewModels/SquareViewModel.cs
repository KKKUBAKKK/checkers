using ReactiveUI;

namespace checkers.ViewModels
{
    public class SquareViewModel : ViewModelBase
    {
        private int _row;
        private int _column;
        private string _color;
        private bool _hasPiece;
        private string _pieceColor;
        private bool _isKing;
        private bool _isSelected;
        private bool _isHighlighted;
        private bool _isHintSource;
        private bool _isHintTarget;

        public bool IsHintSource
        {
            get => _isHintSource;
            set => this.RaiseAndSetIfChanged(ref _isHintSource, value);
        }

        public bool IsHintTarget
        {
            get => _isHintTarget;
            set => this.RaiseAndSetIfChanged(ref _isHintTarget, value);
        }

        public int Row
        {
            get => _row;
            set => this.RaiseAndSetIfChanged(ref _row, value);
        }

        public int Column
        {
            get => _column;
            set => this.RaiseAndSetIfChanged(ref _column, value);
        }

        public string Color
        {
            get => _color;
            set => this.RaiseAndSetIfChanged(ref _color, value);
        }

        public bool HasPiece
        {
            get => _hasPiece;
            set => this.RaiseAndSetIfChanged(ref _hasPiece, value);
        }

        public string PieceColor
        {
            get => _pieceColor;
            set => this.RaiseAndSetIfChanged(ref _pieceColor, value);
        }

        public bool IsKing
        {
            get => _isKing;
            set => this.RaiseAndSetIfChanged(ref _isKing, value);
        }
        
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
        
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => this.RaiseAndSetIfChanged(ref _isHighlighted, value);
        }
        
        // Calculate the display color, which might change when selected or highlighted
        public string DisplayColor
        {
            get
            {
                if (IsSelected)
                    return "#77AAFF"; // Light blue for selected squares
                else if (IsHighlighted)
                    return "#AAFFAA"; // Light green for highlighted valid moves
                else
                    return Color;
            }
        }
        
        // Helper property to determine if this square is a valid board position for a piece
        public bool IsValidPosition => (Row + Column) % 2 == 1; // Only dark squares are valid
        
        // Returns a string representation, useful for debugging
        public override string ToString()
        {
            return $"Square({Row},{Column}) - {(HasPiece ? PieceColor + (IsKing ? " King" : "") : "Empty")}";
        }
    }
}