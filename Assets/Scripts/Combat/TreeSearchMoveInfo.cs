using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TreeSearchMoveInfo
{
    public Vector2 BaseDir
    {
        get; private set;
    }

    public bool IsJet
    {
        get; private set;
    }

    public TreeSearchMoveInfo(Vector2 baseDir, bool isJet) {
        BaseDir = baseDir;
        IsJet = isJet;
    }
}
