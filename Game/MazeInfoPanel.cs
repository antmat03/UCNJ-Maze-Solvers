using Godot;

public partial class MazeInfoPanel : CanvasLayer
{
	private Foreground _maze = null!;
	private WallFollowingAgent _wallAgent = null!;
	private PledgeAlgorithmAgent _pledgeAgent = null!;
	private TremauxAlgoritmAgent _tremauxAgent = null!;
	private DijkstraAgent _dijkstraAgent = null!;
	private AStarAgent _aStarAgent = null!;
	private DeadEndFillingAgent _deadEndFillingAgent = null!;
	private PriorityAgent _coinPriorityAgent = null!;
	private PriorityAgent _hazardPriorityAgent = null!;
	private PriorityAgent _timePriorityAgent = null!;

	private Label _dimensionsLabel = null!;
	private Label _algorithmLabel = null!;
	private Label _seedLabel = null!;
	private Label _wallStatusLabel = null!;
	private Label _wallPositionLabel = null!;
	private Label _wallStepsLabel = null!;
	private Label _wallScoreLabel = null!;
	private Label _wallTimeLabel = null!;
	private Label _wallRuleLabel = null!;
	private Label _wallSpeedLabel = null!;
	private Label _pledgeStatusLabel = null!;
	private Label _pledgePositionLabel = null!;
	private Label _pledgeStepsLabel = null!;
	private Label _pledgeScoreLabel = null!;
	private Label _pledgeTimeLabel = null!;
	private Label _pledgeHeadingLabel = null!;
	private Label _pledgeWallRuleLabel = null!;
	private Label _pledgeTurnSumLabel = null!;
	private Label _pledgeSpeedLabel = null!;
	private Label _tremauxStatusLabel = null!;
	private Label _tremauxPositionLabel = null!;
	private Label _tremauxStepsLabel = null!;
	private Label _tremauxScoreLabel = null!;
	private Label _tremauxTimeLabel = null!;
	private Label _tremauxPassagesLabel = null!;
	private Label _tremauxSpeedLabel = null!;
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
	private Label _deadEndStatusLabel = null!;
	private Label _deadEndPositionLabel = null!;
	private Label _deadEndStepsLabel = null!;
	private Label _deadEndScoreLabel = null!;
	private Label _deadEndTimeLabel = null!;
	private Label _deadEndPathLabel = null!;
	private Label _deadEndFilledLabel = null!;
	private Label _deadEndSpeedLabel = null!;
	private Label _coinPriorityStatusLabel = null!;
	private Label _coinPriorityStatsLabel = null!;
	private Label _hazardPriorityStatusLabel = null!;
	private Label _hazardPriorityStatsLabel = null!;
	private Label _timePriorityStatusLabel = null!;
	private Label _timePriorityStatsLabel = null!;

	public override void _Ready()
	{
		_maze = GetNodeOrNull<Foreground>("../foreground")!;
		_wallAgent = GetNodeOrNull<WallFollowingAgent>("../WallFollowingAgent")!;
		_pledgeAgent = GetNodeOrNull<PledgeAlgorithmAgent>("../PledgeAlgorithmAgent")!;
		_tremauxAgent = GetNodeOrNull<TremauxAlgoritmAgent>("../TremauxAlgoritmAgent")!;
		_dijkstraAgent = GetNodeOrNull<DijkstraAgent>("../DijkstraAgent")!;
		_aStarAgent = GetNodeOrNull<AStarAgent>("../AStarAgent")!;
		_deadEndFillingAgent = GetNodeOrNull<DeadEndFillingAgent>("../DeadEndFillingAgent")!;
		_coinPriorityAgent = GetNodeOrNull<PriorityAgent>("../CoinPriorityAgent")!;
		_hazardPriorityAgent = GetNodeOrNull<PriorityAgent>("../HazardPriorityAgent")!;
		_timePriorityAgent = GetNodeOrNull<PriorityAgent>("../TimePriorityAgent")!;

		if (_maze == null || _wallAgent == null || _pledgeAgent == null ||
			_tremauxAgent == null || _dijkstraAgent == null || _aStarAgent == null ||
			_deadEndFillingAgent == null || _coinPriorityAgent == null ||
			_hazardPriorityAgent == null || _timePriorityAgent == null)
		{
			GD.PushError($"{nameof(MazeInfoPanel)} requires the maze and all agent nodes.");
			SetProcess(false);
			return;
		}

		SetAgentSectionVisible(_wallAgent.Visible, "WallSeparator", "WallHeading", "WallStatus",
			"WallPosition", "WallSteps", "WallScore", "WallElapsed", "WallRule", "WallSpeed");
		SetAgentSectionVisible(_pledgeAgent.Visible, "PledgeSeparator", "PledgeHeading", "PledgeStatus",
			"PledgePosition", "PledgeSteps", "PledgeScore", "PledgeElapsed", "PledgePreferredHeading",
			"PledgeWallRule", "PledgeTurnSum", "PledgeSpeed");
		SetAgentSectionVisible(_tremauxAgent.Visible, "TremauxSeparator", "TremauxHeading", "TremauxStatus",
			"TremauxPosition", "TremauxSteps", "TremauxScore", "TremauxElapsed", "TremauxPassages", "TremauxSpeed");
		SetAgentSectionVisible(_dijkstraAgent.Visible, "DijkstraSeparator", "DijkstraHeading", "DijkstraStatus",
			"DijkstraLegend", "DijkstraPosition", "DijkstraSteps", "DijkstraScore", "DijkstraElapsed", "DijkstraPath",
			"DijkstraExplored", "DijkstraSpeed");
		SetAgentSectionVisible(_aStarAgent.Visible, "AStarSeparator", "AStarHeading", "AStarLegend", "AStarStatus",
			"AStarPosition", "AStarSteps", "AStarScore", "AStarElapsed", "AStarPath", "AStarExplored", "AStarSpeed");
		SetAgentSectionVisible(_deadEndFillingAgent.Visible, "DeadEndSeparator", "DeadEndHeading",
			"DeadEndLegend", "DeadEndStatus", "DeadEndPosition", "DeadEndSteps", "DeadEndScore", "DeadEndElapsed",
			"DeadEndPath", "DeadEndFilled", "DeadEndSpeed");
		SetAgentSectionVisible(_coinPriorityAgent.Visible, "PriorityTeamSeparator", "PriorityTeamHeading",
			"CoinPriorityStatus", "CoinPriorityStats", "HazardPriorityStatus", "HazardPriorityStats",
			"TimePriorityStatus", "TimePriorityStats");

		_dimensionsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Dimensions");
		_algorithmLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Generator");
		_seedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Seed");
		_wallStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallStatus");
		_wallPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallPosition");
		_wallStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallSteps");
		_wallScoreLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallScore");
		_wallTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallElapsed");
		_wallRuleLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallRule");
		_wallSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallSpeed");
		_pledgeStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeStatus");
		_pledgePositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgePosition");
		_pledgeStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeSteps");
		_pledgeScoreLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeScore");
		_pledgeTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeElapsed");
		_pledgeHeadingLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgePreferredHeading");
		_pledgeWallRuleLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeWallRule");
		_pledgeTurnSumLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeTurnSum");
		_pledgeSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeSpeed");
		_tremauxStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxStatus");
		_tremauxPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxPosition");
		_tremauxStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxSteps");
		_tremauxScoreLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxScore");
		_tremauxTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxElapsed");
		_tremauxPassagesLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxPassages");
		_tremauxSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxSpeed");
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
		_deadEndStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndStatus");
		_deadEndPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndPosition");
		_deadEndStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndSteps");
		_deadEndScoreLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndScore");
		_deadEndTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndElapsed");
		_deadEndPathLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndPath");
		_deadEndFilledLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndFilled");
		_deadEndSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/DeadEndSpeed");
		_coinPriorityStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/CoinPriorityStatus");
		_coinPriorityStatsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/CoinPriorityStats");
		_hazardPriorityStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/HazardPriorityStatus");
		_hazardPriorityStatsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/HazardPriorityStats");
		_timePriorityStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TimePriorityStatus");
		_timePriorityStatsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TimePriorityStats");

		UpdatePanel();
	}

	public override void _Process(double delta)
	{
		UpdatePanel();
	}

	private void UpdatePanel()
	{
		_dimensionsLabel.Text = $"Dimensions: {_maze.GeneratedSize.X} x {_maze.GeneratedSize.Y} cells";
		_algorithmLabel.Text = $"Generator: {_maze.ActiveAlgorithm}";
		_seedLabel.Text = _maze.Seed == 0
			? $"Seed: Random ({_maze.GenerationSeed})"
			: $"Seed: {_maze.GenerationSeed}";

		if (_wallAgent.ReachedExit)
		{
			SetStatus(_wallStatusLabel, "Status: EXIT REACHED", new Color(0.45f, 0.95f, 0.72f));
		}
		else if (_wallAgent.IsRunning)
		{
			SetStatus(_wallStatusLabel, "Status: RUNNING", new Color(1.0f, 0.82f, 0.36f));
		}
		else
		{
			SetStatus(_wallStatusLabel, "Status: STOPPED", new Color(1.0f, 0.48f, 0.43f));
		}

		_wallPositionLabel.Text = $"Position: ({_wallAgent.CurrentCell.X}, {_wallAgent.CurrentCell.Y})";
		_wallStepsLabel.Text = $"Steps: {_wallAgent.StepsTaken}";
		_wallScoreLabel.Text = $"Score: {_wallAgent.Score}";
		_wallTimeLabel.Text = $"Elapsed: {_wallAgent.ElapsedSeconds:0.0} s";
		_wallRuleLabel.Text = $"Wall rule: {(_wallAgent.FollowLeftWall ? "Left-hand" : "Right-hand")}";
		_wallSpeedLabel.Text = $"Step interval: {_wallAgent.SecondsPerCell:0.00} s / cell";

		if (_pledgeAgent.ReachedExit)
		{
			SetStatus(_pledgeStatusLabel, "Status: EXIT REACHED", new Color(0.45f, 0.95f, 0.72f));
		}
		else if (_pledgeAgent.IsRunning)
		{
			string status = _pledgeAgent.IsFollowingWall ? "Status: FOLLOWING WALL" : "Status: RUNNING";
			SetStatus(_pledgeStatusLabel, status, new Color(0.42f, 0.78f, 1.0f));
		}
		else
		{
			SetStatus(_pledgeStatusLabel, "Status: STOPPED", new Color(1.0f, 0.48f, 0.43f));
		}

		_pledgePositionLabel.Text = $"Position: ({_pledgeAgent.CurrentCell.X}, {_pledgeAgent.CurrentCell.Y})";
		_pledgeStepsLabel.Text = $"Steps: {_pledgeAgent.StepsTaken}";
		_pledgeScoreLabel.Text = $"Score: {_pledgeAgent.Score}";
		_pledgeTimeLabel.Text = $"Elapsed: {_pledgeAgent.ElapsedSeconds:0.0} s";
		_pledgeHeadingLabel.Text = $"Preferred heading: {_pledgeAgent.PreferredHeading}";
		_pledgeWallRuleLabel.Text = $"Wall rule: {(_pledgeAgent.FollowLeftWall ? "Left-hand" : "Right-hand")}";
		_pledgeTurnSumLabel.Text = $"Turn sum: {_pledgeAgent.TurnSum}";
		_pledgeSpeedLabel.Text = $"Step interval: {_pledgeAgent.SecondsPerCell:0.00} s / cell";

		if (_tremauxAgent.ReachedExit)
		{
			SetStatus(_tremauxStatusLabel, "Status: EXIT REACHED", new Color(0.45f, 0.95f, 0.72f));
		}
		else if (_tremauxAgent.IsRunning)
		{
			SetStatus(_tremauxStatusLabel, "Status: RUNNING", new Color(1.0f, 0.68f, 0.35f));
		}
		else
		{
			SetStatus(_tremauxStatusLabel, "Status: STOPPED", new Color(1.0f, 0.48f, 0.43f));
		}

		_tremauxPositionLabel.Text = $"Position: ({_tremauxAgent.CurrentCell.X}, {_tremauxAgent.CurrentCell.Y})";
		_tremauxStepsLabel.Text = $"Steps: {_tremauxAgent.StepsTaken}";
		_tremauxScoreLabel.Text = $"Score: {_tremauxAgent.Score}";
		_tremauxTimeLabel.Text = $"Elapsed: {_tremauxAgent.ElapsedSeconds:0.0} s";
		_tremauxPassagesLabel.Text = $"Marked passages: {_tremauxAgent.MarkedPassages}";
		_tremauxSpeedLabel.Text = $"Step interval: {_tremauxAgent.SecondsPerCell:0.00} s / cell";

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

		if (_deadEndFillingAgent.ReachedExit)
		{
			SetStatus(_deadEndStatusLabel, "Status: EXIT REACHED", new Color(0.45f, 0.95f, 0.72f));
		}
		else if (_deadEndFillingAgent.IsFilling)
		{
			SetStatus(_deadEndStatusLabel, "Status: FILLING DEAD ENDS", new Color(1.0f, 0.72f, 0.24f));
		}
		else if (_deadEndFillingAgent.IsRunning)
		{
			SetStatus(_deadEndStatusLabel, "Status: RUNNING", new Color(1.0f, 0.72f, 0.24f));
		}
		else
		{
			SetStatus(_deadEndStatusLabel, "Status: STOPPED", new Color(1.0f, 0.48f, 0.43f));
		}

		_deadEndPositionLabel.Text = $"Position: ({_deadEndFillingAgent.CurrentCell.X}, {_deadEndFillingAgent.CurrentCell.Y})";
		_deadEndStepsLabel.Text = $"Steps: {_deadEndFillingAgent.StepsTaken}";
		_deadEndScoreLabel.Text = $"Score: {_deadEndFillingAgent.Score}";
		_deadEndTimeLabel.Text = $"Elapsed: {_deadEndFillingAgent.ElapsedSeconds:0.0} s";
		_deadEndPathLabel.Text =
			$"Remaining route: {_deadEndFillingAgent.RouteCost} cost, {_deadEndFillingAgent.PathLength} cells";
		_deadEndFilledLabel.Text = $"Dead ends filled: {_deadEndFillingAgent.CellsFilled}";
		_deadEndSpeedLabel.Text = $"Step interval: {_deadEndFillingAgent.SecondsPerCell:0.00} s / cell";

		UpdatePriorityAgent(
			_coinPriorityAgent, _coinPriorityStatusLabel, _coinPriorityStatsLabel,
			"COIN PRIORITY");
		UpdatePriorityAgent(
			_hazardPriorityAgent, _hazardPriorityStatusLabel, _hazardPriorityStatsLabel,
			"HAZARD AVOIDANCE");
		UpdatePriorityAgent(
			_timePriorityAgent, _timePriorityStatusLabel, _timePriorityStatsLabel,
			"FASTEST TIME");
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
