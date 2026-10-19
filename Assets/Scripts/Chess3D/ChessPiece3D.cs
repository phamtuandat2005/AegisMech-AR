using UnityEngine;
using MechChess.ProceduralMech;

namespace MechChess.Chess3D
{
    /// <summary>
    /// Represents an active 3D Mech piece on the chessboard.
    /// </summary>
    public class ChessPiece3D : MonoBehaviour
    {
        public PieceType pieceType;
        public PieceColor pieceColor;
        public int file; // 0..7 (a..h)
        public int rank; // 0..7 (1..8)
        public bool hasMoved = false;

        public MechPieceAnimator Animator { get; private set; }

        public void Setup(PieceType type, PieceColor color, int initFile, int initRank)
        {
            pieceType = type;
            pieceColor = color;
            file = initFile;
            rank = initRank;
            Animator = GetComponent<MechPieceAnimator>();
        }

        public string GetAlgebraicPosition()
        {
            char colChar = (char)('A' + file);
            int rowNum = rank + 1;
            return $"{colChar}{rowNum}";
        }

        public override string ToString()
        {
            return $"{pieceColor} {pieceType} at {GetAlgebraicPosition()}";
        }
    }
}
