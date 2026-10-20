using Godot;
using System.Collections.Generic;

public partial class AStarAgent : Node2D
{
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
	public Color AgentColor { get; set; } = Colors.Cyan;

	[Export(PropertyHint.Range, "0.005,0.1,0.005")]
	public float SearchStepInterval { get; set; } = 0.01f;

	public bool RunInSimulation { get; set; } = true;

	private readonly List<Vector2I> _path = new();
	private readonly HashSet<Vector2I> _frontierCells = new();
	private readonly HashSet<Vector2I> _exploredCells = new();
	private readonly HashSet<Vector2I> _pathCells = new();
	private readonly HashSet<Vector2I> _collectedCoins = new();
	private readonly HashSet<Vector2I> _blockedHazards = new();
	private readonly List<Vector2I> _exitCells = new();
	private readonly PriorityQueue<(Vector2I Cell, int Distance), int> _frontier = new();
	private readonly Dictionary<Vector2I, int> _distances = new();
	private readonly Dictionary<Vector2I, Vector2I> _previousCells = new();
	private TileMapLayer _mazeLayer = null!;
	private DijkstraSearchOverlay _searchOverlay = null!;
	private Vector2I _currentCell;
	private Vector2I _nextCell;
	private Vector2I _goalCell;
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
	public int PathLength => _path.Count == 0 ? 0 : _path.Count - 1;
	public int RouteCost { get; private set; }
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
			GD.PushError($"{nameof(AStarAgent)} could not find a TileMapLayer at '{MazeLayerPath}'.");
			SetProcess(false);
			return;
		}

		_searchOverlay = new DijkstraSearchOverlay();
		_searchOverlay.Name = "AStarSearchOverlay";
		_mazeLayer.AddChild(_searchOverlay);
		_searchOverlay.Initialize(
			_mazeLayer, _exploredCells, _frontierCells, _pathCells,
			new Color(0.0f, 0.8f, 1.0f, 0.48f),
			new Color(1.0f, 0.2f, 0.75f, 0.65f),
			new Color(1.0f, 0.9f, 0.15f, 0.82f));

		_currentCell = new Vector2I(0, 1);
		if (!IsWalkable(_currentCell))
		{
			GD.PushError($"{nameof(AStarAgent)} start cell {_currentCell} is not an open floor tile.");
			SetProcess(false);
			return;
		}

		_exitCells.AddRange(AgentTileRules.GetExitCells(_mazeLayer));
		if (_exitCells.Count == 0)
		{
			GD.PushError($"{nameof(AStarAgent)} could not find a walkable exit cell.");
			SetProcess(false);
			return;
		}

		GlobalPosition = CellToGlobalPosition(_currentCell);
		_isRunning = true;
		_isSearching = true;
		_distances[_currentCell] = 0;
		_frontier.Enqueue((_currentCell, 0), GetHeuristic(_currentCell));
		_frontierCells.Add(_currentCell);
		_searchOverlay.QueueRedraw();
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
					_blockedHazards.Add(_currentCell);
				}

				RestartSearch();
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

	private void ExploreNextCell()
	{
		while (_frontier.TryDequeue(out (Vector2I Cell, int Distance) entry, out _))
		{
			Vector2I current = entry.Cell;
			int currentDistance = entry.Distance;
			if (!_distances.TryGetValue(current, out int bestDistance) ||
				currentDistance != bestDistance)
			{
				continue;
			}

			_frontierCells.Remove(current);
			_exploredCells.Add(current);
			CellsExplored++;
			if (_exitCells.Contains(current))
			{
				_goalCell = current;
				BuildShortestPath();
				_searchOverlay.QueueRedraw();
				break;
			}

			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = current + direction;
				if (!IsWalkable(neighbor) || _blockedHazards.Contains(neighbor))
				{
					continue;
				}

				int movementCost = AgentTileRules.GetMovementCost(_mazeLayer, neighbor);
				if (movementCost == AgentTileRules.InfiniteMovementCost)
				{
					continue;
				}

				int candidateDistance = currentDistance + movementCost;
				if (_distances.TryGetValue(neighbor, out int knownDistance) &&
					knownDistance <= candidateDistance)
				{
					continue;
				}

				_distances[neighbor] = candidateDistance;
				_previousCells[neighbor] = current;
				_frontier.Enqueue((neighbor, candidateDistance),
					candidateDistance + GetHeuristic(neighbor));
				_frontierCells.Add(neighbor);
			}

			_searchOverlay.QueueRedraw();
			break;
		}

		if (_isSearching && _frontier.Count == 0)
		{
			_isSearching = false;
			_isRunning = false;
			GD.PushWarning($"{nameof(AStarAgent)} could not find a path from the entrance to the exit.");
		}
	}

	private void BuildShortestPath()
	{
		for (Vector2I cell = _goalCell; ; cell = _previousCells[cell])
		{
			_path.Add(cell);
			_pathCells.Add(cell);
			if (cell == _currentCell)
			{
				break;
			}
		}

		_path.Reverse();
		RouteCost = _distances[_goalCell];
		_isSearching = false;
		_pathIndex = 1;
		_searchOverlay.QueueRedraw();
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

	private void RestartSearch()
	{
		_currentCell = new Vector2I(0, 1);
		GlobalPosition = CellToGlobalPosition(_currentCell);
		_path.Clear();
		_pathCells.Clear();
		_frontierCells.Clear();
		_exploredCells.Clear();
		_frontier.Clear();
		_distances.Clear();
		_previousCells.Clear();
		_pathIndex = 0;
		_moveProgress = 0.0f;
		_searchTimer = 0.0f;
		_isMoving = false;
		_isSearching = true;
		_isRunning = true;
		_reachedExit = false;
		CellsExplored = 0;
		RouteCost = 0;
		_distances[_currentCell] = 0;
		_frontier.Enqueue((_currentCell, 0), GetHeuristic(_currentCell));
		_frontierCells.Add(_currentCell);
		_searchOverlay.QueueRedraw();
	}

	private int GetHeuristic(Vector2I cell)
	{
		int shortestDistance = int.MaxValue;
		foreach (Vector2I exitCell in _exitCells)
		{
			int distance = Mathf.Abs(cell.X - exitCell.X) + Mathf.Abs(cell.Y - exitCell.Y);
			shortestDistance = Mathf.Min(shortestDistance, distance);
		}

		return shortestDistance;
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

	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return _mazeLayer.ToGlobal(_mazeLayer.MapToLocal(cell));
	}
}
