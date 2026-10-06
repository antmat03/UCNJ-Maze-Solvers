using Godot;
using System;

public partial class MainMenu : Control
{
	public Button start;
	public Button exit;	
	public OptionButton optionButton;
	public PackedScene game = GD.Load<PackedScene>("res://Game/Maze.tscn");
	public override void _Ready()
	{
		start = GetNode<Button>("PanelContainer/VBoxContainer/start");
		exit = GetNode<Button>("PanelContainer/VBoxContainer/exit");


		start.Pressed += OnStartPressed;
		exit.Pressed += OnExitPressed;
	}

    private void OnStartPressed()
    {
        var gameScene = game.Instantiate();
		GetTree().ChangeSceneToNode(gameScene);
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }

    public override void _Process(double delta)
	{
	}
}
