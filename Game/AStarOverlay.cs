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

    [Export]
    public float CoinGameScore = 10f;

    [Export]
    public float SpikeGameScore = -10f;

	[Export(PropertyHint.Range, "0,1")] public float Alpha = 0.6f;

	public Array<Vector2I> Path { get; set; }

    public AStarGrid2D Grid;

    [Export]
	public required Foreground Maze;


    public Array<Vector2I> GeneratePath(Vector2I from, Vector2I to) => Grid.GetIdPath(from, to);

	public override void _Ready()
    {
        // ToggleButton.Toggled += (bool val) => {
        //     Maze.Visible = !val;
        //     Maze.ItemLayer.Visible = !val;
            
        //     Visible = val;
        // };		
        
        //init a star grid
        Grid = new AStarGrid2D
		{
			Region = new Rect2I(0, 0, Maze.MazeWidth, Maze.MazeHeight),
			CellSize = Vector2.One,
			DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never
		};

		Grid.Update();
		
		ForEachTile((int x, int y) =>
		{
			Vector2I cell = new(x, y);
			Grid.SetPointSolid(cell, Maze.isWall(cell));

			return true;
		});

        UpdateAllWeights();

        QueueRedraw();
    }

	public override void _Draw()
	{
		if (Grid == null) return;

		Vector2 tileSize = Maze.TileSet.TileSize;
		Rect2I region = Grid.Region;
        ForEachTile((int x, int y) =>
        {
            Vector2I cell = new(x, y);
            Color color;
            color = Grid.IsPointSolid(cell) ? WallColor : GetWeightedTileColor(cell);

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
            if (Maze.CoinCells.Contains(cell))
            {
                Grid.SetPointWeightScale(cell, CoinWeight);
            }
            else if (Maze.SpikeCells.Contains(cell))
            {
                Grid.SetPointWeightScale(cell, SpikeWeight);
            }
            else if (Maze.isFloor(cell))
            {
                Grid.SetPointWeightScale(cell, FloorWeight);
            }
            return true;
        });

    }

    public void EatCoin(Vector2I cell)
    {
        //agent takes coin
        if(!Maze.CoinCells.Contains(cell)) return;

        Maze.CoinCells.Remove(cell);
        Maze.ItemLayer.SetCell(cell);
        UpdateAllWeights();
        QueueRedraw();
    }

    // *** Different than foreground verison!
    public void ForEachTile(Func<int,int,bool> func)
    {
		for (int y = Grid.Region.Position.Y; y < Grid.Region.End.Y; y++)
		{
			for (int x = Grid.Region.Position.X; x < Grid.Region.End.X; x++)
            {
                func.Invoke(x,y);
            }
        }
    }

    public float GetGameScoreAt(Vector2I cell)
    {
        float score = 0f;
        if (Maze.CoinCells.Contains(cell))
        {
            score = CoinGameScore;
        }else if (Maze.SpikeCells.Contains(cell))
        {
            score = SpikeGameScore;
        }
        return score;
    }

    //returns a color for the tile
    public Color GetWeightedTileColor(Vector2I cell)
    {
        float weight =  Grid.GetPointWeightScale(cell);

        //convert cell weight to a color. green = floor, red = max cell weight
        float max_t = Math.Max(MaxWeight - 1.0f, 0.00001f);
        float t = Math.Clamp((weight - 1.0f) / max_t, 0f, 1.0f);
        return FloorColor.Lerp(HeavyColor, t);
    }
}