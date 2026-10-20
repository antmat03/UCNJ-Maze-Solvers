using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class AStarOverlay : Node2D
{
	[Export] public Color WallColor = new(0.4f, 0.4f, 0.4f);
	[Export] public Color FloorColor = new(0.2f, 0.8f, 0.3f);
	[Export] public Color HeavyColor = new(0.9f, 0.15f, 0.15f);
	[Export] public Color PathColor = new(1f, 1f, 0f);
	[Export] public float MaxWeight = 100f;       // weight that maps to full HeavyColor

    [Export]
    public float CoinWeight = 1f;
    
    [Export]
    public float FloorWeight = 50f;
    [Export]
    public float SpikeWeight = 100f;
	[Export(PropertyHint.Range, "0,1")] public float Alpha = 0.6f;

	public Array<Vector2I> Path { get; set; }

    public AStarGrid2D grid;

    [Export]
	public required Foreground _maze;

    [Export]
    public required Button toggleButton;

    public Array<Vector2I> GeneratePath(Vector2I from, Vector2I to) => grid.GetIdPath(from, to);

	public override void _Ready()
    {
        toggleButton.Toggled += (bool val) => {
            _maze.Visible = !val;
            _maze.ItemLayer.Visible = !val;
            
            Visible = val;
        };		
        
        //init a star grid
        grid = new AStarGrid2D
		{
			Region = new Rect2I(0, 0, Foreground.MazeWidth, Foreground.MazeHeight),
			CellSize = Vector2.One,
			DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never
		};

		grid.Update();
		
		ForEachTile((int x, int y) =>
		{
			Vector2I cell = new(x, y);
			grid.SetPointSolid(cell, _maze.isWall(cell));

			return true;
		});

        UpdateAllWeights();

        QueueRedraw();
    }

	public override void _Draw()
	{
		if (grid == null) return;

		Vector2 tileSize = _maze.TileSet.TileSize;
		Rect2I region = grid.Region;
        ForEachTile((int x, int y) =>
        {
            Vector2I cell = new(x, y);
            Color color;
            color = grid.IsPointSolid(cell) ? WallColor : GetWeightedTileColor(cell);

            color.A = Alpha;
            DrawRect(new Rect2(new Vector2(x, y) * tileSize, tileSize), color);

            return true;
        });

        if(Path == null) return;

        Array<Vector2> pathPoints = new();
        foreach(Vector2I tile in Path)
        {
            pathPoints.Add((tile + new Vector2(0.5f, 0.5f)) * tileSize);
        }


        DrawPolyline(pathPoints.ToArray<Vector2>(), PathColor, 3f);
    }

    //sets weights for all floor tiles
    public void UpdateAllWeights()
    {
        ForEachTile((int x, int y) =>
        {
            Vector2I cell = new(x,y);
            if (_maze.CoinCells.Contains(cell))
            {
                grid.SetPointWeightScale(cell, CoinWeight);
            }
            else if (_maze.SpikeCells.Contains(cell))
            {
                grid.SetPointWeightScale(cell, SpikeWeight);
            }
            else if (_maze.isFloor(cell))
            {
                grid.SetPointWeightScale(cell, FloorWeight);
            }
            return true;
        });

    }

    public void EatCoin(Vector2I cell)
    {
        //agent takes coin
        if(!_maze.CoinCells.Contains(cell)) return;

        _maze.CoinCells.Remove(cell);
        _maze.ItemLayer.SetCell(cell);
        UpdateAllWeights();
        QueueRedraw();
    }

    // *** Different than foreground verison!
    public void ForEachTile(Func<int,int,bool> func)
    {
		for (int y = grid.Region.Position.Y; y < grid.Region.End.Y; y++)
		{
			for (int x = grid.Region.Position.X; x < grid.Region.End.X; x++)
            {
                func.Invoke(x,y);
            }
        }
    }

    //returns a color for the tile
    public Color GetWeightedTileColor(Vector2I cell)
    {
        float weight =  grid.GetPointWeightScale(cell);

        //convert cell weight to a color. green = floor, red = max cell weight
        float max_t = Math.Max(MaxWeight - 1.0f, 0.00001f);
        float t = Math.Clamp((weight - 1.0f) / max_t, 0f, 1.0f);
        return FloorColor.Lerp(HeavyColor, t);
    }
}