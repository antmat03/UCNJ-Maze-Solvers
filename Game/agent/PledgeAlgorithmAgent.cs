using Godot;
using System.Collections.Generic;

public partial class PledgeAlgorithmAgent : Node2D
{
	public enum Heading
	{
		North,
		East,
		South,
		West
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
	public Heading PreferredHeading { get; set; } = Heading.East;

	[Export]
	public bool FollowLeftWall { get; set; }

	[Export]
	public Color AgentColor { get; set; } = Colors.DeepSkyBlue;

	public bool RunInSimulation { get; set; } = true;

	private TileMapLayer _mazeLayer = null!;
	private readonly HashSet<Vector2I> _collectedCoins = new();
	private readonly HashSet<Vector2I> _triggeredHazards = new();
	private Vector2I _currentCell;
	private Vector2I _nextCell;
	private Vector2 _moveStart;
	private Vector2 _moveEnd;
	private float _moveProgress;
	private int _directionIndex = (int)Heading.East;
	private int _turnSum;
	private bool _isFollowingWall;
	private bool _isMoving;
	private bool _isRunning;
	private bool _reachedExit;

	public Vector2I CurrentCell => _currentCell;
	public int StepsTaken { get; private set; }
	public int Score { get; private set; }
	public double ElapsedSeconds { get; private set; }
	public int TurnSum => _turnSum;
	public bool IsFollowingWall => _isFollowingWall;
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
			GD.PushError($"{nameof(PledgeAlgorithmAgent)} could not find a TileMapLayer at '{MazeLayerPath}'.");
			SetProcess(false);
			return;
		}

		// The generated maze opens (0,1) as its entrance; begin facing the preferred heading.
		_currentCell = new Vector2I(0, 1);
		_directionIndex = (int)PreferredHeading;
		if (!IsWalkable(_currentCell))
		{
			GD.PushError($"{nameof(PledgeAlgorithmAgent)} start cell {_currentCell} is not an open floor tile.");
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
				_directionIndex = (int)PreferredHeading;
				_turnSum = 0;
				_isFollowingWall = false;
				GlobalPosition = CellToGlobalPosition(_currentCell);
				ChooseNextCell();
				return;
			}

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
		int preferredDirection = (int)PreferredHeading;

		if (!_isFollowingWall)
		{
			// While unobstructed, keep moving in the fixed preferred direction.
			if (TryStartMove(preferredDirection))
			{
				return;
			}

			// An obstacle blocks the preferred route; begin following its right or left wall.
			_isFollowingWall = true;
			_turnSum = 0;
		}
		else if (_turnSum == 0 &&
			_directionIndex == preferredDirection &&
			IsWalkable(_currentCell + Directions[preferredDirection]))
		{
			// Resume the preferred route only when the accumulated turn is zero and
			// the agent is facing its preferred direction with a clear cell ahead.
			_isFollowingWall = false;
			TryStartMove(preferredDirection);
			return;
		}

		TryFollowWall();
	}

	private void TryFollowWall()
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
			if (TryStartMove(directionIndex))
			{
				return;
			}
		}

		_isRunning = false;
		GD.PushWarning($"{nameof(PledgeAlgorithmAgent)} has no walkable neighboring cell at {_currentCell}.");
	}

	private bool TryStartMove(int directionIndex)
	{
		Vector2I candidate = _currentCell + Directions[directionIndex];
		if (!IsWalkable(candidate))
		{
			return false;
		}

		if (_isFollowingWall)
		{
			_turnSum += GetSignedQuarterTurns(_directionIndex, directionIndex);
		}

		_directionIndex = directionIndex;
		_nextCell = candidate;
		_moveStart = CellToGlobalPosition(_currentCell);
		_moveEnd = CellToGlobalPosition(_nextCell);
		_moveProgress = 0.0f;
		_isMoving = true;
		return true;
	}

	private int GetSignedQuarterTurns(int fromDirection, int toDirection)
	{
		int turn = (toDirection - fromDirection + Directions.Length) % Directions.Length;
		if (turn == 1)
		{
			return 1;
		}

		if (turn == 3)
		{
			return -1;
		}

		if (turn == 2)
		{
			return FollowLeftWall ? -2 : 2;
		}

		return 0;
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
