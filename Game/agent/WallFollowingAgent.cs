using Godot;

public partial class WallFollowingAgent : Node2D
{
	private static readonly Vector2I[] Directions =
	{
		new(0, -1),
		new(1, 0),
		new(0, 1),
		new(-1, 0)
	};

	private static readonly Vector2I FloorAtlasCoords = new(1, 14);
	private const int TileSetSourceId = 0;

	[Export]
	public NodePath MazeLayerPath { get; set; } = new("../foreground");

	[Export(PropertyHint.Range, "0.03,1.0,0.01")]
	public float SecondsPerCell { get; set; } = 0.12f;

	[Export]
	public bool FollowLeftWall { get; set; }

	[Export]
	public Color AgentColor { get; set; } = Colors.LimeGreen;

	private TileMapLayer _mazeLayer = null!;
	private Vector2I _currentCell;
	private Vector2I _nextCell;
	private Vector2 _moveStart;
	private Vector2 _moveEnd;
	private float _moveProgress;
	private int _directionIndex = 1;
	private bool _isMoving;
	private bool _isRunning;
	private bool _reachedExit;

	public Vector2I CurrentCell => _currentCell;
	public int StepsTaken { get; private set; }
	public double ElapsedSeconds { get; private set; }
	public bool IsRunning => _isRunning;
	public bool ReachedExit => _reachedExit;

	public override void _Ready()
	{
		_mazeLayer = GetNodeOrNull<TileMapLayer>(MazeLayerPath);
		if (_mazeLayer == null)
		{
			GD.PushError($"{nameof(WallFollowingAgent)} could not find a TileMapLayer at '{MazeLayerPath}'.");
			SetProcess(false);
			return;
		}

		// The maze generator opens (0,1) as the entrance; face right into the maze.
		_currentCell = new Vector2I(0, 1);
		_directionIndex = 1;
		if (!IsWalkable(_currentCell))
		{
			GD.PushError($"{nameof(WallFollowingAgent)} start cell {_currentCell} is not an open floor tile.");
			SetProcess(false);
			return;
		}

		GlobalPosition = CellToGlobalPosition(_currentCell);
		_isRunning = true;
		ChooseNextCell();
	}

	public override void _Process(double delta)
	{
		if (!_isRunning)
		{
			return;
		}

		ElapsedSeconds += delta;
		if (!_isMoving)
		{
			return;
		}

		_moveProgress += (float)delta / Mathf.Max(SecondsPerCell, 0.03f);
		if (_moveProgress >= 1.0f)
		{
			_currentCell = _nextCell;
			StepsTaken++;
			GlobalPosition = _moveEnd;
			_isMoving = false;
			QueueRedraw();

			if (IsAtExit())
			{
				_reachedExit = true;
				_isRunning = false;
				return;
			}

			ChooseNextCell();
			return;
		}

		GlobalPosition = _moveStart.Lerp(_moveEnd, _moveProgress);
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 5.0f, AgentColor);
		DrawArc(Vector2.Zero, 5.0f, 0, Mathf.Tau, 24, Colors.Black, 1.0f);
	}

	private void ChooseNextCell()
	{
		int preferredTurn = FollowLeftWall ? 3 : 1;
		int oppositeTurn = FollowLeftWall ? 1 : 3;
		int[] turnOrder =
		{
			(_directionIndex + preferredTurn) % Directions.Length,
			_directionIndex,
			(_directionIndex + oppositeTurn) % Directions.Length,
			(_directionIndex + 2) % Directions.Length
		};

		foreach (int directionIndex in turnOrder)
		{
			Vector2I candidate = _currentCell + Directions[directionIndex];
			if (!IsWalkable(candidate))
			{
				continue;
			}

			_directionIndex = directionIndex;
			_nextCell = candidate;
			_moveStart = CellToGlobalPosition(_currentCell);
			_moveEnd = CellToGlobalPosition(_nextCell);
			_moveProgress = 0.0f;
			_isMoving = true;
			return;
		}

		_isRunning = false;
		GD.PushWarning($"{nameof(WallFollowingAgent)} has no walkable neighboring cell at {_currentCell}.");
	}

	private bool IsWalkable(Vector2I cell)
	{
		return _mazeLayer.GetCellSourceId(cell) == TileSetSourceId &&
			_mazeLayer.GetCellAtlasCoords(cell) == FloorAtlasCoords;
	}

	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return _mazeLayer.ToGlobal(_mazeLayer.MapToLocal(cell));
	}

	private bool IsAtExit()
	{
		Rect2I usedRect = _mazeLayer.GetUsedRect();
		return _directionIndex == 1 && _currentCell.X == usedRect.End.X - 1;
	}
}
