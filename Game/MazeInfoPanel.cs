using Godot;

public partial class MazeInfoPanel : CanvasLayer
{
	private Foreground _maze = null!;
	private WallFollowingAgent _wallAgent = null!;
	private PledgeAlgorithmAgent _pledgeAgent = null!;
	private TremauxAlgoritmAgent _tremauxAgent = null!;

	private Label _dimensionsLabel = null!;
	private Label _algorithmLabel = null!;
	private Label _seedLabel = null!;
	private Label _wallStatusLabel = null!;
	private Label _wallPositionLabel = null!;
	private Label _wallStepsLabel = null!;
	private Label _wallTimeLabel = null!;
	private Label _wallRuleLabel = null!;
	private Label _wallSpeedLabel = null!;
	private Label _pledgeStatusLabel = null!;
	private Label _pledgePositionLabel = null!;
	private Label _pledgeStepsLabel = null!;
	private Label _pledgeTimeLabel = null!;
	private Label _pledgeHeadingLabel = null!;
	private Label _pledgeWallRuleLabel = null!;
	private Label _pledgeTurnSumLabel = null!;
	private Label _pledgeSpeedLabel = null!;
	private Label _tremauxStatusLabel = null!;
	private Label _tremauxPositionLabel = null!;
	private Label _tremauxStepsLabel = null!;
	private Label _tremauxTimeLabel = null!;
	private Label _tremauxPassagesLabel = null!;
	private Label _tremauxSpeedLabel = null!;

	public override void _Ready()
	{
		_maze = GetNodeOrNull<Foreground>("../foreground")!;
		_wallAgent = GetNodeOrNull<WallFollowingAgent>("../WallFollowingAgent")!;
		_pledgeAgent = GetNodeOrNull<PledgeAlgorithmAgent>("../PledgeAlgorithmAgent")!;
		_tremauxAgent = GetNodeOrNull<TremauxAlgoritmAgent>("../TremauxAlgoritmAgent")!;

		if (_maze == null || _wallAgent == null || _pledgeAgent == null || _tremauxAgent == null)
		{
			GD.PushError($"{nameof(MazeInfoPanel)} requires the maze and all three agent nodes.");
			SetProcess(false);
			return;
		}

		_dimensionsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Dimensions");
		_algorithmLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Generator");
		_seedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/Seed");
		_wallStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallStatus");
		_wallPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallPosition");
		_wallStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallSteps");
		_wallTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallElapsed");
		_wallRuleLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallRule");
		_wallSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/WallSpeed");
		_pledgeStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeStatus");
		_pledgePositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgePosition");
		_pledgeStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeSteps");
		_pledgeTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeElapsed");
		_pledgeHeadingLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgePreferredHeading");
		_pledgeWallRuleLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeWallRule");
		_pledgeTurnSumLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeTurnSum");
		_pledgeSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/PledgeSpeed");
		_tremauxStatusLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxStatus");
		_tremauxPositionLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxPosition");
		_tremauxStepsLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxSteps");
		_tremauxTimeLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxElapsed");
		_tremauxPassagesLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxPassages");
		_tremauxSpeedLabel = GetNode<Label>("PanelContainer/MarginContainer/ScrollContainer/Content/TremauxSpeed");

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
		_seedLabel.Text = _maze.Seed == 0 ? "Seed: Random" : $"Seed: {_maze.Seed}";

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
		_tremauxTimeLabel.Text = $"Elapsed: {_tremauxAgent.ElapsedSeconds:0.0} s";
		_tremauxPassagesLabel.Text = $"Marked passages: {_tremauxAgent.MarkedPassages}";
		_tremauxSpeedLabel.Text = $"Step interval: {_tremauxAgent.SecondsPerCell:0.00} s / cell";
	}

	private static void SetStatus(Label label, string text, Color color)
	{
		label.Text = text;
		label.AddThemeColorOverride("font_color", color);
	}
}
