using Godot;

public partial class AStarAgent : Node2D
{
	private Vector2 _moveStart;
	private Vector2 _moveEnd;
	private bool _isMoving;
	private float _moveProgress;

	[Export(PropertyHint.Range, "0.03,1.0,0.01")]
	public float SecondsPerCell { get; set; } = 0.12f;

	[Export]
	public Color AgentColor { get; set; } = Colors.Orange;

    [Export]
	public required Foreground Maze;
    [Export]
	public required AStarOverlay Overlay;
	public Godot.Collections.Array<Vector2I> Path {get; private set;} = new();
	public int PathIndex {get; private set;}          // index in _path of the cell we are currently standing on
	public Vector2I CurrentCell;
	public int CoinsCollected;
	public bool ReachedExit;
	public float WeightScore;
	public float GameScore;
	public string AgentType = "Goal-Oriented";
	public int StepsTaken { get; private set; }
	public double ElapsedSeconds { get; private set; }
	public bool IsRunning;
	public bool RunInSimulation;

    private int _health = 4;

    public int health
    {
        get => _health;
        set {
            healthBar.Value = value;
            GD.Print($"Health changed to {value}");
            _health = value;

			if(_health <= 0) EndRun();
        }
    }

    [Export]
    public HealthBar healthBar;

	public override void _Ready()
	{
        healthBar.Value = health;
        //calls StartRun once maze and astar overlay exists
		CallDeferred(MethodName.StartRun);
	}

	public void StartRun()
	{
		
		CurrentCell = Maze.StartCell;
		GlobalPosition = CellToGlobalPosition(CurrentCell);

		StepsTaken = 0;
		ElapsedSeconds = 0;
		ReachedExit = false;

		if (!Replan())
		{
			SetProcess(false);
			return;
		}
		
		IsRunning = true;
		StepAlongPath();
	}

	//run whenever run ends, whether agent dies, completes maze, or whatever.
	public void EndRun()
	{
		GD.Print($"{AgentType} agent has ended run! logging data");
		IsRunning = false;
		CsvLogger.Append("user://results.csv", packData());

	}

    //recalcs path, call this if weight or tiles change
	public bool Replan()
	{
        //TODO: if adding more exit cells, iterate throught them all
		Path = Overlay.GeneratePath(CurrentCell, Maze.ExitCells[0]);
		PathIndex = 0; // _path[0] is the cell we're standing on

		if (Overlay != null)
		{
			Overlay.Path = Path;
			Overlay.QueueRedraw();
		}

		if (Path.Count == 0)
		{
			GD.PushWarning($"{nameof(AStarAgent)} found no path from {CurrentCell} to {Maze.ExitCells[0]}.");
			return false;
		}

		return true;
	}

	public override void _Process(double delta)
	{
		if (!IsRunning || Foreground.IsPaused)
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
			PathIndex++;
			CurrentCell = Path[PathIndex];
			StepsTaken++;
			GlobalPosition = _moveEnd;
			_isMoving = false;
			QueueRedraw();

            //stepped on coin cell
            if (Maze.CoinCells.Contains(CurrentCell))
            {
                GD.PushWarning("Moved to coin cell!");
                Overlay.EatCoin(CurrentCell);
				CoinsCollected += 1;
                Replan();
            }
            //stepped on spike cell
            if (Maze.SpikeCells.Contains(CurrentCell))
            {
                GD.PushWarning("Moved to spike cell!");
                Maze.SpikeCells.Remove(CurrentCell);  //TODO: temp, remove!
                health -= 1;
                Replan();
            }

			//stepped on exit cell
			if (CurrentCell == Maze.ExitCells[0])
			{
				ReachedExit = true;
				EndRun();
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
		int nextIndex = PathIndex + 1;
		if (nextIndex >= Path.Count)
		{
			GD.PushError("A* next index exceeds Path, ending run");
			EndRun();
			return;
		}

		//update data collection values
		WeightScore += Overlay.Grid.GetPointWeightScale(Path[nextIndex]);
		GameScore += Overlay.GetGameScoreAt(Path[nextIndex]);

		_moveStart = CellToGlobalPosition(Path[PathIndex]);
		_moveEnd = CellToGlobalPosition(Path[nextIndex]);
		_moveProgress = 0.0f;
		_isMoving = true;
	}


	private Vector2 CellToGlobalPosition(Vector2I cell)
	{
		return Maze.ToGlobal(Maze.MapToLocal(cell));
	}

	public ResearchData packData()
	{
		return new ResearchData
		{
			AgentType = AgentType,
			WeightScore = WeightScore,
			GameScore = GameScore,
			Moves = StepsTaken,
			ReachedExit = ReachedExit,
			ElapsedSeconds = ElapsedSeconds,
			RemainingHealth = _health,
			CoinsCollected = CoinsCollected,		
			Seed = Maze.Seed,
			ExitCount = Maze.ExitCells.Count,
			CoinCount = Maze.CoinCells.Count,
			HazardCount = Maze.SpikeCells.Count,
		};
	}

	public struct ResearchData
	{
		public string AgentType; 
		public float WeightScore;
		public float GameScore;
		public int Moves;	//Steps taken
		public bool ReachedExit;
		public double ElapsedSeconds;
		public int RemainingHealth;
		public int CoinsCollected;
		public int Seed;
		public int ExitCount;
		public int CoinCount;
		public int HazardCount;
	}
}