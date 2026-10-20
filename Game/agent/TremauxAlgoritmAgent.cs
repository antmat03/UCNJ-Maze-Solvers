using Godot;
using System.Collections.Generic;

public partial class TremauxAlgoritmAgent : Node2D
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
	public Color AgentColor { get; set; } = Colors.Orange;

	public bool RunInSimulation { get; set; } = true;

	private readonly Dictionary<string, int> _passageMarks = new();
	private readonly HashSet<Vector2I> _collectedCoins = new();
	private readonly HashSet<Vector2I> _triggeredHazards = new();
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
	public int Score { get; private set; }
	public double ElapsedSeconds { get; private set; }
	public int MarkedPassages => _passageMarks.Count;
	public bool IsRunning => _isRunning;
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
			GD.PushError($"{nameof(TremauxAlgoritmAgent)} could not find a TileMapLayer at '{MazeLayerPath}'.");
			SetProcess(false);
			return;
		}

		// Start at the maze's left entrance, initially facing east.
		_currentCell = new Vector2I(0, 1);
		_directionIndex = 1;
		if (!IsWalkable(_currentCell))
		{
			GD.PushError($"{nameof(TremauxAlgoritmAgent)} start cell {_currentCell} is not an open floor tile.");
			SetProcess(false);
			return;
		}

		GlobalPosition = CellToGlobalPosition(_currentCell);
		_isRunning = true;
		ChooseNextPassage();
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
				_directionIndex = 1;
				GlobalPosition = CellToGlobalPosition(_currentCell);
				ChooseNextPassage();
				return;
			}

			if (IsAtExit())
			{
				_reachedExit = true;
				_isRunning = false;
				return;
			}

			ChooseNextPassage();
			return;
		}

		GlobalPosition = _moveStart.Lerp(_moveEnd, _moveProgress);
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 5.0f, AgentColor);
		DrawArc(Vector2.Zero, 5.0f, 0, Mathf.Tau, 24, Colors.Black, 1.0f);
	}

	private void ChooseNextPassage()
	{
		int chosenDirection = -1;
		int lowestMarkCount = 3;

		// Prefer unmarked passages, then passages marked once. Never traverse a passage more than twice.
		for (int offset = 0; offset < Directions.Length; offset++)
		{
			int directionIndex = (_directionIndex + offset) % Directions.Length;
			Vector2I neighbor = _currentCell + Directions[directionIndex];
			if (!IsWalkable(neighbor))
			{
				continue;
			}

			string key = GetPassageKey(_currentCell, neighbor);
			int marks = _passageMarks.TryGetValue(key, out int count) ? count : 0;
			if (marks < lowestMarkCount && marks < 2)
			{
				lowestMarkCount = marks;
				chosenDirection = directionIndex;
			}
		}

		if (chosenDirection < 0)
		{
			_isRunning = false;
			GD.PushWarning($"{nameof(TremauxAlgoritmAgent)} has no unexhausted passage at {_currentCell}.");
			return;
		}

		Vector2I nextCell = _currentCell + Directions[chosenDirection];
		string passageKey = GetPassageKey(_currentCell, nextCell);
		_passageMarks.TryGetValue(passageKey, out int existingMarks);
		_passageMarks[passageKey] = existingMarks + 1;

		_directionIndex = chosenDirection;
		_nextCell = nextCell;
		_moveStart = CellToGlobalPosition(_currentCell);
		_moveEnd = CellToGlobalPosition(_nextCell);
		_moveProgress = 0.0f;
		_isMoving = true;
	}

	private static string GetPassageKey(Vector2I first, Vector2I second)
	{
		// Sort endpoints so both directions through the same passage share one mark counter.
		if (first.X > second.X || (first.X == second.X && first.Y > second.Y))
		{
			(first, second) = (second, first);
		}

		return $"{first.X},{first.Y}:{second.X},{second.Y}";
	}

	private bool IsWalkable(Vector2I cell)
	{
		return !_triggeredHazards.Contains(cell) && AgentTileRules.IsWalkable(_mazeLayer, cell);
	}

	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return _mazeLayer.ToGlobal(_mazeLayer.MapToLocal(cell));
	}

	private bool IsAtExit()
	{
		return AgentTileRules.IsExit(_mazeLayer, _currentCell);
	}
}
