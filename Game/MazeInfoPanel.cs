using Godot;

public partial class MazeInfoPanel : CanvasLayer
{
	private Foreground _maze = null!;
	private DijkstraAgent _dijkstraAgent = null!;
	private AStarAgent _aStarAgent = null!;
	private Label _dimensionsLabel = null!;
	private Label _algorithmLabel = null!;
	private Label _seedLabel = null!;
	private Label _dijkstraStatusLabel = null!;
	private Label _dijkstraPositionLabel = null!;
	private Label _dijkstraStepsLabel = null!;
	private Label _dijkstraScoreLabel = null!;
	private Label _dijkstraTimeLabel = null!;
	private Label _dijkstraPathLabel = null!;
	private Label _dijkstraExploredLabel = null!;
	private Label _dijkstraSpeedLabel = null!;
	private Label _aStarStatusLabel = null!;
	private Label _aStarPositionLabel = null!;
	private Label _aStarStepsLabel = null!;
	private Label _aStarScoreLabel = null!;
	private Label _aStarTimeLabel = null!;
	private Label _aStarPathLabel = null!;
	private Label _aStarExploredLabel = null!;
	private Label _aStarSpeedLabel = null!;

	public override void _Ready()
	{
		_maze = GetNodeOrNull<Foreground>("../foreground")!;
		_dijkstraAgent = GetNodeOrNull<DijkstraAgent>("../DijkstraAgent")!;
		_aStarAgent = GetNodeOrNull<AStarAgent>("../AStarAgent")!;


		SetAgentSectionVisible(_dijkstraAgent.Visible, "DijkstraSeparator", "DijkstraHeading", "DijkstraStatus",
			"DijkstraLegend", "DijkstraPosition", "DijkstraSteps", "DijkstraScore", "DijkstraElapsed", "DijkstraPath",
			"DijkstraExplored", "DijkstraSpeed");
		SetAgentSectionVisible(_aStarAgent.Visible, "AStarSeparator", "AStarHeading", "AStarLegend", "AStarStatus",
			"AStarPosition", "AStarSteps", "AStarScore", "AStarElapsed", "AStarPath", "AStarExplored", "AStarSpeed");

		_dimensionsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Dimensions");
		_algorithmLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Generator");
		_seedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Seed");
		_dijkstraStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraStatus");
		_dijkstraPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraPosition");
		_dijkstraStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraSteps");
		_dijkstraScoreLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraScore");
		_dijkstraTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraElapsed");
		_dijkstraPathLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraPath");
		_dijkstraExploredLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraExplored");
		_dijkstraSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DijkstraSpeed");
		_aStarStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarStatus");
		_aStarPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarPosition");
		_aStarStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarSteps");
		_aStarScoreLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarScore");
		_aStarTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarElapsed");
		_aStarPathLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarPath");
		_aStarExploredLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarExplored");
		_aStarSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/AStarSpeed");


		UpdatePanel();
	}

	public override void _Process(double delta)
	{
		UpdatePanel();
	}

	private void UpdatePanel()
	{
		//kill me now
		_dimensionsLabel.Text = $"Dimensions: {_maze.GeneratedSize.X} x {_maze.GeneratedSize.Y} cells";
		_algorithmLabel.Text = $"Generator: {_maze.ActiveAlgorithm}";
		_seedLabel.Text = _maze.Seed == 0
			? $"Seed: Random ({_maze.GenerationSeed})"
			: $"Seed: {_maze.GenerationSeed}";

				
		if (_dijkstraAgent.ReachedExit)
		{
			SetStatus(_dijkstraStatusLabel, "Status: EXIT REACHED", new Color(0.45f, 0.95f, 0.72f));
		}
		else if (_dijkstraAgent.IsSearching)
		{
			SetStatus(_dijkstraStatusLabel, "Status: FINDING LEAST-COST PATH", new Color(1.0f, 0.78f, 0.32f));
		}
		else if (_dijkstraAgent.IsRunning)
		{
			SetStatus(_dijkstraStatusLabel, "Status: RUNNING", new Color(0.75f, 0.62f, 1.0f));
		}
		else
		{
			SetStatus(_dijkstraStatusLabel, "Status: STOPPED", new Color(1.0f, 0.48f, 0.43f));
		}

		_dijkstraPositionLabel.Text = $"Position: ({_dijkstraAgent.CurrentCell.X}, {_dijkstraAgent.CurrentCell.Y})";
		_dijkstraStepsLabel.Text = $"Steps: {_dijkstraAgent.StepsTaken}";
		_dijkstraScoreLabel.Text = $"Score: {_dijkstraAgent.Score}";
		_dijkstraTimeLabel.Text = $"Elapsed: {_dijkstraAgent.ElapsedSeconds:0.0} s";
		_dijkstraPathLabel.Text =
			$"Least-cost route: {_dijkstraAgent.RouteCost} cost, {_dijkstraAgent.PathLength} cells";
		_dijkstraExploredLabel.Text = $"Cells explored: {_dijkstraAgent.CellsExplored}";
		_dijkstraSpeedLabel.Text = $"Step interval: {_dijkstraAgent.SecondsPerCell:0.00} s / cell";

		if (_aStarAgent.ReachedExit)
		{
			SetStatus(_aStarStatusLabel, "Status: EXIT REACHED", new Color(0.45f, 0.95f, 0.72f));
		}
		// else if (_aStarAgent.IsSearching)
		// {
		// 	SetStatus(_aStarStatusLabel, "Status: FINDING LEAST-COST PATH", new Color(1.0f, 0.78f, 0.32f));
		// }
		else if (_aStarAgent.IsRunning)
		{
			SetStatus(_aStarStatusLabel, "Status: RUNNING", new Color(0.0f, 0.8f, 1.0f));
		}
		else
		{
			SetStatus(_aStarStatusLabel, "Status: STOPPED", new Color(1.0f, 0.48f, 0.43f));
		}

		_aStarPositionLabel.Text = $"Position: ({_aStarAgent.CurrentCell.X}, {_aStarAgent.CurrentCell.Y})";
		_aStarStepsLabel.Text = $"Steps: {_aStarAgent.StepsTaken}";
		_aStarScoreLabel.Text = $"Game Score: {_aStarAgent.GameScore}";
		_aStarScoreLabel.Text = $"Weight Score: {_aStarAgent.WeightScore}";
		_aStarTimeLabel.Text = $"Elapsed: {_aStarAgent.ElapsedSeconds:0.0} s";
		// _aStarPathLabel.Text =
		// 	$"Least-cost route: {_aStarAgent.RouteCost} cost, {_aStarAgent.PathLength} cells";
		// _aStarExploredLabel.Text = $"Cells explored: {_aStarAgent.CellsExplored}";
		_aStarSpeedLabel.Text = $"Step interval: {_aStarAgent.SecondsPerCell:0.00} s / cell";

	}

	private static void UpdatePriorityAgent(
		PriorityAgent agent,
		Label statusLabel,
		Label statsLabel,
		string priorityName)
	{
		string status;
		Color color;
		if (agent.ReachedExit)
		{
			status = "EXIT REACHED";
			color = new Color(0.45f, 0.95f, 0.72f);
		}
		else if (agent.IsSearching)
		{
			status = "PLANNING";
			color = new Color(1.0f, 0.78f, 0.32f);
		}
		else if (agent.IsRunning)
		{
			status = "RUNNING";
			color = new Color(0.75f, 0.82f, 1.0f);
		}
		else
		{
			status = "STOPPED";
			color = new Color(1.0f, 0.48f, 0.43f);
		}

		SetStatus(statusLabel, $"{priorityName}: {status}", color);
		statsLabel.Text =
			$"Position: ({agent.CurrentCell.X}, {agent.CurrentCell.Y})  |  Steps: {agent.StepsTaken}  |  Coins: {agent.Score}  |  Route cost: {agent.RouteCost}  |  Elapsed: {agent.ElapsedSeconds:0.0} s";
	}

	private static void SetStatus(Label label, string text, Color color)
	{
		label.Text = text;
		label.AddThemeColorOverride("font_color", color);
	}

	private void SetAgentSectionVisible(bool visible, params string[] nodeNames)
	{
		foreach (string nodeName in nodeNames)
		{
			GetNode<CanvasItem>($"PanelContainer/MarginContainer/ScrollContainer/Content/{nodeName}").Visible = visible;
		}
	}
}
