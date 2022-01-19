using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Main : MonoBehaviour
{
    public GameObject[] piecePrefabs;
    public Vector3 FirstSquarePos;
    public float SquareSpace;
    public GameObject sphere;
    Dictionary<string, GameObject> pieces = new Dictionary<string, GameObject>();
    Board board = new Board();
    int mouseClicks = 0;
    List<Move> possibleMoves = new List<Move>();
    List<GameObject> highlightedSquares = new List<GameObject>();
    List<GameObject> drawnPieces = new List<GameObject>();
    Dictionary<GameObject,(int x, int y)> drawnPiecesDict = new Dictionary<GameObject,(int x, int y)>();
    (int x, int y) startSquare = (9,9);
    (int x, int y) prevStartSquare = (9,9);
    (int x, int y) endSquare;
    bool again = false;
    bool generate = false;

    // Start is called before the first frame update
    void Start()
    {
        EditDict();
        DrawPieces();
        possibleMoves = board.GetAllValidMoves();
    }

    // Update is called once per frame
    void Update()
    {
        again = false;
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit  hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.name != "Board" ){
                    Vector3 M1_position = hit.transform.position;
                    var piece = hit.transform.GetComponent<Renderer>();

                    (int x, int y) mousePos = WorldToGrid(piece);
                    mouseClicks += 1;

                    if ((board.grid[mousePos.y,mousePos.x][0] != board.grid[possibleMoves[0].start.y, possibleMoves[0].start.x][0]) && (mouseClicks == 1)){
                        mouseClicks = 0;
                    }

                    if ((startSquare == mousePos) && (mouseClicks > 1)){
                        mouseClicks = 0;
                        startSquare = (9,9);
                        UnHighlightSquares();
                    }
                    
                    else if ((board.grid[mousePos.y,mousePos.x][0] == board.grid[possibleMoves[0].start.y, possibleMoves[0].start.x][0]) && (mouseClicks > 1)){
                        mouseClicks = 1;
                        UnHighlightSquares();
                    }

                    if (mouseClicks == 1){
                        startSquare = mousePos;
                        if (startSquare == prevStartSquare){
                            again = true;
                        }
                    }
                    
                    if (mouseClicks > 1){
                        endSquare = mousePos;
                        foreach (Move move in possibleMoves){
                            if ((move.start == startSquare) && (move.end == endSquare)){
                                mouseClicks = 0;
                                generate = true;
                                board.MovePiece(move);
                                StartCoroutine(MovePiece(move));
                            }
                        }
                    }
                }
            }
        }

        if (generate){
            possibleMoves = board.GetAllValidMoves();
            CameraMovement(board.whiteToMove);
            generate = false;
        }

        if (((startSquare != (9,9)) && (startSquare != prevStartSquare)) || (again) ){
            HighlightSquares(startSquare);
            prevStartSquare = startSquare;
        }
    }

    (int x, int y) WorldToGrid(UnityEngine.Renderer piece){
        (int x, int y) gridPos = ((int)((piece.transform.position.x - 5.25) / -1.5) , (int)((piece.transform.position.z + 5.25) / 1.5));
        return gridPos;
    }

    void CameraMovement(bool whiteToMove){
        if (whiteToMove){
            Camera.main.transform.position = new Vector3(0, 10.5f, -7.5f);
            Camera.main.transform.rotation = Quaternion.Euler(60,0,0);
        }
        else {
            Camera.main.transform.position = new Vector3(0, 10.5f, 7.5f);
            Camera.main.transform.rotation = Quaternion.Euler(120,0,180);
        }

    }

    void EditDict(){
        foreach(GameObject piece in piecePrefabs){
            pieces.Add(piece.name, piece);
        }
    }

    void DrawPieces(){

        foreach (GameObject piece in drawnPieces){
            GameObject.Destroy(piece);
        }
        drawnPieces.Clear();
        drawnPiecesDict.Clear();

        UnHighlightSquares();

        for(int row = 0; row < 8; row++){
            for(int col = 0; col < 8; col++){
                string pieceStr = board.grid[row,col];
                if (pieceStr == "--"){
                    continue;
                }
                GameObject piece = Instantiate(pieces[pieceStr], CalculatePiecePos(col, row), transform.rotation);
                drawnPieces.Add(piece);
                drawnPiecesDict.Add(piece, (row, col));
            }
        }
    }

    void HighlightSquares((int x, int y) pieceSquare){
        foreach (Move move in possibleMoves){
            if (pieceSquare == move.start){
                float worldX = (float)((move.end.x * -1.5) + 5.25);
                float worldZ = (float)((move.end.y * 1.5) - 5.25);
                float worldY = 0.001f;
                GameObject newHighlight  = Instantiate(sphere, new Vector3(worldX, worldY, worldZ), transform.rotation);
                highlightedSquares.Add(newHighlight);
            }
        }
    }

    void UnHighlightSquares(){
        foreach(GameObject highlight in highlightedSquares){
            GameObject.Destroy(highlight);
        }
    }

    IEnumerator MovePiece(Move move){
        foreach (GameObject pi in drawnPieces){
            if (drawnPiecesDict[pi] == (move.end.y,move.end.x)){
                continue;
            }
            GameObject.Destroy(pi);
        }


        UnHighlightSquares();

        float speed = 10;
        string pieceStr = board.grid[move.end.y, move.end.x];
        GameObject piece = Instantiate(pieces[pieceStr], CalculatePiecePos(move.start.x, move.start.y), transform.rotation);
        Vector3 endPos = CalculatePiecePos(move.end.x, move.end.y);

        for(int row = 0; row < 8; row++){
                    for(int col = 0; col < 8; col++){
                        string pieceString = board.grid[row,col];
                        if (pieceString == "--"){
                            continue;
                        }
                        if ((move.end.y == row) && (move.end.x == col)){
                            continue;
                        }
                        GameObject piece1 = Instantiate(pieces[pieceString], CalculatePiecePos(col, row), transform.rotation);
                        drawnPieces.Add(piece1);
                        drawnPiecesDict.Add(piece1, (row, col));
                    }
                }

        while (piece.transform.position != endPos){
            piece.transform.position = Vector3.MoveTowards(piece.transform.position, endPos, speed * Time.deltaTime);
            yield return null;
        }
        drawnPieces.Add(piece);
        drawnPiecesDict.Add(piece, (move.end.y,move.end.x));
        DrawPieces();
    
    }

    Vector3 CalculatePiecePos(int col, int row){
        int c = 7 - col;
        float z = row * SquareSpace + FirstSquarePos.z;
        float x = c * SquareSpace + FirstSquarePos.x;
        return new Vector3(x, 0, z);
    }
}
