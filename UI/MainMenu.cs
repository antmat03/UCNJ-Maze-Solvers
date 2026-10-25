using Godot;
using System;

public partial class MainMenu : Control
{
	private sealed record SetupSettings(
		bool WallFollowing,
		bool Pledge,
		bool Tremaux,
		bool Dijkstra,
		bool AStar,
		bool DeadEndFilling,
		bool PriorityTeam,
		int MazeAlgorithmIndex,
		int PriorityTeamAlgorithmIndex,
		double MazeSize,
		double ExitCount,
		double HazardCount,
		double CoinCount,
		bool UniqueCollectibles,
		bool UniqueHazards);

	private static SetupSettings _lastSetupSettings;
	private static bool _returnToSetupOnNextOpen;

	private Button _startButton = null!;
	private Button _exitButton = null!;
	private Button _runSimulationButton = null!;
	private Button _backButton = null!;
	private CheckBox _wallFollowingCheckBox = null!;
	private CheckBox _pledgeCheckBox = null!;
	private CheckBox _tremauxCheckBox = null!;
	private CheckBox _dijkstraCheckBox = null!;
	private CheckBox _aStarCheckBox = null!;
	private CheckBox _deadEndFillingCheckBox = null!;
	private CheckBox _priorityTeamCheckBox = null!;
	private OptionButton _mazeAlgorithmOption = null!;
	private OptionButton _priorityTeamAlgorithmOption = null!;
	private SpinBox _mazeSizeSpinBox = null!;
	private SpinBox _exitCountSpinBox = null!;
	private SpinBox _hazardCountSpinBox = null!;
	private SpinBox _coinCountSpinBox = null!;
	private CheckBox _uniqueCollectiblesCheckBox = null!;
	private CheckBox _uniqueHazardsCheckBox = null!;
	private Label _selectionErrorLabel = null!;
	private Control _mainOptions = null!;
	private Control _setupOptions = null!;

	public override void _Ready()
	{
		_mainOptions = GetNode<Control>("PanelContainer/VBoxContainer/MainOptions");
		_setupOptions = GetNode<Control>("PanelContainer/VBoxContainer/SetupOptions");
		_startButton = GetNode<Button>("PanelContainer/VBoxContainer/MainOptions/start");
		_exitButton = GetNode<Button>("PanelContainer/VBoxContainer/MainOptions/exit");
		_runSimulationButton = GetNode<Button>("PanelContainer/VBoxContainer/SetupOptions/RunSimulation");
		_backButton = GetNode<Button>("PanelContainer/VBoxContainer/SetupOptions/Back");
		_wallFollowingCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/WallFollowing");
		_pledgeCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/Pledge");
		_tremauxCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/Tremaux");
		_dijkstraCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/Dijkstra");
		_aStarCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/AStar");
		_deadEndFillingCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/DeadEndFilling");
		_priorityTeamCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/PriorityTeamOptions/Enabled");
		_mazeAlgorithmOption = GetNode<OptionButton>("PanelContainer/VBoxContainer/SetupOptions/MazeAlgorithm");
		_priorityTeamAlgorithmOption = GetNode<OptionButton>("PanelContainer/VBoxContainer/SetupOptions/PriorityTeamOptions/Algorithm");
		_mazeSizeSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/MazeSize");
		_exitCountSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/ExitCount/Count");
		_hazardCountSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/HazardCount/Count");
		_coinCountSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/CoinCount/Count");
		_uniqueCollectiblesCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/UniqueCollectibles");
		_uniqueHazardsCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/UniqueHazards");
		_selectionErrorLabel = GetNode<Label>("PanelContainer/VBoxContainer/SetupOptions/SelectionError");

		_mazeAlgorithmOption.AddItem("Randomized Prim", (int)Foreground.MazeAlgorithm.RandomizedPrim);
		_mazeAlgorithmOption.AddItem("Depth-first search", (int)Foreground.MazeAlgorithm.DepthFirstSearch);
		_mazeAlgorithmOption.Select(0);
		_priorityTeamAlgorithmOption.AddItem("Dijkstra", (int)PriorityAgent.SearchAlgorithm.Dijkstra);
		_priorityTeamAlgorithmOption.AddItem("A*", (int)PriorityAgent.SearchAlgorithm.AStar);
		_priorityTeamAlgorithmOption.Select(1);
		_mazeSizeSpinBox.ValueChanged += OnMazeSizeValueChanged;

		if (_returnToSetupOnNextOpen)
		{
			RestoreSetupSettings(_lastSetupSettings);
			_mainOptions.Hide();
			_setupOptions.Show();
			_returnToSetupOnNextOpen = false;
		}

		_startButton.Pressed += ShowSetupOptions;
		_exitButton.Pressed += OnExitPressed;
		_runSimulationButton.Pressed += OnRunSimulationPressed;
		_backButton.Pressed += ShowMainOptions;
	}

	private void ShowSetupOptions()
	{
		_mainOptions.Hide();
		_setupOptions.Show();
	}

	private void ShowMainOptions()
	{
		_selectionErrorLabel.Hide();
		_setupOptions.Hide();
		_mainOptions.Show();
	}

	private void OnRunSimulationPressed()
	{
		if (!_wallFollowingCheckBox.ButtonPressed &&
			!_pledgeCheckBox.ButtonPressed &&
			!_tremauxCheckBox.ButtonPressed &&
			!_dijkstraCheckBox.ButtonPressed &&
			!_aStarCheckBox.ButtonPressed &&
			!_deadEndFillingCheckBox.ButtonPressed &&
			!_priorityTeamCheckBox.ButtonPressed)
		{
			_selectionErrorLabel.Show();
			return;
		}

		_lastSetupSettings = new SetupSettings(
			_wallFollowingCheckBox.ButtonPressed,
			_pledgeCheckBox.ButtonPressed,
			_tremauxCheckBox.ButtonPressed,
			_dijkstraCheckBox.ButtonPressed,
			_aStarCheckBox.ButtonPressed,
			_deadEndFillingCheckBox.ButtonPressed,
			_priorityTeamCheckBox.ButtonPressed,
			_mazeAlgorithmOption.Selected,
			_priorityTeamAlgorithmOption.Selected,
			_mazeSizeSpinBox.Value,
			_exitCountSpinBox.Value,
			_hazardCountSpinBox.Value,
			_coinCountSpinBox.Value,
			_uniqueCollectiblesCheckBox.ButtonPressed,
			_uniqueHazardsCheckBox.ButtonPressed);

		Error error = StartSimulation(_lastSetupSettings, 0);
		if (error != Error.Ok)
		{
			GD.PushError($"Could not start the simulation: {error}.");
		}
	}

	public static Error RestartSimulation(int generationSeed)
	{
		if (_lastSetupSettings == null)
		{
			GD.PushError("Cannot restart the simulation because no setup settings have been saved.");
			return Error.Failed;
		}

		return StartSimulation(_lastSetupSettings, generationSeed);
	}

	private static Error StartSimulation(SetupSettings settings, int seed)
	{
		PackedScene mazeScene = GD.Load<PackedScene>("res://Game/Maze.tscn");
		if (mazeScene == null)
		{
			GD.PushError("Could not load the maze scene.");
			return Error.Failed;
		}

		Node gameScene = mazeScene.Instantiate();
		Foreground maze = gameScene.GetNode<Foreground>("foreground");
		maze.SelectedAlgorithm = (Foreground.MazeAlgorithm)settings.MazeAlgorithmIndex;
		int mazeSize = Mathf.RoundToInt((float)settings.MazeSize);
		// maze.MazeWidth = mazeSize;
		// maze.MazeHeight = mazeSize;
		maze.Seed = seed;
		maze.ExitCount = (int)settings.ExitCount;
		maze.HazardCount = (int)settings.HazardCount;
		maze.CoinCount = (int)settings.CoinCount;
		maze.UniqueCollectibles = settings.UniqueCollectibles;
		maze.UniqueHazards = settings.UniqueHazards;
		gameScene.GetNode<WallFollowingAgent>("WallFollowingAgent").RunInSimulation = settings.WallFollowing;
		gameScene.GetNode<PledgeAlgorithmAgent>("PledgeAlgorithmAgent").RunInSimulation = settings.Pledge;
		gameScene.GetNode<TremauxAlgoritmAgent>("TremauxAlgoritmAgent").RunInSimulation = settings.Tremaux;
		gameScene.GetNode<DijkstraAgent>("DijkstraAgent").RunInSimulation = settings.Dijkstra;
		// gameScene.GetNode<AStarAgent>("AStarAgent").RunInSimulation = settings.AStar;
		gameScene.GetNode<DeadEndFillingAgent>("DeadEndFillingAgent").RunInSimulation = settings.DeadEndFilling;
		PriorityAgent.SearchAlgorithm teamAlgorithm =
			(PriorityAgent.SearchAlgorithm)settings.PriorityTeamAlgorithmIndex;
		ConfigurePriorityAgent(
			gameScene.GetNode<PriorityAgent>("CoinPriorityAgent"),
			settings.PriorityTeam,
			teamAlgorithm,
			PriorityAgent.Priority.CollectCoins);
		ConfigurePriorityAgent(
			gameScene.GetNode<PriorityAgent>("HazardPriorityAgent"),
			settings.PriorityTeam,
			teamAlgorithm,
			PriorityAgent.Priority.AvoidHazards);
		ConfigurePriorityAgent(
			gameScene.GetNode<PriorityAgent>("TimePriorityAgent"),
			settings.PriorityTeam,
			teamAlgorithm,
			PriorityAgent.Priority.FastestTime);

		SceneTree tree = (SceneTree)Engine.GetMainLoop();
		return tree.ChangeSceneToNode(gameScene);
	}

	private static void ConfigurePriorityAgent(
		PriorityAgent agent,
		bool enabled,
		PriorityAgent.SearchAlgorithm algorithm,
		PriorityAgent.Priority priority)
	{
		agent.RunInSimulation = enabled;
		agent.Algorithm = algorithm;
		agent.AgentPriority = priority;
	}

	public static void ReturnToSetupOnNextOpen()
	{
		_returnToSetupOnNextOpen = true;
	}

	private void RestoreSetupSettings(SetupSettings settings)
	{
		_wallFollowingCheckBox.ButtonPressed = settings.WallFollowing;
		_pledgeCheckBox.ButtonPressed = settings.Pledge;
		_tremauxCheckBox.ButtonPressed = settings.Tremaux;
		_dijkstraCheckBox.ButtonPressed = settings.Dijkstra;
		_aStarCheckBox.ButtonPressed = settings.AStar;
		_deadEndFillingCheckBox.ButtonPressed = settings.DeadEndFilling;
		_priorityTeamCheckBox.ButtonPressed = settings.PriorityTeam;
		_mazeAlgorithmOption.Select(settings.MazeAlgorithmIndex);
		_priorityTeamAlgorithmOption.Select(settings.PriorityTeamAlgorithmIndex);
		_mazeSizeSpinBox.Value = settings.MazeSize;
		_exitCountSpinBox.Value = settings.ExitCount;
		_hazardCountSpinBox.Value = settings.HazardCount;
		_coinCountSpinBox.Value = settings.CoinCount;
		_uniqueCollectiblesCheckBox.ButtonPressed = settings.UniqueCollectibles;
		_uniqueHazardsCheckBox.ButtonPressed = settings.UniqueHazards;
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
	}

	private void OnMazeSizeValueChanged(double value)
	{
		int evenSize = Mathf.Clamp(Mathf.RoundToInt((float)value / 2.0f) * 2, 6, 100);
		if ((int)value != evenSize)
		{
			_mazeSizeSpinBox.Value = evenSize;
		}
	}

	public override void _Process(double delta)
	{
	}
}
