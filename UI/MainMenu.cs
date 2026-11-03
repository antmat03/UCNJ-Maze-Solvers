using Godot;
using System;

public partial class MainMenu : Control
{
	public struct Weights(float c, float f, float h)
	{
		public float Coin = c;
		public float Floor = f ;
		public float Hazard = h;
		public static Weights from(AgentWeightsSelector selector)
		{
			return new Weights((float)selector.CoinBox.Value, (float)selector.FloorBox.Value, (float)selector.HazardBox.Value);
		}
	}
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
		bool UniqueHazards,
		Weights GoalWeights,
		Weights CollectorWeights,
		Weights CautiousWeights,
		float SpeedInterval);

	private static SetupSettings _lastSetupSettings;
	private static bool _returnToSetupOnNextOpen;
	[ExportGroup("Weight UIs")]

	public static AgentWeightsSelector GoalSelector;
	public static AgentWeightsSelector CollectorSelector;
	public static AgentWeightsSelector CautiousSelector;

	[ExportGroup("Other UI Refs")]
	[Export]
	public Button StartSimButton;	//opens the sim options
	[Export]
	public Button ExitButton;
	[Export]
	public Button RunSimButton;	//starts the sim

	[Export]
	public CheckBox AStarCheckBox;
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
	[Export]
	public SpinBox SpeedIntervalBox;

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

		//hate this
		GoalSelector = GetNode<AgentWeightsSelector>("PanelContainer/VBoxContainer/SetupOptions/Weights/GoalAgentWeights");
		CollectorSelector = GetNode<AgentWeightsSelector>("PanelContainer/VBoxContainer/SetupOptions/Weights/CollectorAgentWeights");
		CautiousSelector= GetNode<AgentWeightsSelector>("PanelContainer/VBoxContainer/SetupOptions/Weights/CautiousAgentWeights");

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

	//TODO: add buttons for agents
	private void OnRunSimulationPressed()
	{
		_lastSetupSettings = new SetupSettings(
			false,
			true,
			false,
			MazeAlgorithmOption.Selected,
			MazeSizeSpinBox.Value,
			ExitCountSpinBox.Value,
			HazardCountSpinBox.Value,
			CoinCountSpinBox.Value,
			UniqueCollectiblesCheckBox.ButtonPressed,
			UniqueHazardsCheckBox.ButtonPressed,
			Weights.from(GoalSelector),
			Weights.from(CollectorSelector),
			Weights.from(CautiousSelector),
			(float)SpeedIntervalBox.Value);

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
		Foreground maze = gameScene.GetNode<Foreground>("MainMaze");
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

		// gameScene.GetNode<DijkstraAgent>("DijkstraAgent").RunInSimulation = settings.Dijkstra;
		// gameScene.GetNode<AStarAgent>("GoalAgent").RunInSimulation = settings.AStar;
		// gameScene.GetNode<OtherAStarAgent>("OtherAStarAgent").RunInSimulation = settings.DeadEndFilling;
		AStarAgent goal = gameScene.GetNode<AStarAgent>("GoalAgent");
		AStarAgent collector = gameScene.GetNode<AStarAgent>("CollectorAgent");
		AStarAgent cautious = gameScene.GetNode<AStarAgent>("CautiousAgent");

		goal.SetWeights(settings.GoalWeights);
		goal.SecondsPerCell = settings.SpeedInterval;
		collector.SetWeights(settings.CollectorWeights);
		collector.SecondsPerCell = settings.SpeedInterval;
		cautious.SetWeights(settings.CautiousWeights);
		cautious.SecondsPerCell = settings.SpeedInterval;

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
		// DijkstraCheckBox.ButtonPressed = settings.Dijkstra;
		// AStarCheckBox.ButtonPressed = settings.AStar;
		// DeadEndFillingCheckBox.ButtonPressed = settings.DeadEndFilling;
		MazeAlgorithmOption.Select(settings.MazeAlgorithmIndex);
		MazeSizeSpinBox.Value = settings.MazeSize;
		ExitCountSpinBox.Value = settings.ExitCount;
		HazardCountSpinBox.Value = settings.HazardCount;
		CoinCountSpinBox.Value = settings.CoinCount;
		UniqueCollectiblesCheckBox.ButtonPressed = settings.UniqueCollectibles;
		UniqueHazardsCheckBox.ButtonPressed = settings.UniqueHazards;
		GoalSelector.Restore(settings.GoalWeights);
		CollectorSelector.Restore(settings.CollectorWeights);
		CautiousSelector.Restore(settings.CautiousWeights);
		SpeedIntervalBox.Value = settings.SpeedInterval;
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
