public enum Piece { X, O, Y, Z } // Extensible for more players

public class Player
{
    public int Id { get; } // Used for the O(1) win array mapping
    public string Name { get; }
    public Piece Piece { get; }

    public Player(int id, string name, Piece piece)
    {
        Id = id;
        Name = name;
        Piece = piece;
    }
}

public record Cell(int Row, int Col);

public class Board
{
    public int Size { get; }
    private readonly Piece?[,] _grid;
    public int TotalMoves { get; private set; }

    public Board(int size)
    {
        Size = size;
        _grid = new Piece?[size, size];
    }

    public bool IsValidMove(Cell cell)
    {
        return cell.Row >= 0 && cell.Row < Size &&
               cell.Col >= 0 && cell.Col < Size &&
               _grid[cell.Row, cell.Col] == null;
    }

    public void ApplyMove(Cell cell, Piece piece)
    {
        _grid[cell.Row, cell.Col] = piece;
        TotalMoves++;
    }

    public bool IsFull() => TotalMoves == (Size * Size);

    public void Display()
    {
        // Simple console render (omitted for brevity in interview, just write a comment)
        Console.WriteLine($"\nBoard updated. Total Moves: {TotalMoves}");
    }
}

public class WinAnalyzer
{
    private readonly int _boardSize;

    // Arrays track score per player. Dimensions: [Row/Col Index, PlayerId]
    private readonly int[,] _rowCounts;
    private readonly int[,] _colCounts;
    private readonly int[] _diagCounts;
    private readonly int[] _antiDiagCounts;

    public WinAnalyzer(int boardSize, int numberOfPlayers)
    {
        _boardSize = boardSize;
        _rowCounts = new int[boardSize, numberOfPlayers];
        _colCounts = new int[boardSize, numberOfPlayers];
        _diagCounts = new int[numberOfPlayers];
        _antiDiagCounts = new int[numberOfPlayers];
    }

    public bool RecordAndCheckWin(Cell cell, int playerId)
    {
        _rowCounts[cell.Row, playerId]++;
        _colCounts[cell.Col, playerId]++;

        if (cell.Row == cell.Col)
            _diagCounts[playerId]++;

        if (cell.Row + cell.Col == _boardSize - 1)
            _antiDiagCounts[playerId]++;

        // If any of this player's counts reach the board size, they win
        return _rowCounts[cell.Row, playerId] == _boardSize ||
               _colCounts[cell.Col, playerId] == _boardSize ||
               _diagCounts[playerId] == _boardSize ||
               _antiDiagCounts[playerId] == _boardSize;
    }
}

public class TicTacToeGame
{
    private readonly Board _board;
    private readonly WinAnalyzer _winAnalyzer;
    private readonly Queue<Player> _players;

    public TicTacToeGame(int boardSize, List<Player> players)
    {
        _board = new Board(boardSize);
        _winAnalyzer = new WinAnalyzer(boardSize, players.Count);
        _players = new Queue<Player>(players);
    }

    public void Start()
    {
        Console.WriteLine("Game Started!");
        _board.Display();

        while (true)
        {
            var currentPlayer = _players.Dequeue();
            Cell move = GetValidMoveFromUser(currentPlayer);

            _board.ApplyMove(move, currentPlayer.Piece);
            _board.Display();

            if (_winAnalyzer.RecordAndCheckWin(move, currentPlayer.Id))
            {
                Console.WriteLine($"*** {currentPlayer.Name} ({currentPlayer.Piece}) WINS! ***");
                return;
            }

            if (_board.IsFull())
            {
                Console.WriteLine("*** Game ends in a DRAW! ***");
                return;
            }

            // Put player back in queue for next turn
            _players.Enqueue(currentPlayer);
        }
    }

    private Cell GetValidMoveFromUser(Player player)
    {
        while (true)
        {
            Console.WriteLine($"{player.Name}'s turn. Enter Row and Col (e.g. 0 1):");
            // In a real interview, mock this or write simple Console.ReadLine parsing
            var input = Console.ReadLine()?.Split(' ');

            if (input != null && input.Length == 2 &&
                int.TryParse(input[0], out int row) &&
                int.TryParse(input[1], out int col))
            {
                var cell = new Cell(row, col);
                if (_board.IsValidMove(cell))
                {
                    return cell;
                }
            }
            Console.WriteLine("Invalid move. Cell is taken or out of bounds. Try again.");
        }
    }
}

/*
AI TIPS:

When you walk into an LLD round, follow this exact sequence:

Clarify Requirements (5 mins): "Does it have to be 3x3? Do we need to support more than 2 players?"

Draft the Entities verbally (5 mins): "I'm thinking a Board class to hold state, a Player class, and a TicTacToeGame orchestrator."

Write the Concrete Implementation (20 mins): Write the simplest, cleanest classes that separate concerns properly. Don't worry about DI containers or generic interfaces yet. Get the game loop running.

Refactor and Flex (15 mins): Once the code is working, you say, "Okay, we have a working game. Now let's talk about how to make it extensible." This is where you point out where the interfaces should go, or apply the Strategy pattern if the interviewer asks, "How would you add a rule where playing in the 4 corners wins?"

Always secure the working solution first. You can always verbally scale a simple working system, but you cannot easily debug a complex, broken system with 3 minutes left on the clock.
*/
