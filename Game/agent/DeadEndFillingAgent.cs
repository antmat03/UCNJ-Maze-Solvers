using Godot;
using System.Collections.Generic;

public partial class DeadEndFillingAgent : Node2D
{
	private static readonly Vector2I[] Directions =
	{
		new(0, -1),
		new(1, 0),
		new(0, 1),
		new(-1, 0)
	};

	private static readonly HashSet<Vector2I> NoCells = new();

	[Export]
	public NodePath MazeLayerPath { get; set; } = new("../foreground");

	[Export(PropertyHint.Range, "0.03,1.0,0.01")]
	public float SecondsPerCell { get; set; } = 0.12f;

	[Export(PropertyHint.Range, "0.005,0.1,0.005")]
	public float FillStepInterval { get; set; } = 0.025f;

	[Export]
	public Color AgentColor { get; set; } = Colors.Gold;

	public bool RunInSimulation { get; set; } = false;

	private readonly HashSet<Vector2I> _remainingCells = new();
	private readonly HashSet<Vector2I> _filledCells = new();
	private readonly HashSet<Vector2I> _pathCells = new();
	private readonly HashSet<Vector2I> _collectedCoins = new();
	private readonly HashSet<Vector2I> _exitCells = new();
	private readonly HashSet<Vector2I> _blockedHazards = new();
	private readonly Dictionary<Vector2I, int> _degrees = new();
	private readonly Queue<Vector2I> _deadEnds = new();
	private readonly HashSet<Vector2I> _queuedDeadEnds = new();
	private readonly List<Vector2I> _path = new();
	private TileMapLayer _mazeLayer = null!;
	private DijkstraSearchOverlay _overlay = null!;
	private Vector2I _currentCell;
	private Vector2I _nextCell;
	private Vector2I _startCell;
	private Vector2 _moveStart;
	private Vector2 _moveEnd;
	private float _fillTimer;
	private float _moveProgress;
	private int _pathIndex;
	private bool _isFilling;
	private bool _isMoving;
	private bool _isRunning;
	private bool _reachedExit;

	public Vector2I CurrentCell => _currentCell;
	public int StepsTaken { get; private set; }
	public int Score { get; private set; }
	public int PathLength => _path.Count == 0 ? 0 : _path.Count - 1;
	public int RouteCost { get; private set; }
	public int CellsFilled => _filledCells.Count;
	public double ElapsedSeconds { get; private set; }
	public bool IsRunning => _isRunning;
	public bool IsFilling => _isFilling;
	public bool ReachedExit => _reachedExit;

	public override void _Ready()
	{
		if (!RunInSimulation)
		{
			Hide();
			SetProcess(false);
			return;
		}

		_mazeLayer = GetNodeOrNull<TileMapLayer>(MazeLayerPath);
		if (_mazeLayer == null)
		{
			GD.PushError($"{nameof(DeadEndFillingAgent)} could not find a TileMapLayer at '{MazeLayerPath}'.");
			SetProcess(false);
			return;
		}

		_overlay = new DijkstraSearchOverlay();
		_overlay.Name = "DeadEndFillingOverlay";
		_mazeLayer.AddChild(_overlay);
		_overlay.Initialize(
			_mazeLayer, _filledCells, NoCells, _pathCells,
			new Color(1.0f, 0.22f, 0.18f, 0.65f),
			new Color(0.4f, 0.4f, 0.4f, 0.0f),
			new Color(0.2f, 0.95f, 0.45f, 0.82f));

		_startCell = new Vector2I(0, 1);
		_exitCells.UnionWith(AgentTileRules.GetExitCells(_mazeLayer));
		if (!IsWalkable(_startCell) || _exitCells.Count == 0)
		{
			GD.PushError($"{nameof(DeadEndFillingAgent)} could not find walkable entrance and exit cells.");
			SetProcess(false);
			return;
		}

		InitializeMazeGraph();
		if (_deadEnds.Count == 0)
		{
			CompleteFilling();
			return;
		}

		_currentCell = _startCell;
		GlobalPosition = CellToGlobalPosition(_currentCell);
		_isFilling = true;
		_isRunning = true;
		_overlay.QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!_isRunning)
		{
			return;
		}

		ElapsedSeconds += delta;
		if (_isFilling)
		{
			_fillTimer += (float)delta;
			float interval = Mathf.Max(FillStepInterval, 0.005f);
			while (_fillTimer >= interval && _isFilling)
			{
				_fillTimer -= interval;
				FillNextDeadEnd();
			}

			return;
		}

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

			if (AgentTileRules.CollectCoin(_mazeLayer, _currentCell, _collectedCoins))
			{
				Score++;
			}

			if (AgentTileRules.TriggerHazard(_mazeLayer, _currentCell))
			{
				if (AgentTileRules.IsHazard(_mazeLayer, _currentCell))
				{
					_blockedHazards.Add(_currentCell);
				}

				_currentCell = _startCell;
				GlobalPosition = CellToGlobalPosition(_currentCell);
				_isMoving = false;
				CompleteFilling();
				return;
			}

			if (_pathIndex >= _path.Count)
			{
				_reachedExit = true;
				_isRunning = false;
				return;
			}

			StartNextMove();
			return;
		}

		GlobalPosition = _moveStart.Lerp(_moveEnd, _moveProgress);
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 5.0f, AgentColor);
		DrawArc(Vector2.Zero, 5.0f, 0, Mathf.Tau, 24, Colors.Black, 1.0f);
	}

	private void InitializeMazeGraph()
	{
		Rect2I usedRect = _mazeLayer.GetUsedRect();
		for (int y = usedRect.Position.Y; y < usedRect.End.Y; y++)
		{
			for (int x = usedRect.Position.X; x < usedRect.End.X; x++)
			{
				Vector2I cell = new(x, y);
				if (IsWalkable(cell))
				{
					_remainingCells.Add(cell);
				}
			}
		}

		foreach (Vector2I cell in _remainingCells)
		{
			int degree = 0;
			foreach (Vector2I direction in Directions)
			{
				if (_remainingCells.Contains(cell + direction))
				{
					degree++;
				}
			}

			_degrees[cell] = degree;
			if (!IsProtectedCell(cell) && degree <= 1)
			{
				EnqueueDeadEnd(cell);
			}
		}
	}

	private void FillNextDeadEnd()
	{
		while (_deadEnds.Count > 0)
		{
			Vector2I deadEnd = _deadEnds.Dequeue();
			_queuedDeadEnds.Remove(deadEnd);
			if (IsProtectedCell(deadEnd) ||
				!_remainingCells.Remove(deadEnd) ||
				_degrees[deadEnd] > 1)
			{
				continue;
			}

			_filledCells.Add(deadEnd);
			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = deadEnd + direction;
				if (!_remainingCells.Contains(neighbor) || _blockedHazards.Contains(neighbor))
				{
					continue;
				}

				_degrees[neighbor]--;
				if (!IsProtectedCell(neighbor) &&
					_degrees[neighbor] <= 1)
				{
					EnqueueDeadEnd(neighbor);
				}
			}

			_overlay.QueueRedraw();
			return;
		}

		CompleteFilling();
	}

	private void EnqueueDeadEnd(Vector2I cell)
	{
		if (_queuedDeadEnds.Add(cell))
		{
			_deadEnds.Enqueue(cell);
		}
	}

	private void CompleteFilling()
	{
		_isFilling = false;
		if (!TryFindRemainingPath())
		{
			_isRunning = false;
			GD.PushWarning($"{nameof(DeadEndFillingAgent)} could not find a path after filling dead ends.");
			return;
		}

		_currentCell = _startCell;
		GlobalPosition = CellToGlobalPosition(_currentCell);
		_pathIndex = 1;
		_isRunning = true;
		_overlay.QueueRedraw();
		if (_path.Count > 1)
		{
			StartNextMove();
		}
		else
		{
			_reachedExit = true;
			_isRunning = false;
		}
	}

	private bool TryFindRemainingPath()
	{
		_path.Clear();
		_pathCells.Clear();
		PriorityQueue<Vector2I, int> cellsToVisit = new();
		Dictionary<Vector2I, Vector2I> previousCells = new();
		Dictionary<Vector2I, int> distances = new() { [_startCell] = 0 };
		Vector2I foundExit = default;
		bool exitFound = false;
		cellsToVisit.Enqueue(_startCell, 0);

		while (cellsToVisit.TryDequeue(out Vector2I current, out int queuedCost))
		{
			if (!distances.TryGetValue(current, out int currentCost) ||
				queuedCost != currentCost)
			{
				continue;
			}

			if (_exitCells.Contains(current))
			{
				foundExit = current;
				exitFound = true;
				break;
			}

			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = current + direction;
				if (!_remainingCells.Contains(neighbor))
				{
					continue;
				}

				int movementCost = AgentTileRules.GetMovementCost(_mazeLayer, neighbor);
				if (movementCost == AgentTileRules.InfiniteMovementCost)
				{
					continue;
				}

				int candidateCost = currentCost + movementCost;
				if (distances.TryGetValue(neighbor, out int knownCost) &&
					knownCost <= candidateCost)
				{
					continue;
				}

				distances[neighbor] = candidateCost;
				previousCells[neighbor] = current;
				cellsToVisit.Enqueue(neighbor, candidateCost);
			}
		}

		if (!exitFound)
		{
			return false;
		}

		for (Vector2I cell = foundExit; ; cell = previousCells[cell])
		{
			_path.Add(cell);
			_pathCells.Add(cell);
			if (cell == _startCell)
			{
				break;
			}
		}

		_path.Reverse();
		RouteCost = distances[foundExit];
		return true;
	}

	private void StartNextMove()
	{
		_nextCell = _path[_pathIndex++];
		_moveStart = CellToGlobalPosition(_currentCell);
		_moveEnd = CellToGlobalPosition(_nextCell);
		_moveProgress = 0.0f;
		_isMoving = true;
	}

	private bool IsWalkable(Vector2I cell)
	{
		return AgentTileRules.IsWalkable(_mazeLayer, cell);
	}

	private bool IsProtectedCell(Vector2I cell)
	{
		return cell == _startCell ||
			_exitCells.Contains(cell) ||
			AgentTileRules.IsHazard(_mazeLayer, cell) ||
			(_mazeLayer.GetCellSourceId(cell) == AgentTileRules.TileSetSourceId &&
			 _mazeLayer.GetCellAtlasCoords(cell) == AgentTileRules.CoinAtlasCoords);
	}

	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return _mazeLayer.ToGlobal(_mazeLayer.MapToLocal(cell));
	}
}
