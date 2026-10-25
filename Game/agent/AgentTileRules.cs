using Godot;
using System.Collections.Generic;

public static class AgentTileRules
{
	public const int TileSetSourceId = 0;
	public const int FloorMovementCost = 1;
	public const int HazardMovementCost = 10;
	public const int InfiniteMovementCost = int.MaxValue;
	public static readonly Vector2I FloorAtlasCoords = new(1, 14);
	public static readonly Vector2I HazardAtlasCoords = new(0, 0);
	public static readonly Vector2I CoinAtlasCoords = new(12, 26);

	public static bool IsWalkable(TileMapLayer mazeLayer, Vector2I cell)
	{
		if (mazeLayer.GetCellSourceId(cell) != TileSetSourceId)
		{
			return false;
		}

		Vector2I atlasCoords = mazeLayer.GetCellAtlasCoords(cell);
		return atlasCoords == FloorAtlasCoords ||
			atlasCoords == HazardAtlasCoords ||
			atlasCoords == CoinAtlasCoords;
	}

	public static bool IsHazard(TileMapLayer mazeLayer, Vector2I cell)
	{
		return mazeLayer.GetCellSourceId(cell) == TileSetSourceId &&
			mazeLayer.GetCellAtlasCoords(cell) == HazardAtlasCoords;
	}

	public static bool TriggerHazard(TileMapLayer mazeLayer, Vector2I cell)
	{
		if (!IsHazard(mazeLayer, cell))
		{
			return false;
		}

		if (mazeLayer is Foreground { UniqueHazards: true })
		{
			mazeLayer.SetCell(cell, TileSetSourceId, FloorAtlasCoords);
		}

		return true;
	}

	public static int GetMovementCost(TileMapLayer mazeLayer, Vector2I cell)
	{
		int sourceId = mazeLayer.GetCellSourceId(cell);
		if (sourceId != TileSetSourceId)
		{
			return InfiniteMovementCost;
		}

		Vector2I atlasCoords = mazeLayer.GetCellAtlasCoords(cell);
		if (atlasCoords == HazardAtlasCoords)
		{
			return HazardMovementCost;
		}

		if (atlasCoords == FloorAtlasCoords || atlasCoords == CoinAtlasCoords)
		{
			return FloorMovementCost;
		}

		return InfiniteMovementCost;
	}

	public static bool IsExit(TileMapLayer mazeLayer, Vector2I cell)
	{
		Rect2I usedRect = mazeLayer.GetUsedRect();
		return cell.X == usedRect.End.X - 1 && IsWalkable(mazeLayer, cell);
	}

	public static List<Vector2I> GetExitCells(TileMapLayer mazeLayer)
	{
		List<Vector2I> exitCells = new();
		Rect2I usedRect = mazeLayer.GetUsedRect();
		int exitX = usedRect.End.X - 1;
		for (int y = usedRect.Position.Y; y < usedRect.End.Y; y++)
		{
			Vector2I cell = new(exitX, y);
			if (IsWalkable(mazeLayer, cell))
			{
				exitCells.Add(cell);
			}
		}

		return exitCells;
	}

	public static bool CollectCoin(
		TileMapLayer mazeLayer,
		Vector2I cell,
		HashSet<Vector2I> collectedCoins)
	{
		if (mazeLayer.GetCellSourceId(cell) != TileSetSourceId ||
			mazeLayer.GetCellAtlasCoords(cell) != CoinAtlasCoords ||
			!collectedCoins.Add(cell))
		{
			return false;
		}

		if (mazeLayer is Foreground { UniqueCollectibles: true })
		{
			mazeLayer.SetCell(cell, TileSetSourceId, FloorAtlasCoords);
		}

		return true;
	}
}
