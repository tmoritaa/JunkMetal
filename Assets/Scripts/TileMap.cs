using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TileMap
{
    public int Rows
    {
        get; private set;
    }

    public int Cols
    {
        get; private set;
    }

    public float TileDim
    {
        get; private set;
    }

    private float mapWidth;
    private float mapHeight;
    
    private char[,] map;

    public TileMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls) {
        mapWidth = _mapWidth;
        mapHeight = _mapHeight;
        TileDim = _tileDim;

        // First generate map array.
        Rows = Mathf.CeilToInt(mapWidth / TileDim);
        Cols = Mathf.CeilToInt(mapHeight / TileDim);

        map = new char[Cols, Rows];
        Array.Clear(map, 0, map.Length);

        foreach (Transform trans in walls) {
            RectTransform rect = trans.GetComponent<RectTransform>();

            Vector2 size = rect.sizeDelta;

            // Note: tiledim is subtracted from x and y s.t. the map will set adjacent tiles as also blocked
            for (float x = -size.x/2f - TileDim/2f; x <= size.x / 2f + TileDim/2f; x += TileDim) {
                for (float y = -size.y/2f - TileDim/2f; y <= size.y/2f + TileDim/2f; y += TileDim) {
                    Vector2 pos = (Vector2)trans.localPosition + new Vector2(x, y);

                    int[] indices = PositionToIdx(pos);

                    if (indices[0] >= 0 && indices[0] < Cols && indices[1] >= 0 && indices[1] < Rows) {
                        map[indices[0], indices[1]] = (char)1;
                    }
                }
            }
        }
    }

    public char GetValueAtIdx(int x, int y) {
        return map[x, y];
    }

    public int[] PositionToIdx(Vector2 position) {
        int[] indices = new int[2];

        indices[0] = Mathf.FloorToInt((position.x - TileDim/2f + mapWidth/2f) / TileDim + 0.5f);
        indices[1] = Mathf.FloorToInt((-position.y - TileDim/2f + mapHeight/2f) / TileDim + 0.5f);

        return indices;
    }

    public Vector2 IdxToPosition(int x, int y) {
        return new Vector2(TileDim / 2f + x*TileDim - mapWidth / 2f, -TileDim/2f - y*TileDim + mapHeight/2f);
    }
}
