using Godot;
using System.Collections.Generic;

public partial class DijkstraSearchOverlay : Node2D
{
	private TileMapLayer _mazeLayer = null!;
	private IEnumerable<Vector2I> _exploredCells = System.Array.Empty<Vector2I>();
	private IEnumerable<Vector2I> _frontierCells = System.Array.Empty<Vector2I>();
	private IEnumerable<Vector2I> _pathCells = System.Array.Empty<Vector2I>();
	private Color _exploredColor;
	private Color _frontierColor;
	private Color _pathColor;

	public void Initialize(
		TileMapLayer mazeLayer,
		IEnumerable<Vector2I> exploredCells,
		IEnumerable<Vector2I> frontierCells,
		IEnumerable<Vector2I> pathCells,
		Color exploredColor,
		Color frontierColor,
		Color pathColor)
	{
		_mazeLayer = mazeLayer;
		_exploredCells = exploredCells;
		_frontierCells = frontierCells;
		_pathCells = pathCells;
		_exploredColor = exploredColor;
		_frontierColor = frontierColor;
		_pathColor = pathColor;
	}

	public override void _Draw()
	{
		if (_mazeLayer == null)
		{
			return;
		}

		Vector2I tileSize = _mazeLayer.TileSet.TileSize;
		Vector2 tileDimensions = new(tileSize.X, tileSize.Y);
		DrawCells(_exploredCells, tileDimensions, _exploredColor);
		DrawCells(_frontierCells, tileDimensions, _frontierColor);
		DrawCells(_pathCells, tileDimensions, _pathColor);
	}

	private void DrawCells(IEnumerable<Vector2I> cells, Vector2 tileDimensions, Color color)
	{
		foreach (Vector2I cell in cells)
		{
			Vector2 center = _mazeLayer.MapToLocal(cell);
			DrawRect(new Rect2(center - tileDimensions / 2.0f, tileDimensions), color);
		}
	}
}
