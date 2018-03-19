using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class LookaheadTree
{
    private LookaheadNode root = null;

    public void PopulateTree(Tank tank, Map map, float searchTime, float timeStep, List<float> possibleRotAngles) {
        root = new LookaheadNode(null, new Vector2(), tank.StateInfo, 0, new List<Node>());

        float elapsedTime = 0;
        List<LookaheadNode> nodesToPopulate = new List<LookaheadNode>();
        nodesToPopulate.Add(root);
        while (elapsedTime <= searchTime) {
            elapsedTime += timeStep;

            List<LookaheadNode> populatedChildren = new List<LookaheadNode>();
            foreach (LookaheadNode node in nodesToPopulate) {
                node.PopulateChildren(map, timeStep, possibleRotAngles);
                foreach (LookaheadNode childNode in node.ChildNodes) {
                    populatedChildren.Add(childNode);
                }
            }

            nodesToPopulate = populatedChildren;
        }
    }

    public List<LookaheadNode> FindAllNodesAtSearchTime(float searchTime) {
        List<LookaheadNode> nodesToSearch = new List<LookaheadNode>();
        nodesToSearch.Add(root);

        float elapsedTime = 0;
        while (elapsedTime < searchTime) {
            List<LookaheadNode> allChildNodes = new List<LookaheadNode>();
            foreach (LookaheadNode node in nodesToSearch) {
                allChildNodes.AddRange(node.ChildNodes);
            }

            if (allChildNodes.Count > 0) {
                elapsedTime += allChildNodes[0].ElapsedTimeFromParent;
                nodesToSearch = allChildNodes;
            } else {
                Debug.LogWarning("Found no nodes for search time. Should never happen");
                break;
            }
        }

        return nodesToSearch;
    }
}
