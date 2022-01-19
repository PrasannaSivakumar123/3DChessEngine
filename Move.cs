using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public string[,] board;
    public (int x, int y) start;
    public (int x, int y) end;
    public bool checkmate = false;
    public bool stalemate = false;
    public bool rookMoved = false;
    public bool kingMoved = false;
    public bool castle = false;
}
