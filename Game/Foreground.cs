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

	[Export(PropertyHint.Range, "5,101,1")]
	public int MazeWidth { get; set; } = 16;

	[Export(PropertyHint.Range, "5,101,1")]
	public int MazeHeight { get; set; } = 16;

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
	private static readonly Vector2I HazardAtlasCoords = new(0, 0);
	private static readonly Vector2I CoinAtlasCoords = new(12, 26);
	private static readonly Vector2I FloorAtlasCoords = new(1, 14);

	private static readonly Vector2I[] Directions =
	{
		new(0, -1),
		new(1, 0),
		new(0, 1),
		new(-1, 0)
	};

	public override void _Ready()
	{
		switch (SelectedAlgorithm)
		{
			case MazeAlgorithm.RandomizedPrim:
				GeneratePrim();
				break;
			case MazeAlgorithm.DepthFirstSearch:
				GenerateDepthFirst();
				break;
			default:
				GD.PushError($"Unsupported maze algorithm: {SelectedAlgorithm}.");
				break;
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
		int width = NormalizeSize(MazeWidth);
		int height = NormalizeSize(MazeHeight);
		ActiveAlgorithm = "Depth-first search";
		GeneratedSize = new Vector2I(width, height);
		bool[,] walls = new bool[height, width];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
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
				if (next.X > 0 && next.X < width - 1 &&
					next.Y > 0 && next.Y < height - 1 &&
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
		OpenEntrances(walls, width, height, random);
		DrawMaze(walls, width, height);
	}

	public void GeneratePrim()
	{
		// 1) Normalize the requested dimensions and initialize every cell as a wall.
		int width = NormalizeSize(MazeWidth);
		int height = NormalizeSize(MazeHeight);
		ActiveAlgorithm = "Randomized Prim";
		GeneratedSize = new Vector2I(width, height);
		// true means wall; false means open floor.
		bool[,] walls = new bool[height, width];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
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
		bool[,] queued = new bool[height, width];

		// Add each in-bounds, still-walled neighbor of an open cell to the frontier.
		void AddFrontier(Vector2I cell)
		{
			foreach (Vector2I direction in Directions)
			{
				Vector2I candidate = cell + direction * 2;
				if (candidate.X > 0 && candidate.X < width - 1 &&
					candidate.Y > 0 && candidate.Y < height - 1 &&
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
				if (neighbor.X > 0 && neighbor.X < width - 1 &&
					neighbor.Y > 0 && neighbor.Y < height - 1 &&
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
		OpenEntrances(walls, width, height, random);
		DrawMaze(walls, width, height);
	}

	private void OpenEntrances(
		bool[,] walls,
		int width,
		int height,
		RandomNumberGenerator random)
	{
		// Open the left entrance and the default right-side exit.
		walls[1, 0] = false;
		int exitY = height % 2 == 0 ? height - 3 : height - 2;
		walls[exitY, width - 2] = false;
		walls[exitY, width - 1] = false;

		int requestedExitCount = Math.Max(ExitCount, 1);
		List<int> candidateRows = new();
		for (int y = 1; y < height - 1; y++)
		{
			if (y != exitY && Math.Abs(y - exitY) > 1 && !walls[y, width - 3])
			{
				candidateRows.Add(y);
			}
		}

		int generatedExitCount = 1;
		while (generatedExitCount < requestedExitCount && candidateRows.Count > 0)
		{
			int candidateIndex = (int)random.RandiRange(0, candidateRows.Count - 1);
			int candidateY = candidateRows[candidateIndex];
			candidateRows.RemoveAt(candidateIndex);

			walls[candidateY, width - 2] = false;
			walls[candidateY, width - 1] = false;
			generatedExitCount++;

			candidateRows.RemoveAll(row => Math.Abs(row - candidateY) <= 1);
		}

		if (generatedExitCount < requestedExitCount)
		{
			GD.PushWarning(
				$"Requested {requestedExitCount} exits, but only {generatedExitCount} distinct connected exits fit this maze.");
		}
	}

	private void DrawMaze(bool[,] walls, int width, int height)
	{
		// Remove the previous layout, draw floor at every cell, and place walls over blocked cells.
		Clear();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Vector2I cell = new(x, y);
				// Atlas coordinates: floor is (1,14), wall is (1,19).
				SetCell(cell, TileSetSourceId, new Vector2I(1, 14));
				if (walls[y, x])
				{
					SetCell(cell, TileSetSourceId, new Vector2I(1, 19));
				}
			}
		}

		PlaceHazardAndCoin(walls, width, height);
	}

	private void PlaceHazardAndCoin(bool[,] walls, int width, int height)
	{
		List<Vector2I> candidates = new();
		for (int y = 1; y < height - 1; y++)
		{
			for (int x = 1; x < width - 1; x++)
			{
				if (!walls[y, x])
				{
					candidates.Add(new Vector2I(x, y));
				}
			}
		}

		HashSet<Vector2I> guaranteedExitRoute =
			new(GetRouteToAnyExit(walls, width, height));
		List<Vector2I> hazardCandidates = candidates.FindAll(
			cell => !guaranteedExitRoute.Contains(cell));
		int hazardCount = Math.Max(HazardCount, 0);
		int coinCount = Math.Max(CoinCount, 0);
		if (hazardCount > hazardCandidates.Count)
		{
			GD.PushWarning(
				$"Requested {hazardCount} hazards, but only {hazardCandidates.Count} can be placed without blocking every safe exit route. " +
				$"Reducing the hazard count to {hazardCandidates.Count}.");
			hazardCount = hazardCandidates.Count;
		}

		if (coinCount > candidates.Count - hazardCount)
		{
			int availableCoinCells = Math.Max(candidates.Count - hazardCount, 0);
			GD.PushWarning(
				$"Requested {coinCount} coins, but only {availableCoinCells} interior floor cells remain. " +
				$"Reducing the coin count to {availableCoinCells}.");
			coinCount = availableCoinCells;
		}

		RandomNumberGenerator random = new()
		{
			Seed = unchecked((ulong)GenerationSeed) ^ 0x9E3779B97F4A7C15UL
		};

		for (int i = 0; i < hazardCount; i++)
		{
			int index = (int)random.RandiRange(0, hazardCandidates.Count - 1);
			Vector2I hazardCell = hazardCandidates[index];
			SetCell(hazardCell, TileSetSourceId, HazardAtlasCoords);
			hazardCandidates.RemoveAt(index);
			candidates.Remove(hazardCell);
		}

		for (int i = 0; i < coinCount; i++)
		{
			int index = (int)random.RandiRange(0, candidates.Count - 1);
			SetCell(candidates[index], TileSetSourceId, CoinAtlasCoords);
			candidates.RemoveAt(index);
		}
	}

	private static List<Vector2I> GetRouteToAnyExit(bool[,] walls, int width, int height)
	{
		Vector2I start = new(0, 1);
		Queue<Vector2I> cellsToVisit = new();
		Dictionary<Vector2I, Vector2I> previousCells = new();
		HashSet<Vector2I> visited = new() { start };
		cellsToVisit.Enqueue(start);
		Vector2I exit = default;
		bool foundExit = false;

		while (cellsToVisit.Count > 0)
		{
			Vector2I current = cellsToVisit.Dequeue();
			if (current.X == width - 1 && !walls[current.Y, current.X])
			{
				exit = current;
				foundExit = true;
				break;
			}

			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = current + direction;
				if (neighbor.X < 0 || neighbor.X >= width ||
					neighbor.Y < 0 || neighbor.Y >= height ||
					walls[neighbor.Y, neighbor.X] ||
					!visited.Add(neighbor))
				{
					continue;
				}

				previousCells[neighbor] = current;
				cellsToVisit.Enqueue(neighbor);
			}
		}

		List<Vector2I> route = new();
		if (!foundExit)
		{
			GD.PushWarning("Could not identify a guaranteed safe route while placing hazards.");
			return route;
		}

		for (Vector2I cell = exit; ; cell = previousCells[cell])
		{
			route.Add(cell);
			if (cell == start)
			{
				break;
			}
		}

		return route;
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

	private static int NormalizeSize(int size)
	{
		return Math.Max(size, 5);
	}
}
