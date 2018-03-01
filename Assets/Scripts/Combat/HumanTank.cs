using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank
{
    Dictionary<Map, List<Vector2>> prevMarkedPosesPerMap = new Dictionary<Map, List<Vector2>>();

    public void HandleInput() {
        if (initialized) {
            Wheels.HandleInput();
            Turret.HandleInput();
        }
    }

    public void MarkCurPositionAsBlockedOnMap(Map map) {
        if (!prevMarkedPosesPerMap.ContainsKey(map)) {
            prevMarkedPosesPerMap.Add(map, new List<Vector2>());
        }

        List<Vector2> prevMarkedPos = prevMarkedPosesPerMap[map];

        foreach (Vector2 vec in prevMarkedPos) {
            map.MarkPositionAsTempBlocked(vec, false);
        }
        prevMarkedPos.Clear();

        for (int x = -1; x <= 1; ++x) {
            for (int y = -1; y <= 1; ++y) {
                Vector2 pos = (Vector2)this.transform.position + new Vector2(Hull.Schematic.Size.x / 2f * (float)x, Hull.Schematic.Size.y / 2f * (float)y);
                map.MarkPositionAsTempBlocked(pos, true);
                prevMarkedPos.Add(pos);
            }
        }
    }
}
