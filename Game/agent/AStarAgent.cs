using System.Text.RegularExpressions;
using Godot;

public partial class AStarAgent : Node2D
{
	[Export(PropertyHint.Range, "0.03,1.0,0.01")]
	public float SecondsPerCell { get; set; } = 0.12f;

	[Export]
	public Color AgentColor { get; set; } = Colors.Orange;

    [Export]
	public required Foreground _maze;
    [Export]
	public required AStarOverlay _overlay;
	public Godot.Collections.Array<Vector2I> _path {get; private set;} = new();
	public int _pathIndex {get; private set;}          // index in _path of the cell we are currently standing on
	private Vector2I _currentCell;
	private Vector2 _moveStart;
	private Vector2 _moveEnd;
	private float _moveProgress;
	private bool _isMoving;
	private bool _isRunning;
	private bool _reachedExit;

    private int _health = 4;

    public int health
    {
        get => _health;
        set {
            healthBar.Value = value;
            GD.Print($"Health changed to {value}");
            _health = value;
        }
    }

    [Export]
    public HealthBar healthBar;

	public Vector2I CurrentCell => _currentCell;
	public int StepsTaken { get; private set; }
	public double ElapsedSeconds { get; private set; }
	public bool IsRunning => _isRunning;
	public bool ReachedExit => _reachedExit;

	public override void _Ready()
	{
        healthBar.Value = health;
        //calls StartRun once maze and astar overlay exists
		CallDeferred(MethodName.StartRun);
	}

	public void StartRun()
	{

		_currentCell = _maze.StartCell;
		GlobalPosition = CellToGlobalPosition(_currentCell);

		StepsTaken = 0;
		ElapsedSeconds = 0;
		_reachedExit = false;

		if (!Replan())
		{
			SetProcess(false);
			return;
		}

		_isRunning = true;
		StepAlongPath();
	}

    //recalcs path, call this if weight or tiles change
	public bool Replan()
	{
        //TODO: if adding more exit cells, iterate throught them all
		_path = _overlay.GeneratePath(_currentCell, _maze.ExitCells[0]);
		_pathIndex = 0; // _path[0] is the cell we're standing on

		if (_overlay != null)
		{
			_overlay.Path = _path;
			_overlay.QueueRedraw();
		}

		if (_path.Count == 0)
		{
			GD.PushWarning($"{nameof(AStarAgent)} found no path from {_currentCell} to {_maze.ExitCells[0]}.");
			return false;
		}

		return true;
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
            //move to next position (logically)
			_pathIndex++;
			_currentCell = _path[_pathIndex];
			StepsTaken++;
			GlobalPosition = _moveEnd;
			_isMoving = false;
			QueueRedraw();

            //stepped on coin cell
            if (_maze.CoinCells.Contains(_currentCell))
            {
                GD.PushWarning("Moved to coin cell!");
                _overlay.EatCoin(_currentCell);
                Replan();
            }
            //stepped on spike cell
            if (_maze.SpikeCells.Contains(_currentCell))
            {
                GD.PushWarning("Moved to spike cell!");
                _maze.SpikeCells.Remove(_currentCell);  //TODO: temp, remove!
                health -= 1;
                Replan();
            }

			if (_currentCell == _maze.ExitCells[0])
			{
				_reachedExit = true;
				_isRunning = false;
				return;
			}

			StepAlongPath();
			return;
		}

		GlobalPosition = _moveStart.Lerp(_moveEnd, _moveProgress);
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 5.0f, AgentColor);
		DrawArc(Vector2.Zero, 5.0f, 0, Mathf.Tau, 24, Colors.Black, 1.0f);
	}

	// Begin moving toward the next cell on the precomputed path.
	private void StepAlongPath()
	{
		int nextIndex = _pathIndex + 1;
		if (nextIndex >= _path.Count)
		{
			_isRunning = false;
			return;
		}

		_moveStart = CellToGlobalPosition(_path[_pathIndex]);
		_moveEnd = CellToGlobalPosition(_path[nextIndex]);
		_moveProgress = 0.0f;
		_isMoving = true;
	}

	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return _maze.ToGlobal(_maze.MapToLocal(cell));
	}
}