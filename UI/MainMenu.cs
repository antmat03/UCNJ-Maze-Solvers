using Godot;
using System;

public partial class MainMenu : Control
{
	private sealed record SetupSettings(
		bool Dijkstra,
		bool AStar,
		bool DeadEndFilling,
		int MazeAlgorithmIndex,
		double MazeSize,
		double ExitCount,
		double HazardCount,
		double CoinCount,
		bool UniqueCollectibles,
		bool UniqueHazards);

	private static SetupSettings _lastSetupSettings;
	private static bool _returnToSetupOnNextOpen;

	[Export]
	public Button StartSimButton;	//opens the sim options
	[Export]
	public Button ExitButton;
	[Export]
	public Button RunSimButton;	//starts the sim
	[Export]
	public CheckBox DijkstraCheckBox;
	[Export]
	public CheckBox AStarCheckBox;
	[Export]
	public CheckBox DeadEndFillingCheckBox;
	[Export]
	public OptionButton MazeAlgorithmOption;
	[Export]
	public SpinBox MazeSizeSpinBox;
	[Export]
	public SpinBox ExitCountSpinBox;
	[Export]
	public SpinBox HazardCountSpinBox;
	[Export]
	public SpinBox CoinCountSpinBox;
	[Export]
	public CheckBox UniqueCollectiblesCheckBox;
	[Export]
	public CheckBox UniqueHazardsCheckBox;
	[Export]
	public Control MainOptions;
	[Export]
	public Control SetupOptions;

	public override void _Ready()
	{
		// _mainOptions = GetNode<Control>("PanelContainer/VBoxContainer/MainOptions");
		// _setupOptions = GetNode<Control>("PanelContainer/VBoxContainer/SetupOptions");
		// StartSimButton = GetNode<Button>("PanelContainer/VBoxContainer/MainOptions/start");
		// ExitButton = GetNode<Button>("PanelContainer/VBoxContainer/MainOptions/exit");
		// RunSimButton = GetNode<Button>("PanelContainer/VBoxContainer/SetupOptions/RunSimulation");
		// DijkstraCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/Dijkstra");
		// AStarCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/AStar");
		// _deadEndFillingCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/DeadEndFilling");
		// _priorityTeamCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/PriorityTeamOptions/Enabled");
		// _mazeAlgorithmOption = GetNode<OptionButton>("PanelContainer/VBoxContainer/SetupOptions/MazeAlgorithm");
		// _priorityTeamAlgorithmOption = GetNode<OptionButton>("PanelContainer/VBoxContainer/SetupOptions/PriorityTeamOptions/Algorithm");
		// _mazeSizeSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/MazeSize");
		// _exitCountSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/ExitCount/Count");
		// _hazardCountSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/HazardCount/Count");
		// _coinCountSpinBox = GetNode<SpinBox>("PanelContainer/VBoxContainer/SetupOptions/CoinCount/Count");
		// _uniqueCollectiblesCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/UniqueCollectibles");
		// _uniqueHazardsCheckBox = GetNode<CheckBox>("PanelContainer/VBoxContainer/SetupOptions/UniqueHazards");
		// _selectionErrorLabel = GetNode<Label>("PanelContainer/VBoxContainer/SetupOptions/SelectionError");

		MazeAlgorithmOption.AddItem("Randomized Prim", (int)Foreground.MazeAlgorithm.RandomizedPrim);
		MazeAlgorithmOption.AddItem("Depth-first search", (int)Foreground.MazeAlgorithm.DepthFirstSearch);
		MazeAlgorithmOption.Select(0);
		MazeSizeSpinBox.ValueChanged += OnMazeSizeValueChanged;

		if (_returnToSetupOnNextOpen)
		{
			RestoreSetupSettings(_lastSetupSettings);
			MainOptions.Hide();
			SetupOptions.Show();
			_returnToSetupOnNextOpen = false;
		}

		StartSimButton.Pressed += ShowSetupOptions;
		ExitButton.Pressed += OnExitPressed;
		RunSimButton.Pressed += OnRunSimulationPressed;
	}

	private void ShowSetupOptions()
	{
		MainOptions.Hide();
		SetupOptions.Show();
	}

	private void ShowMainOptions()
	{
		SetupOptions.Hide();
		MainOptions.Show();
	}

	private void OnRunSimulationPressed()
	{
		_lastSetupSettings = new SetupSettings(
			DijkstraCheckBox.ButtonPressed,
			AStarCheckBox.ButtonPressed,
			DeadEndFillingCheckBox.ButtonPressed,
			MazeAlgorithmOption.Selected,
			MazeSizeSpinBox.Value,
			ExitCountSpinBox.Value,
			HazardCountSpinBox.Value,
			CoinCountSpinBox.Value,
			UniqueCollectiblesCheckBox.ButtonPressed,
			UniqueHazardsCheckBox.ButtonPressed);

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
		maze.MazeWidth = mazeSize;
		maze.MazeHeight = mazeSize;
		maze.Seed = seed;
		maze.ExitCount = (int)settings.ExitCount;
		maze.SpikeCount = (int)settings.HazardCount;
		maze.CoinCount = (int)settings.CoinCount;
		maze.UniqueCollectibles = settings.UniqueCollectibles;
		maze.UniqueHazards = settings.UniqueHazards;

		gameScene.GetNode<DijkstraAgent>("DijkstraAgent").RunInSimulation = settings.Dijkstra;
		gameScene.GetNode<AStarAgent>("AStarAgent").RunInSimulation = settings.AStar;
		gameScene.GetNode<OtherAStarAgent>("OtherAStarAgent").RunInSimulation = settings.DeadEndFilling;

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
		DijkstraCheckBox.ButtonPressed = settings.Dijkstra;
		AStarCheckBox.ButtonPressed = settings.AStar;
		DeadEndFillingCheckBox.ButtonPressed = settings.DeadEndFilling;
		MazeAlgorithmOption.Select(settings.MazeAlgorithmIndex);
		MazeSizeSpinBox.Value = settings.MazeSize;
		ExitCountSpinBox.Value = settings.ExitCount;
		HazardCountSpinBox.Value = settings.HazardCount;
		CoinCountSpinBox.Value = settings.CoinCount;
		UniqueCollectiblesCheckBox.ButtonPressed = settings.UniqueCollectibles;
		UniqueHazardsCheckBox.ButtonPressed = settings.UniqueHazards;
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
			MazeSizeSpinBox.Value = evenSize;
		}
	}

	public override void _Process(double delta)
	{
	}
}
