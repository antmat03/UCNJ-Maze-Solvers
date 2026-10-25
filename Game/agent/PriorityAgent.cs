using Godot;
using System.Collections.Generic;

public partial class PriorityAgent : Node2D
{
	private const int FastestTimeStepWeight = 1024;

	public enum SearchAlgorithm
	{
		Dijkstra,
		AStar
	}

	public enum Priority
	{
		CollectCoins,
		AvoidHazards,
		FastestTime
	}

	private static readonly Vector2I[] Directions =
	{
		new(0, -1),
		new(1, 0),
		new(0, 1),
		new(-1, 0)
	};

	[Export]
	public NodePath MazeLayerPath { get; set; } = new("../foreground");

	[Export(PropertyHint.Range, "0.03,1.0,0.01")]
	public float SecondsPerCell { get; set; } = 0.12f;

	[Export]
	public Color AgentColor { get; set; } = Colors.White;

	[Export(PropertyHint.Range, "0.005,0.1,0.005")]
	public float SearchStepInterval { get; set; } = 0.01f;

	public SearchAlgorithm Algorithm { get; set; }
	public Priority AgentPriority { get; set; }
	public bool RunInSimulation { get; set; } = true;

	private readonly List<Vector2I> _path = new();
	private readonly HashSet<Vector2I> _coinsRemaining = new();
	private readonly HashSet<Vector2I> _frontierCells = new();
	private readonly HashSet<Vector2I> _exploredCells = new();
	private readonly HashSet<Vector2I> _pathCells = new();
	private readonly HashSet<Vector2I> _collectedCoins = new();
	private readonly HashSet<Vector2I> _triggeredHazards = new();
	private readonly List<Vector2I> _exitCells = new();
	private readonly PriorityQueue<(Vector2I Cell, int Cost), int> _frontier = new();
	private readonly Dictionary<Vector2I, int> _distances = new();
	private readonly Dictionary<Vector2I, Vector2I> _previousCells = new();
	private TileMapLayer _mazeLayer = null!;
	private DijkstraSearchOverlay _overlay = null!;
	private Vector2I _currentCell;
	private Vector2I _searchStart;
	private Vector2I _nextCell;
	private Vector2 _moveStart;
	private Vector2 _moveEnd;
	private float _moveProgress;
	private float _searchTimer;
	private int _pathIndex;
	private bool _isMoving;
	private bool _isRunning;
	private bool _isSearching;
	private bool _reachedExit;

	public Vector2I CurrentCell => _currentCell;
	public int StepsTaken { get; private set; }
	public int Score { get; private set; }
	public int RouteCost { get; private set; }
	public int PathLength => _path.Count == 0 ? 0 : _path.Count - 1;
	public int CellsExplored { get; private set; }
	public double ElapsedSeconds { get; private set; }
	public bool IsRunning => _isRunning;
	public bool IsSearching => _isSearching;
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
			GD.PushError($"{nameof(PriorityAgent)} could not find a TileMapLayer at '{MazeLayerPath}'.");
			SetProcess(false);
			return;
		}

		_overlay = new DijkstraSearchOverlay { Name = $"{Name}SearchOverlay" };
		_mazeLayer.AddChild(_overlay);
		_overlay.Initialize(
			_mazeLayer, _exploredCells, _frontierCells, _pathCells,
			new Color(0.18f, 0.42f, 1.0f, 0.35f),
			new Color(1.0f, 0.68f, 0.16f, 0.45f),
			new Color(0.18f, 0.92f, 0.48f, 0.65f));

		_currentCell = new Vector2I(0, 1);
		if (!AgentTileRules.IsWalkable(_mazeLayer, _currentCell))
		{
			GD.PushError($"{nameof(PriorityAgent)} start cell {_currentCell} is not walkable.");
			SetProcess(false);
			return;
		}

		_exitCells.AddRange(AgentTileRules.GetExitCells(_mazeLayer));
		if (_exitCells.Count == 0)
		{
			GD.PushError($"{nameof(PriorityAgent)} could not find a walkable exit.");
			SetProcess(false);
			return;
		}

		ReloadRemainingCoins();

		GlobalPosition = CellToGlobalPosition(_currentCell);
		_isRunning = true;
		StartNextSearch(_currentCell);
	}

	public override void _Process(double delta)
	{
		if (!_isRunning)
		{
			return;
		}

		ElapsedSeconds += delta;
		if (_isSearching)
		{
			_searchTimer += (float)delta;
			float interval = Mathf.Max(SearchStepInterval, 0.005f);
			while (_searchTimer >= interval && _isSearching)
			{
				_searchTimer -= interval;
				ExploreNextCell();
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
					_triggeredHazards.Add(_currentCell);
				}

				_currentCell = new Vector2I(0, 1);
				GlobalPosition = CellToGlobalPosition(_currentCell);
				_path.Clear();
				_pathCells.Clear();
				_pathIndex = 0;
				RouteCost = 0;
				_exploredCells.Clear();
				ReloadRemainingCoins();
				_overlay.QueueRedraw();
				StartNextSearch(_currentCell);
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

	private void StartNextSearch(Vector2I startCell)
	{
		RemoveUnavailableCoins();

		_frontier.Clear();
		_frontierCells.Clear();
		_distances.Clear();
		_previousCells.Clear();
		_searchStart = startCell;
		_distances[_searchStart] = 0;
		_frontier.Enqueue((_searchStart, 0), 0);
		_frontierCells.Add(_searchStart);
		_isSearching = true;
		_isRunning = true;
		_searchTimer = 0.0f;
		_overlay.QueueRedraw();
	}

	private void RemoveUnavailableCoins()
	{
		_coinsRemaining.RemoveWhere(cell =>
			_collectedCoins.Contains(cell) ||
			_mazeLayer.GetCellSourceId(cell) != AgentTileRules.TileSetSourceId ||
			_mazeLayer.GetCellAtlasCoords(cell) != AgentTileRules.CoinAtlasCoords);
	}

	private void ReloadRemainingCoins()
	{
		_coinsRemaining.Clear();
		Rect2I usedRect = _mazeLayer.GetUsedRect();
		for (int y = usedRect.Position.Y; y < usedRect.End.Y; y++)
		{
			for (int x = usedRect.Position.X; x < usedRect.End.X; x++)
			{
				Vector2I cell = new(x, y);
				if (!_collectedCoins.Contains(cell) &&
					_mazeLayer.GetCellSourceId(cell) == AgentTileRules.TileSetSourceId &&
					_mazeLayer.GetCellAtlasCoords(cell) == AgentTileRules.CoinAtlasCoords)
				{
					_coinsRemaining.Add(cell);
				}
			}
		}
	}

	private bool IsGoal(Vector2I cell)
	{
		return AgentPriority != Priority.FastestTime && _coinsRemaining.Count > 0
			? _coinsRemaining.Contains(cell)
			: _exitCells.Contains(cell);
	}

	private void ExploreNextCell()
	{
		while (_frontier.TryDequeue(out (Vector2I Cell, int Cost) entry, out _))
		{
			Vector2I current = entry.Cell;
			if (!_distances.TryGetValue(current, out int bestKnownCost) ||
				entry.Cost != bestKnownCost)
			{
				continue;
			}

			int currentCost = entry.Cost;
			_frontierCells.Remove(current);
			_exploredCells.Add(current);
			CellsExplored++;
			if (IsGoal(current))
			{
				AppendPathTo(current);
				_searchStart = current;
				if (_coinsRemaining.Remove(current))
				{
					StartNextSearch(current);
				}
				else
				{
					FinishPlanning();
				}

				_overlay.QueueRedraw();
				return;
			}

			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = current + direction;
				if (!AgentTileRules.IsWalkable(_mazeLayer, neighbor))
				{
					continue;
				}

				if (AgentPriority == Priority.AvoidHazards &&
					AgentTileRules.IsHazard(_mazeLayer, neighbor))
				{
					continue;
				}

				if (_triggeredHazards.Contains(neighbor))
				{
					continue;
				}

				int movementCost = GetMovementCost(neighbor);
				if (movementCost == AgentTileRules.InfiniteMovementCost)
				{
					continue;
				}

				int candidateCost = currentCost + movementCost;
				if (_distances.TryGetValue(neighbor, out int knownCost) &&
					knownCost <= candidateCost)
				{
					continue;
				}

				_distances[neighbor] = candidateCost;
				_previousCells[neighbor] = current;
				int priority = Algorithm == SearchAlgorithm.AStar
					? candidateCost + GetHeuristic(neighbor)
					: candidateCost;
				_frontier.Enqueue((neighbor, candidateCost), priority);
				_frontierCells.Add(neighbor);
			}

			_overlay.QueueRedraw();
			return;
		}

		if (_coinsRemaining.Count > 0)
		{
			GD.PushWarning($"{Name} could not reach the remaining coins; continuing to an exit.");
			_coinsRemaining.Clear();
			StartNextSearch(_searchStart);
			return;
		}

		_isSearching = false;
		_isRunning = false;
		GD.PushWarning($"{Name} could not find a path to an exit.");
	}

	private int GetMovementCost(Vector2I cell)
	{
		if (AgentPriority == Priority.AvoidHazards &&
			AgentTileRules.IsHazard(_mazeLayer, cell))
		{
			return AgentTileRules.InfiniteMovementCost;
		}

		if (AgentPriority == Priority.CollectCoins &&
			AgentTileRules.IsHazard(_mazeLayer, cell))
		{
			Rect2I mazeBounds = _mazeLayer.GetUsedRect();
			return mazeBounds.Size.X * mazeBounds.Size.Y + 1;
		}

		if (AgentPriority == Priority.FastestTime)
		{
			return FastestTimeStepWeight +
				(AgentTileRules.IsHazard(_mazeLayer, cell) ? 1 : 0);
		}

		return AgentTileRules.GetMovementCost(_mazeLayer, cell);
	}

	private void AppendPathTo(Vector2I target)
	{
		List<Vector2I> segment = new();
		for (Vector2I cell = target; ; cell = _previousCells[cell])
		{
			segment.Add(cell);
			if (cell == _searchStart)
			{
				break;
			}
		}

		segment.Reverse();
		for (int i = 1; i < segment.Count; i++)
		{
			_path.Add(segment[i]);
			_pathCells.Add(segment[i]);
			RouteCost += AgentTileRules.GetMovementCost(_mazeLayer, segment[i]);
		}
	}

	private void FinishPlanning()
	{
		_isSearching = false;
		if (_path.Count == 0)
		{
			_reachedExit = true;
			_isRunning = false;
			return;
		}

		_currentCell = new Vector2I(0, 1);
		GlobalPosition = CellToGlobalPosition(_currentCell);
		_pathIndex = 0;
		_overlay.QueueRedraw();
		StartNextMove();
	}

	private int GetHeuristic(Vector2I cell)
	{
		int shortestDistance = int.MaxValue;
		int minimumStepCost = AgentPriority == Priority.FastestTime
			? FastestTimeStepWeight
			: AgentTileRules.FloorMovementCost;
		IEnumerable<Vector2I> goals = AgentPriority != Priority.FastestTime &&
			_coinsRemaining.Count > 0
			? _coinsRemaining
			: _exitCells;
		foreach (Vector2I goal in goals)
		{
			int distance = (Mathf.Abs(cell.X - goal.X) + Mathf.Abs(cell.Y - goal.Y)) *
				minimumStepCost;
			shortestDistance = Mathf.Min(shortestDistance, distance);
		}

		return shortestDistance;
	}

	private void StartNextMove()
	{
		if (_pathIndex >= _path.Count)
		{
			_reachedExit = true;
			_isRunning = false;
			return;
		}

		_nextCell = _path[_pathIndex++];
		_moveStart = CellToGlobalPosition(_currentCell);
		_moveEnd = CellToGlobalPosition(_nextCell);
		_moveProgress = 0.0f;
		_isMoving = true;
	}

	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return _mazeLayer.ToGlobal(_mazeLayer.MapToLocal(cell));
	}
}
