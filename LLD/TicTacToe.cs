/*
Tic-Tac-Toe
--------------------------------------------------
The Requirements:

The game is played between 2 players.

The board is traditionally 3x3, but your design should theoretically support an N x N board.

Players are assigned a piece (X or O).

Players take turns making a move on a grid (row, column).

The game ends when a player wins (row, column, or diagonal) or the board is full (a draw).

Invalid moves (playing out of bounds, or on an already occupied space) must be rejected.

Your Task:
Don't write the full game loop or every line of logic just yet. Write the C# Class Layout—the entities, interfaces, properties, and method signatures.

Think about:

What are the core nouns (Entities)? (e.g., Player, Board, Piece)

Who is responsible for keeping track of whose turn it is?

Who is responsible for validating if a move is legal?

Who is responsible for checking the winning condition?
*/

/*
Game - TicTacToe
Player
Peice
Board

Player has a peice
Game has two players with different peice
One board n x n matrix -> each cell has a state (empty, x, o)
Player can play a move on empty spaces only.
Game state is - finished / inProgress.
p1 wins, p2 wins or draw.
*/

public class TicTacToe
{
    private readonly IBoard _board;
    private readonly IPlayer _player1;
    private readonly IPlayer _player2;
    private readonly List<IWinRule> _winRules;
    private readonly List<IDrawRule> _drawRules;
    private int _numberOfTurns = 0;

    public TicTacToe(int boardSize, string playerName1, string playerName2)
    {
        _board = new Board(boardSize);
        _player1 = new Player(playerName1, Peice.X);
        _player2 = new Player(playerName2, Peice.O);
        _winRules = [RowWinRule, ColumnWinRule, DiagonalWinRule];
        _drawRules = [FullBoardDrawRule];
    }

    public void Start()
    {
        var gameResult = GetGameResult();
        while (gameResult.GameState == GameState.InProgress)
        {
            var currentPlayer = _numberOfTurns % 2 == 0 ? _player1 : _player2;
            PlayWithPlayer(currentPlayer);
            _numberOfTurns++;
            _board.Display();
            gameResult = GetGameResult();
        }

        if (gameResult.GameState == GameState.Conclusive)
        {
            if (gameResult.WinningPeice == _player1.GetPeice())
            {
                Console.WriteLine($"Player {_player1.GetName()} Wins.");
            }
            else if (gameResult.WinningPeice == _player2.GetPeice())
            {
                Console.WriteLine($"Player {_player2.GetName()} Wins.");
            }
            else
            {
                Console.WriteLine("Error: Unable to determine winner.");
            }
        }
        else if (gameResult.GameState == GameState.Draw)
        {
            Console.WriteLine("Draw.");
        }
        else
        {
            Console.WriteLine("Unexpected Ending: Game ended while still in Progress.");
        }
    }

    private void PlayWithPlayer(IPlayer player) {
        bool successfulMove = false;
        while (!successfulMove) {
            successfulMove = true;
            var move = player.GetMove();
            try
            {
                _board.MakeMove(move);
            }
            catch (Exception ex) {
                Console.WriteLine($"Invalid Move: {ex.Message}");
                Console.WriteLine("Please try again.");
                successfulMove = false;
            }
        }
    }

    private GameResult GetGameResult() {
        foreach (var winRule in _winRules) {
            var winningPeice = winRule.GetWinnerPeice(_board);
            if (winningPeice != null) {
                return new GameResult(GameState.Conclusive, winningPeice);
            }
        }
        foreach (var drawRule in _drawRules) {
            if (drawRule.IsDraw()) {
                return new GameResult(GameState.Draw, null);
            }
        }
        return new GameResult(GameState.InProgress, null);
    }

    private enum GameState {
        InProgress,
        Draw,
        Conclusive
    }

    private record GameResult {
        public GameState GameState;
        public Peice? WinningPeice;
    }
}

public record Cell
{
    public int Row;
    public int Column;
}

public interface IPlayer
{
    public Cell GetMove();
    public string GetName();
    public Peice GetPeice();
}

public enum Peice
{
    X,
    O
}


public class Player : IPlayer
{
    private readonly string _name;
    private readonly Peice _peice;

    public Player(string name, Peice peice) {
        _name = name;
        _peice = peice;
    }

    public Cell GetMove()
    {
        int row, column;
        // get row and column from user input.
        return new Cell(row, column);
    }

    public string GetName() => _name;
    public Peice GetPeice() => _peice;
}

public class IBoard {
    public void MakeMove(Cell cell, Peice peice);
    public void Display();
    public Peice? GetCellPeice(Cell cell);
    public int GetBoardSize();
    public bool IsFull();
}

public class Board : IBoard
{
    private readonly int _size { get; init; }
    private Peice?[,] _matrix { get; init; }

    public Board(int size)
    {
        _size = size;
        Matrix = new Peice?[size, size];
    }

    public void Display() {
        for (var i = 0; i < _size; i++) {
            Console.WriteLine(_matrix[i]);
        }
    }

    public void MakeMove(Cell cell, Peice peice)
    {
        if (!IsValidCell)
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }
        if (_matrix[cell.Row][cell.Column] is not null)
        {
            throw new InvalidOperationException("cell is already occupied.");
        }
        _matrix[cell.Row][cell.Column] = peice;
    }

    public Peice? GetCellPeice(Cell cell) => IsValidCell(cell) ? _matrix[cell.Row][cell.Column] : null;

    public int GetBoardSize() => _size;

    public bool IsFull() {
        for (int i = 0; i < _size; i++) {
            for (int j = 0; j < _size; j++) {
                if(_matrix[i][j] == null) {
                    return false;
                }
            }
        }
        return true;
    }

    private bool IsValidCell(Cell cell) {
        return cell is { Column >= 0 and Column < _size } &&
        cell is { Row >= 0 and Row < _size };
    }
}

public interface IWinRule {
    public Peice? GetWinnerPeice(IBoard board);
}

public class RowWinRule : IWinRule {
    public Peice? GetWinnerPeice(IBoard board) {
        int size = board.GetBoardSize();
        for (int i = 0; i < size; i++)
        {
            Peice? firstPeice = board.GetCellPeice(new Cell(i, 0));
            bool win = true;
            if (firstPeice is null)
            {
                continue;
            }
            for (int j = 0; j < size; j++)
            {
                if (board.GetCellPeice(i, j) != firstPeice)
                {
                    win = false;
                    break;
                }
            }
            if (win)
            {
                return firstPeice;
            }
        }
        return null;
    }
}

public class ColumnWinRule : IWinRule {
    public Peice? GetWinnerPeice(IBoard board) {
        int size = board.GetBoardSize();
        for (int j = 0; j < size; j++)
        {
            Peice? firstPeice = board.GetCellPeice(new Cell(0, j));
            bool win = true;
            if (firstPeice is null)
            {
                continue;
            }
            for (int i = 0; i < size; i++)
            {
                if (board.GetCellPeice(i, j) != firstPeice)
                {
                    win = false;
                    break;
                }
            }
            if (win)
            {
                return firstPeice;
            }
        }
        return null;
    }
}

public class DiagonalWinRule : IWinRule {
    public Peice? GetWinnerPeice(IBoard board) {
        int size = board.GetBoardSize();
        Peice? winningPeice = null;
        winningPeice = board.GetCellPeice(new Cell(0, 0));
        if (winningPeice != null) {
            bool win = true;
            for (int i = 0; i < size; i++) {
                if (board.GetCellPeice(new Cell(i, i)) != winningPeice) {
                    win = false;
                }
            }
            if (win) {
                return winningPeice;
            }
        }
        winningPeice = board.GetCellPeice(new cell(0, size - 1));
        if (winningPeice != null) {
            bool win = true;
            for (int i = 0; i < size; i++) {
                if (board.GetCellPeice(new Cell(i, size - 1 - i)) != winningPeice) {
                    win = false;
                }
            }
            if (win) {
                return winningPeice;
            }
        }
        return null;
    }
}

public interface IDrawRule {
    public bool IsDraw(IBoard board);
}

public class FullBoardDrawRule : IDrawRule {
   public bool IsDraw(IBoard board) {
        return board.IsFull();
   }
}
