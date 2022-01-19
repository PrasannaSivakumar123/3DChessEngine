using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
    public bool whiteToMove = true;
    public string[,] grid = new string[8,8]{

        {"wR","wN","wB","wK","wQ","wB","wN","wR"},
        {"wP","wP","wP","wP","wP","wP","wP","wP"},
        {"--","--","--","--","--","--","--","--"},
        {"--","--","--","--","--","--","--","--"},
        {"--","--","--","--","--","--","--","--"},
        {"--","--","--","--","--","--","--","--"},
        {"bP","bP","bP","bP","bP","bP","bP","bP"},
        {"bR","bN","bB","bK","bQ","bB","bN","bR"}

    };
    (int x, int y) wKLocation = (3,0);
    (int x, int y) bKLocation = (3,7);
    List<((int x, int y) pin, (int x, int y) direction)> pins;
    bool inCheck;
    List<((int x, int y) end, (int x, int y) direction)> checks;

    CastleRequirements Castle = new CastleRequirements();

    public void MovePiece(Move move){
        string piece = grid[move.start.y,move.start.x];

        if (move.rookMoved){
            if (move.start == (0,0)){
                Castle.wR0Moved = true;
            }
            else if (move.start == (7,0)){
                Castle.wR7Moved = true;
            }
            else if (move.start == (0,7)){
                Castle.bR0Moved = true;
            }
            else if (move.start == (7,7)){
                Castle.bR7Moved = true;
            }
        }

        else if (move.kingMoved){
            if (piece[0] == 'w'){
                Castle.wKMoved = true;
            }
            else{
                Castle.bKMoved = true;
            }
        }

        grid[move.start.y,move.start.x] = "--";
        grid[move.end.y,move.end.x] = piece;

        if (piece == "wK"){
            wKLocation = move.end;
        }
        else if (piece == "bK"){
            bKLocation = move.end;
        }

        if (move.castle){
            if (move.end.x == 5){
                grid[move.start.y, 7] = "--";
                if (piece[0] == 'w'){
                    grid[move.end.y, 4] = "wR";
                }
                else{
                    grid[move.end.y, 4] = "bR";
                }
            }

            if (move.end.x == 1){
                grid[move.start.y, 0] = "--";
                if (piece[0] == 'w'){
                    grid[move.end.y, 2] = "wR";
                }
                else{
                    grid[move.end.y, 2] = "bR";
                }
            }
        }

        whiteToMove ^= true;

    }

    //returns a list of all legal moves that can be made in a gamestate
    public List<Move> GetAllValidMoves(){

        List<Move> validMoves = new List<Move>(); //stores all the legal moves 
        inCheck = false;
        checks = new List<((int x, int y) end, (int x, int y) direction)>();
        //^stores all the checks, 
        //includes the square of the piece checking and the direction of the check
        pins = new List<((int x, int y) pin, (int x, int y) direction)>();
        //^stores all the pins, 
        //includes the square of the piece pinning and the direction of the pin
        CheckPins(checks, pins, ref inCheck); //adds checks and pins to list
        (int x, int y) kingPos;

        if (whiteToMove){ //identifies who's move it is
            kingPos = wKLocation;
        }
        else{
            kingPos = bKLocation;
        }

        if (inCheck){
            if (checks.Count > 1){
                //if there is more than 1 check it means the king must move,
                //as a piece cannot block the check
                GetMovesKing(kingPos.x, kingPos.y, validMoves);
                //adds all king moves to the list of valid moves
                if (validMoves.Count == 0){
                    //no possible moves, so its checkmate
                    Move move = new Move();
                    move.checkmate = true;
                    validMoves.Add(move);
                }
                return validMoves;
            }
            else {
                List<Move> moves = GetAllMoves();
                //returns all the moves every white/black piece can make
                ((int x, int y) end, (int x, int y) direction) check = checks[0];
                string pieceCheck = grid[check.end.y,check.end.x]; //piece that is checking
                List<(int x, int y)> validSquares = new List<(int x, int y)>();
                //^contains all the possible squares that can be moved to to stop the check
                
                if (pieceCheck[1] == 'N'){
                    validSquares.Add(check.end);
                    //if a knight is checking, the check cannot be blocked 
                }

                else {
                    //loops through all the squares inbetween the checking piece and the king
                    //and adds these squares to valid squares list
                    //these are all squares where other pieces can go to block the check
                    for (int i = 1; i < 8; i++){
                        (int x, int y) square = 
                        (kingPos.x + i * check.direction.x, kingPos.y + i * check.direction.y);
                        if (square == check.end){
                            //reached the checking piece so no more squares left to block
                            break;
                        }
                        validSquares.Add(square);
                    }
                }
                //loops through every possible piece move and verifies if it ends on a valid square
                //if it does it is added to the valid moves list, as it blocks the check
                foreach (Move move in moves){
                    (int x, int y) end = move.end;
                    if (grid[move.start.y, move.start.x][1] != 'K'){ //if the piece is not a king
                        if (end == check.end){
                            validMoves.Add(move);
                            continue;
                        }
                        else if (validSquares.Contains(end)){
                            validMoves.Add(move);
                        }
                    }
                    else {
                        validMoves.Add(move);
                    }
                }
            }
        }
        else {
            //there are no checks so all moves are valid
            List<Move> allMoves = GetAllMoves();
            if (allMoves.Count == 0){
                //no moves possible means its a stalemate
                Move move = new Move();
                move.stalemate = true;
                allMoves.Add(move);
            }

            return allMoves;
        }

        if (validMoves.Count == 0){
            //king is in check and there is no possible moves means its a checkmate
            Move move = new Move();
            move.checkmate = true;
            validMoves.Add(move);
        }

        return validMoves;
    }

    void CheckPins(List<((int x, int y) end, (int x, int y) direction)> checks,
                   List<((int x, int y) pin, (int x, int y) direction)> pins, 
                   ref bool inCheck){

        int[,] directions = {{1,1},{-1,1},{1,-1},{-1,-1},{1,0},{0,-1},{-1,0},{0,1}};
        char turn;
        (int x, int y) start;
        if (whiteToMove){
            turn = 'w';
            start = wKLocation;
        }
        else{
            turn = 'b';
            start = bKLocation;
        }

        for (int j = 0; j < 8; j++){
            (int x, int y) direction = (directions[j,0], directions[j,1]);
            (int x, int y) pin = (9,9);
            for (int i = 1; i < 8; i++){
                (int x, int y) end = (direction.x * i + start.x, direction.y * i + start.y);
                if ((end.x < 0) || (end.x > 7) || (end.y < 0) || (end.y > 7)){
                    break;
                }

                if ((grid[end.y,end.x][0] == turn) && (grid[end.y,end.x][1] != 'K')){
                    if (pin == (9,9)){
                        pin = end;
                    }
                    else {
                        break;
                    }
                }

                else if ((grid[end.y,end.x][0] != turn) && (grid[end.y,end.x][0] != '-')){
                    char piece = grid[end.y,end.x][1];

                    if (((piece == 'K') && (i == 1)) ||
                        (piece == 'Q') || 
                        ((piece == 'R') && (j >= 4)) ||
                        ((piece == 'B') && (j < 4)) ||
                        ((piece == 'P') && (i == 1) && (((turn == 'w') && (j < 2)) || ((turn == 'b') && ((j == 2) || (j == 3)))))){
                    
                        if (pin != (9,9)){
                            pins.Add((pin,direction));
                            break;
                        }

                        else {
                            checks.Add((end,direction));
                            inCheck = true;
                            break;
                        }
                    }

                    else {
                        break;
                    }
                }
            }
        }

        int[,] knightMovements = {{2,1},{1,2},{-2,1},{-1,2},{2,-1},{1,-2},{-2,-1},{-1,-2}};
        for (int i = 0; i<8; i++){
            (int x, int y) end = (knightMovements[i,0] + start.x, knightMovements[i,1] + start.y);
            if ((0 <= end.x) && (end.x < 8) && (0 <= end.y) && (end.y < 8)){
                if ((grid[end.y,end.x][1] == 'N') && (grid[end.y,end.x][0] != turn)){
                    checks.Add((end, (knightMovements[i,0], knightMovements[i,1])));
                    inCheck = true;
                }
            }
        }
    }

    public List<Move> GetAllMoves(){
        List<Move> moves = new List<Move>();
        for(int x = 0; x < 8; x++){
            for(int y = 0; y < 8; y++){
                string piece = grid[y,x];
                if(((piece[0] == 'w') && (whiteToMove)) || 
                ((piece[0] == 'b') && (whiteToMove == false))){
                    if(piece[1] == 'P'){
                        GetMovesPawn(x, y, moves);
                    }
                    else if(piece[1] == 'B'){
                        GetMovesBishop(x, y, moves);
                    }
                    else if(piece[1] == 'R'){
                        GetMovesRook(x, y, moves);
                    }
                    else if(piece[1] == 'K'){
                        GetMovesKing(x, y, moves);
                    }
                    else if(piece[1] == 'N'){
                        GetMovesKnight(x, y, moves);
                    }
                    else if(piece[1] == 'Q'){
                        GetMovesRook(x, y, moves);
                        GetMovesBishop(x, y, moves);
                    }
                }
            }
        } 
        return moves;
    }

    void GetMovesPawn(int x, int y, List<Move> moves){
        bool piecePinned = false;
        (int x, int y) pinDirection = (9,9);

        for (int i = pins.Count - 1; i >= 0; i--){
            if (pins[i].pin == (x , y)){
                piecePinned = true;
                pinDirection = pins[i].direction;
                pins.RemoveAt(i);
                break;
            }
        }

        
        if (whiteToMove){
            if (y != 7){
                if (grid[y+1,x] == "--"){
                    if ((piecePinned == false) || (pinDirection == (0,1))){
                        Move move = new Move();
                        move.board = grid;
                        move.start = (x,y);
                        move.end = (x,y+1);
                        moves.Add(move);
                        if ((grid[y+2,x] == "--") && (y == 1)){
                            Move move2 = new Move();
                            move2.board = grid;
                            move2.start = (x,y);
                            move2.end = (x,y+2);
                            moves.Add(move2);
                        }
                    }
                }
                if (x+1 < 8){
                    if ((piecePinned == false) || (pinDirection == (1,1))){
                        if (grid[y+1, x+1][0] == 'b'){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = (x+1,y+1);
                            moves.Add(move);
                        }
                    }
                }

                if (x-1 >= 0){
                    if ((piecePinned == false) || (pinDirection == (-1,1))){
                        if (grid[y+1, x-1][0] == 'b'){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = (x-1,y+1);
                            moves.Add(move);
                        }
                    }
                }

            }
        }
        else{
            if (y != 0){
                if (grid[y-1,x] == "--"){
                    if ((piecePinned == false) || (pinDirection == (0,-1))){
                        Move move = new Move();
                        move.board = grid;
                        move.start = (x,y);
                        move.end = (x,y-1);
                        moves.Add(move);
                        if ((grid[y-2,x] == "--") && (y == 6)){
                            Move move2 = new Move();
                            move2.board = grid;
                            move2.start = (x,y);
                            move2.end = (x,y-2);
                            moves.Add(move2);
                        }
                    }
                }
                if (x+1 < 8){
                    if ((piecePinned == false) || (pinDirection == (1,-1))){
                        if (grid[y-1, x+1][0] == 'w'){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = (x+1,y-1);
                            moves.Add(move);
                        }
                    }
                }

                if (x-1 >= 0){
                    if ((piecePinned == false) || (pinDirection == (-1,-1))){
                        if (grid[y-1, x-1][0] == 'w'){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = (x-1,y-1);
                            moves.Add(move);
                        }
                    }
                }

            }
        }
    }

    void GetMovesKing(int x, int y, List<Move> moves){
        bool inCheck1 = false;

        List<((int x, int y) end, (int x, int y) direction)> irr1 = new List<((int x, int y) end, (int x, int y) direction)>();
        List<((int x, int y) pin1, (int x, int y) direction)> irr2 = new List<((int x, int y) pin, (int x, int y) direction)>();

        char turn = grid[y, x][0];

        CheckPins(irr1, irr2, ref inCheck1);

        if (!inCheck1){
            if (((!Castle.wKMoved) && (!Castle.wR0Moved) && (turn == 'w')) || ((!Castle.bKMoved) && (!Castle.bR0Moved) && (turn == 'b'))){
                bool possible = true;

                for (int i = 1; i < 3; i++){
                    inCheck1 = false;
                    (int x, int y) pos = (x-i, y);

                    if ((pos.x < 0) || (pos.x > 7) || (pos.y < 0) || (pos.y > 7)){
                        break;
                    }

                    if (grid[pos.y, pos.x] != "--"){
                        possible = false;
                        break;
                    }

                    if (turn == 'w'){
                        wKLocation = pos;
                        CheckPins(irr1, irr2, ref inCheck1);
                        wKLocation = (x, y);
                    }

                    else{
                        bKLocation = pos;
                        CheckPins(irr1, irr2, ref inCheck1);
                        bKLocation = (x, y);
                    }


                    if (inCheck1){
                        possible = false;
                        break;
                    }
                }

                if (possible){
                    Move move = new Move();
                    move.board = grid;
                    move.start = (x,y);
                    move.end = (x-2, y);
                    move.castle = true;
                    move.rookMoved = true;
                    move.kingMoved = true;
                    moves.Add(move);
                }
            }

            if (((!Castle.wKMoved) && (!Castle.wR7Moved)) || ((!Castle.bKMoved) && (!Castle.bR7Moved))){
                bool possible = true;

                for (int i = 1; i < 4; i++){
                    inCheck1 = false;
                    (int x, int y) pos = (x+i, y);

                    if (grid[pos.y, pos.x] != "--"){
                        possible = false;
                        break;
                    }

                    if (turn == 'w'){
                        wKLocation = pos;
                        CheckPins(irr1, irr2, ref inCheck1);
                        wKLocation = (x, y);
                    }

                    else{
                        bKLocation = pos;
                        CheckPins(irr1, irr2, ref inCheck1);
                        bKLocation = (x, y);
                    }

                    if (inCheck1){
                        possible = false;
                        break;
                    }
                }

                if (possible){
                    Move move = new Move();
                    move.board = grid;
                    move.start = (x,y);
                    move.end = (x+2, y);
                    move.castle = true;
                    move.rookMoved = true;
                    move.kingMoved = true;
                    moves.Add(move);
                }
            }

        }

        int[,] directions = {{1,0},{0,-1},{-1,0},{0,1},{1,1},{1,-1},{-1,1},{-1,-1}};

        for(int i = 0; i < directions.GetLength(0); i++){
            inCheck1 = false;
            (int x, int y) movement = (directions[i,0], directions[i,1]);
            (int x, int y) end = (x + movement.x, y + movement.y); 
            if ((0 <= end.x) && (end.x < 8) && (0 <= end.y) && (end.y < 8)){
                if(turn != grid[end.y, end.x][0]){
                    if (turn == 'w'){
                        wKLocation = end;
                    }
                    else{
                        bKLocation = end;
                    }

                    CheckPins(irr1, irr2, ref inCheck1);

                    if (turn == 'w'){
                        wKLocation = (x, y);
                    }
                    else{
                        bKLocation = (x, y);
                    }

                    if (inCheck1 == false){
                        Move move = new Move();
                        move.board = grid;
                        move.start = (x,y);
                        move.end = end;
                        if (((Castle.bKMoved) && (turn == 'b')) || ((Castle.wKMoved) && (turn == 'w'))){
                            move.kingMoved = true;
                        }
                        moves.Add(move); 
                    }    
                }
            }
        }
    }

    void GetMovesQueen(int x, int y, List<Move> moves){
        bool piecePinned = false;
        (int x, int y) pinDirection = (9,9);

        for (int i = pins.Count - 1; i >= 0; i--){
            if (pins[i].pin == (x , y)){
                piecePinned = true;
                pinDirection = pins[i].direction;
                pins.RemoveAt(i);
                break;
            }
        }

        int[,] directions = {{1,0},{0,-1},{0,1},{-1,0},{1,1},{1,-1},{-1,1},{-1,-1}};
        
        for(int i = 0; i < directions.GetLength(0); i++){
            (int x, int y) movement = (directions[i,0], directions[i,1]);
            if ((piecePinned == false) || (pinDirection == movement)){
                for(int j = 1; j < 8; j++){
                    (int x, int y) end = (x + movement.x * j, y + movement.y * j);
                    if ((0 <= end.x) && (end.x < 8) && (0 <= end.y) && (end.y < 8)){
                        if(grid[end.y, end.x] == "--"){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = end;
                            moves.Add(move);
                        }
                        else if(grid[y, x][0] != grid[end.y, end.x][0]){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = end;
                            moves.Add(move);
                            break;
                        }
                        else{
                            break;
                        }
                    }
                    else{
                        break;
                    }
                }
            }
        }
    }


    void GetMovesRook(int x, int y, List<Move> moves){
        bool piecePinned = false;
        (int x, int y) pinDirection = (9,9);

        bool firstMove = false;

        if (((x, y) == (0,0) && (Castle.wR0Moved == false))
         || ((x, y) == (7,0) && (Castle.wR7Moved == false))
         || ((x, y) == (0,7) && (Castle.bR0Moved == false))
         || ((x, y) == (7,7) && (Castle.bR7Moved == false))){
             firstMove = true;
         }

        for (int i = pins.Count - 1; i >= 0; i--){
            if (pins[i].pin == (x , y)){
                piecePinned = true;
                pinDirection = pins[i].direction;
                pins.RemoveAt(i);
                break;
            }
        }

        int[,] directions = {{1,0},{0,-1},{0,1},{-1,0}};

        for(int i = 0; i < directions.GetLength(0); i++){
            (int x, int y) movement = (directions[i,0], directions[i,1]);
            if ((piecePinned == false) || (pinDirection == movement)){
                for(int j = 1; j < 8; j++){
                    (int x, int y) end = (x + movement.x * j, y + movement.y * j);
                    if ((0 <= end.x) && (end.x < 8) && (0 <= end.y) && (end.y < 8)){
                        if(grid[end.y, end.x] == "--"){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = end;
                            if (firstMove){
                                move.rookMoved = true;
                            }
                            moves.Add(move);
                        }
                        else if(grid[y, x][0] != grid[end.y, end.x][0]){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = end;
                            if (firstMove){
                                move.rookMoved = true;
                            }
                            moves.Add(move);
                            break;
                        }
                        else{
                            break;
                        }
                    }
                    else{
                        break;
                    }
                }
            }
        }
    }

    void GetMovesBishop(int x, int y, List<Move> moves){
        bool piecePinned = false;
        (int x, int y) pinDirection = (9,9);

        for (int i = pins.Count - 1; i >= 0; i--){
            if (pins[i].pin == (x , y)){
                piecePinned = true;
                pinDirection = pins[i].direction;
                pins.RemoveAt(i);
                break;
            }
        }

        int[,] directions = {{1,1},{1,-1},{-1,1},{-1,-1}};

        for(int i = 0; i < directions.GetLength(0); i++){
            (int x, int y) movement = (directions[i,0], directions[i,1]); 
            if ((piecePinned == false) || (pinDirection == movement)){
                for(int j = 1; j < 8; j++){
                    (int x, int y) end = (x + movement.x * j, y + movement.y * j);
                    if ((0 <= end.x) && (end.x < 8) && (0 <= end.y) && (end.y < 8)){
                        if(grid[end.y, end.x] == "--"){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = end;
                            moves.Add(move);
                        }
                        else if(grid[y, x][0] != grid[end.y, end.x][0]){
                            Move move = new Move();
                            move.board = grid;
                            move.start = (x,y);
                            move.end = end;
                            moves.Add(move);
                            break;
                        }
                        else{
                            break;
                        }
                    }
                    else{
                        break;
                    }
                }
            }
        }
    }


    //returns all the moves the knight can make
    void GetMovesKnight(int x, int y, List<Move> moves){
        bool piecePinned = false;

        //checks if the piece is pinned
        for (int i = pins.Count - 1; i >= 0; i--){
            if (pins[i].pin == (x , y)){
                piecePinned = true;
                pins.RemoveAt(i);
                break;
            }
        }

        //shows the knight movement directions
        int[,] directions = {{2,1},{1,2},{-2,1},{-1,2},{2,-1},{1,-2},{-2,-1},{-1,-2}};

        if (piecePinned == false){
            //loops through all the possible directions
            for(int i = 0; i < directions.GetLength(0); i++){
                //the square the piece lands on after the move
                (int x, int y) end = (directions[i,0] + x, directions[i,1] + y);
                //makes sure the piece isn't outside the board
                if ((0 <= end.x) && (end.x < 8) && (0 <= end.y) && (end.y < 8)){
                    //checks if end square has a piece of the same colour
                    if(grid[y, x][0] != grid[end.y, end.x][0]){
                        Move move = new Move();
                        move.board =  grid;
                        move.start = (x,y);
                        move.end = end;
                        moves.Add(move);
                    }
                }  
            }
        }
    }
}
