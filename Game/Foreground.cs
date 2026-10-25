using Godot;
using System;
using System.Collections.Generic;

public partial class Foreground : TileMapLayer
{
	public enum MazeAlgorithm
	{
		RandomizedPrim,
		DepthFirstSearch
	}

	public const int MazeWidth  = 48;

	public const int MazeHeight = 48;


	//our a star implementation
	[Export]
	public required AStarOverlay _aOverlay;

	//second tilemap representing items
	[Export]
	public required TileMapLayer ItemLayer;

	[Export]
	public int Seed { get; set; }

	[Export(PropertyHint.Range, "0,20,1")]
	public int HazardCount { get; set; } = 1;

	[Export(PropertyHint.Range, "0,20,1")]
	public int CoinCount { get; set; } = 1;

	[Export(PropertyHint.Range, "1,20,1")]
	public int ExitCount { get; set; } = 1;

	public bool UniqueCollectibles { get; set; }
	public bool UniqueHazards { get; set; }

	public MazeAlgorithm SelectedAlgorithm { get; set; } = MazeAlgorithm.RandomizedPrim;
	public string ActiveAlgorithm { get; private set; } = "Not generated";
	public Vector2I GeneratedSize { get; private set; }
	public int GenerationSeed { get; private set; }

	// Both atlas tiles are in the tileset source with ID 0.
	// Their atlas coordinates are (1,14) for floor and (1,19) for wall.
	private const int TileSetSourceId = 0;

	//see its 1,14 for floor and 1,19 for wall
	public readonly static Vector2I FloorAtlasCell = new(1,14);
	public readonly static Vector2I WallAtlasCell = new(1,19);
	public readonly static Vector2I SpikeAtlasCell = new(4,79);
	//its a freaking pumpkin 
	public readonly static Vector2I CoinAtlasCell = new(12,66);

	public Vector2I StartCell = new(0,1);
	//helper for just referencing all the exit cells 
	public Godot.Collections.Array<Vector2I> ExitCells = new();
	//refs for all coin cells
	public Godot.Collections.Array<Vector2I> CoinCells= new();
	//refs for all spike cells
	public Godot.Collections.Array<Vector2I> SpikeCells= new();

	[Export]
	public int NumCoins = 20;

	[Export]
	public int NumSpikes = 20;

	private static readonly Vector2I[] Directions =
	{
		new(0, -1),
		new(1, 0),
		new(0, 1),
		new(-1, 0)
	};
	public bool isFloor(Vector2I cell) => GetCellAtlasCoords(cell) == FloorAtlasCell;
	public bool isWall(Vector2I cell) => GetCellAtlasCoords(cell) == WallAtlasCell;

	public override void _Ready()
	{
		// GeneratePrim();

		GenerateBraided();
		InitItemLayer();
		

		_aOverlay.QueueRedraw();	//must call this after building grid/weight change/finding path
	}
	public void InitItemLayer()
	{
		CoinCells.Clear();
		SpikeCells.Clear();
		int coins = NumCoins;
		int spikes = NumSpikes;
		RandomNumberGenerator random = new();
		if (Seed == 0)
		{
			random.Randomize();
		}
		else
		{
			random.Seed = (ulong)Seed;
		}

		for(; coins > 0 ;)
		{
			Vector2I potentialCoinCell = new Vector2I(random.RandiRange(1, MazeWidth - 1), random.RandiRange(1,MazeHeight - 1));
			if(isFloor(potentialCoinCell)){
				//add a coin
				ItemLayer.SetCell(potentialCoinCell, TileSetSourceId, CoinAtlasCell);
				CoinCells.Add(potentialCoinCell);
				coins--;
			}
		}


		for(; spikes > 0 ;)
		{
			Vector2I potentialSpikeCell = new Vector2I(random.RandiRange(5, MazeWidth - 5), random.RandiRange(5,MazeHeight - 5));
			if (isFloor(potentialSpikeCell))
			{
				ItemLayer.SetCell(potentialSpikeCell, TileSetSourceId, SpikeAtlasCell);
				SpikeCells.Add(potentialSpikeCell);
				spikes--;
			}
		}
		
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.Keycode == Key.Escape)
		{
			MainMenu.ReturnToSetupOnNextOpen();
			Error error = GetTree().ChangeSceneToFile("res://UI/main_menu.tscn");
			if (error != Error.Ok)
			{
				GD.PushError($"Could not return to generator settings: {error}.");
			}

			GetViewport().SetInputAsHandled();
		}
		else if (keyEvent.Keycode == Key.R)
		{
			Error error = MainMenu.RestartSimulation(GenerationSeed);
			if (error != Error.Ok)
			{
				GD.PushError($"Could not restart the simulation: {error}.");
			}

			GetViewport().SetInputAsHandled();
		}
	}

	public void GenerateDepthFirst()
	{
		// 1) Normalize the requested dimensions and initialize every cell as a wall.
		ActiveAlgorithm = "Depth-first search";
		GeneratedSize = new Vector2I(MazeWidth, MazeHeight);
		bool[,] walls = new bool[MazeHeight, MazeWidth];

		for (int y = 0; y < MazeHeight; y++)
		{
			for (int x = 0; x < MazeWidth; x++)
			{
				walls[y, x] = true;
			}
		}

		// 2) Resolve one seed for this run so the full maze can be reproduced on restart.
		RandomNumberGenerator random = CreateGenerationRandom();

		// 3) Start a depth-first search at (1,1). The list acts as a stack of the current route.
		List<Vector2I> path = new() { new Vector2I(1, 1) };
		walls[1, 1] = false;

		// 4) Visit uncarved maze cells until the stack is empty.
		while (path.Count > 0)
		{
			// The last cell in the stack is the current end of the route.
			Vector2I current = path[path.Count - 1];
			List<Vector2I> unvisited = new();

			// Check cells two steps away so the cell between becomes a connecting passage.
			foreach (Vector2I direction in Directions)
			{
				Vector2I next = current + direction * 2;
				if (next.X > 0 && next.X < MazeWidth - 1 &&
					next.Y > 0 && next.Y < MazeHeight - 1 &&
					walls[next.Y, next.X])
				{
					unvisited.Add(next);
				}
			}

			// If there are no unvisited neighbors, backtrack to the previous cell on the route.
			if (unvisited.Count == 0)
			{
				path.RemoveAt(path.Count - 1);
				continue;
			}

			// Choose a random unvisited neighbor, open the intervening wall and the neighbor,
			// then continue the search from that neighbor.
			Vector2I chosen = unvisited[(int)random.RandiRange(0, unvisited.Count - 1)];
			Vector2I between = (current + chosen) / 2;
			walls[between.Y, between.X] = false;
			walls[chosen.Y, chosen.X] = false;
			path.Add(chosen);
		}

		// Add the entrance and exit, then render the wall grid into the TileMapLayer.
		OpenEntrances(walls, MazeWidth, MazeHeight);
		DrawMaze(walls, MazeWidth, MazeHeight);
	}

	public void GeneratePrim()
	{
		// 1) Normalize the requested dimensions and initialize every cell as a wall.
		ActiveAlgorithm = "Randomized Prim";
		GeneratedSize = new Vector2I(MazeWidth, MazeHeight);
		// true means wall; false means open floor.
		bool[,] walls = new bool[MazeHeight, MazeWidth];
		for (int y = 0; y < MazeHeight; y++)
		{
			for (int x = 0; x < MazeWidth; x++)
			{
				walls[y, x] = true;
			}
		}

		// 2) Resolve one seed for this run so the full maze can be reproduced on restart.
		RandomNumberGenerator random = CreateGenerationRandom();

		// 3) Start the maze at (1,1). The frontier stores uncarved cells two steps away
		//    from the currently open maze, and queued prevents adding a cell more than once.
		Vector2I start = new(1, 1);
		walls[start.Y, start.X] = false;
		List<Vector2I> frontier = new();
		bool[,] queued = new bool[MazeHeight, MazeWidth];

		// Add each in-bounds, still-walled neighbor of an open cell to the frontier.
		void AddFrontier(Vector2I cell)
		{
			foreach (Vector2I direction in Directions)
			{
				Vector2I candidate = cell + direction * 2;
				if (candidate.X > 0 && candidate.X < MazeWidth - 1 &&
					candidate.Y > 0 && candidate.Y < MazeHeight - 1 &&
					walls[candidate.Y, candidate.X] &&
					!queued[candidate.Y, candidate.X])
				{
					frontier.Add(candidate);
					queued[candidate.Y, candidate.X] = true;
				}
			}
		}

		AddFrontier(start);
		// 4) Randomized Prim grows outward until there are no frontier cells left.
		while (frontier.Count > 0)
		{
			// Remove a random frontier cell. It may already have been opened by another connection.
			int frontierIndex = (int)random.RandiRange(0, frontier.Count - 1);
			Vector2I candidate = frontier[frontierIndex];
			frontier.RemoveAt(frontierIndex);

			if (!walls[candidate.Y, candidate.X])
			{
				continue;
			}

			// Find open maze cells two steps away; one will connect this frontier cell to the maze.
			List<Vector2I> openNeighbors = new();
			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = candidate + direction * 2;
				if (neighbor.X > 0 && neighbor.X < MazeWidth - 1 &&
					neighbor.Y > 0 && neighbor.Y < MazeHeight - 1 &&
					!walls[neighbor.Y, neighbor.X])
				{
					openNeighbors.Add(neighbor);
				}
			}

			if (openNeighbors.Count == 0)
			{
				continue;
			}

			// Open the candidate and the wall between it and a randomly selected open neighbor.
			Vector2I parent = openNeighbors[(int)random.RandiRange(0, openNeighbors.Count - 1)];
			Vector2I between = (parent + candidate) / 2;
			walls[between.Y, between.X] = false;
			walls[candidate.Y, candidate.X] = false;

			// Add this newly opened cell's still-walled neighbors to the frontier.
			AddFrontier(candidate);
		}

		// Add the entrance and exit, then render the wall grid into the tile map.
		OpenEntrances(walls, MazeWidth, MazeHeight);
		DrawMaze(walls, MazeWidth, MazeHeight);
	}

	public void GenerateBraided()
	{
		ActiveAlgorithm = "Braided (loops)";
		GeneratedSize = new Vector2I(MazeWidth, MazeHeight);

		bool[,] walls = new bool[MazeHeight, MazeWidth];
		for (int y = 0; y < MazeHeight; y++)
		{
			for (int x = 0; x < MazeWidth; x++)
			{
				walls[y, x] = true;
			}
		}

		RandomNumberGenerator random = new();
		if (Seed == 0)
		{
			random.Randomize();
		}
		else
		{
			random.Seed = (ulong)Seed;
		}

		// 1) Start with a perfect maze.
		CarveDepthFirst(walls, random);

		// 2) Remove some interior walls. Cells sit on odd coordinates, so a wall tile with
		//    exactly one even coordinate sits between two cells. Tiles with both coordinates
		//    even are corner posts and stay put, which keeps the grid looking like a maze.
		int lastX = MazeWidth % 2 == 0 ? MazeWidth - 3 : MazeWidth - 2;
		int lastY = MazeHeight % 2 == 0 ? MazeHeight - 3 : MazeHeight - 2;

		for (int y = 1; y <= lastY; y++)
		{
			for (int x = 1; x <= lastX; x++)
			{
				bool betweenCells = (x % 2 == 0) != (y % 2 == 0);
				float LoopChance = 0.15f;	//TODO: temp, move to global
				if (betweenCells && walls[y, x] && random.Randf() < LoopChance)
				{
					walls[y, x] = false;
				}
			}
		}

		OpenEntrances(walls, MazeWidth, MazeHeight);
		DrawMaze(walls, MazeWidth, MazeHeight);
	}


	private void OpenEntrances(bool[,] walls, int width, int height)
	{
		ExitCells.Clear();
		// Open the left entrance and a right-side exit aligned with an inner maze row.
		walls[StartCell.Y, StartCell.X] = false;
		int exitY = height % 2 == 0 ? height - 3 : height - 2;
		walls[exitY, width - 2] = false;
		walls[exitY, width - 1] = false;

		//record exits 
		//the x's and y's are reversed which im sure will never be a problem in the future wink face
		ExitCells.Add(new(width - 1, exitY));
	}

	private void DrawMaze(bool[,] walls, int width, int height)
	{
		// Remove the previous layout, draw floor at every cell, and place walls over blocked cells.
		Clear();

		ForEachTile((int x, int y) =>
		{
			Vector2I cell = new(x, y);
			// Atlas coordinates: floor is (1,14), wall is (1,19).
			SetCell(cell, TileSetSourceId, FloorAtlasCell);
			if (walls[y, x])
			{
				SetCell(cell, TileSetSourceId, WallAtlasCell);
			}
			return true;
		});
	}

	// Same algorithm as GenerateDepthFirst, minus the setup and drawing.
	private static void CarveDepthFirst(bool[,] walls, RandomNumberGenerator random)
	{
		List<Vector2I> path = new() { new Vector2I(1, 1) };
		walls[1, 1] = false;

		while (path.Count > 0)
		{
			Vector2I current = path[path.Count - 1];
			List<Vector2I> unvisited = new();

			foreach (Vector2I direction in Directions)
			{
				Vector2I next = current + direction * 2;
				if (next.X > 0 && next.X < MazeWidth - 1 &&
					next.Y > 0 && next.Y < MazeHeight - 1 &&
					walls[next.Y, next.X])
				{
					unvisited.Add(next);
				}
			}

			if (unvisited.Count == 0)
			{
				path.RemoveAt(path.Count - 1);
				continue;
			}

			Vector2I chosen = unvisited[random.RandiRange(0, unvisited.Count - 1)];
			Vector2I between = (current + chosen) / 2;
			walls[between.Y, between.X] = false;
			walls[chosen.Y, chosen.X] = false;
			path.Add(chosen);
		}
	}

	private RandomNumberGenerator CreateGenerationRandom()
	{
		RandomNumberGenerator random = new();
		if (Seed == 0)
		{
			random.Randomize();
			GenerationSeed = unchecked((int)random.Seed);
			if (GenerationSeed == 0)
			{
				GenerationSeed = 1;
			}
		}
		else
		{
			GenerationSeed = Seed;
		}

		random.Seed = unchecked((ulong)GenerationSeed);
		return random;
	}

	//helper so we don't accidentally parse incorrectly
	public void ForEachTile(Func<int,int,bool> func)
	{
		for (int y = 0; y < MazeHeight; y++)
		{
			for (int x = 0; x < MazeWidth; x++)
			{
				func.Invoke(x,y);
			}
		}
	}

}
