using Godot;

public partial class MazeInfoPanel : CanvasLayer
{
	[Export]
	public Foreground Maze;
	[Export]
	public Label DimensionsLabel ;
	[Export]
	public Label AlgorithmLabel ;
	[Export]
	public Label SeedLabel ;
	public override void _Ready()
	{
		
	}

	public override void _Process(double delta)
	{
		UpdatePanel();
	}

	private void UpdatePanel()
	{
		//kill me now
		DimensionsLabel.Text = $"Dimensions: {Maze.GeneratedSize.X} x {Maze.GeneratedSize.Y} cells";
		AlgorithmLabel.Text = $"Generator: {Maze.ActiveAlgorithm}";
		SeedLabel.Text = Maze.Seed == 0
			? $"Seed: Random ({Maze.GenerationSeed})"
			: $"Seed: {Maze.GenerationSeed}";
	}

	private static void SetStatus(Label label, string text, Color color)
	{
		label.Text = text;
		label.AddThemeColorOverride("font_color", color);
	}

}
