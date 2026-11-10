using System;
using Godot;

public partial class AgentInfo : VBoxContainer
{
    [Export]
    public AStarAgent Agent;

    [ExportGroup("Labels")]
    [Export]
    public Label Name;
    [Export]
    public HealthBar Health;
    [Export]
    public Label Status;
    [Export]
    public Label Position;
    [Export]
    public Label Steps;
    [Export]
    public Label WeightScore;
    [Export]
    public Label GameScore;
    [Export]
    public Label Coins;
    [Export]
    public Label Elapsed;
    [Export]
    public Label CoinW;
    [Export]
    public Label FloorW;
    [Export]
    public Label HazardW;
    [Export]
    public Label Interval;


    public override void _Ready()
    {
        base._Ready();
        Name.Text = Agent.AgentType.ToUpper();
        Name.AddThemeColorOverride("font_color", Agent.AgentColor);
        Agent.healthBar = Health;

    }

    public override void _Process(double delta)
    {
        base._Process(delta);

		Position.Text = $"Position: ({Agent.CurrentCell.X}, {Agent.CurrentCell.Y})";
		Steps.Text = $"Steps: {Agent.StepsTaken}";
		GameScore.Text = $"Game Score: {Agent.GameScore}";
		WeightScore.Text = $"Weight Score: {Agent.WeightScore}";
        Coins.Text = $"Coins: {Agent.CoinsCollected}";
		Elapsed.Text = $"Elapsed: {Agent.ElapsedSeconds:0.0} s";
		// _aStarPathLabel.Text =
		// 	$"Least-cost route: {Agent.RouteCost} cost, {Agent.PathLength} cells";
		// _aStarExploredLabel.Text = $"Cells explored: {Agent.CellsExplored}";
		Interval.Text = $"Step interval: {Agent.SecondsPerCell:0.00} s / cell";

        if (Agent.ReachedExit)
        {
            Status.Text = "Status: EXIT REACHED";
		    Status.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.72f));
        }else if (Agent.IsRunning)
        {

            Status.Text = "Status: ALIVE";
		    Status.AddThemeColorOverride("font_color", new Color(0.0f, 0.8f, 0.1f));
        }
        else
        {
            //dead :(

            Status.Text = "Status: DEAD :(";
		    Status.AddThemeColorOverride("font_color", new Color(1.0f, 0.48f, 0.43f));

        }

        CoinW.Text = $"Coin W:{Agent.AStarAlgo.CoinWeight}";
        FloorW.Text = $"Floor W:{Agent.AStarAlgo.FloorWeight}";
        HazardW.Text = $"Hazard W:{Agent.AStarAlgo.SpikeWeight}";

    }
}