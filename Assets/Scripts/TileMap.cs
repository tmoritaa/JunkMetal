using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class Node
{
    public int x
    {
        get; private set;
    }

    public int y
    {
        get; private set;
    }

    public char value;

    public Node(int _x, int _y, char _value) {
        x = _x;
        y = _y;
        value = _value;
    }
}

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
    
    private Node[,] map;

    private class NodeRecord
    {
        public Node node
        {
            get; private set;
        }

        public float costSoFar = 0;
        public float estimatedTotalCost = 0;
        public Connection connection = null;

        public NodeRecord(Node _node) {
            node = _node;
        }
    }

    private class Connection
    {
        public Node srcNode
        {
            get; private set;
        }

        public Node targetNode
        {
            get; private set;
        }

        public Connection(Node _srcNode, Node _tgtNode) {
            srcNode = _srcNode;
            targetNode = _tgtNode;
        }

        public float Cost(TileMap map) {
            Vector2 pos = map.NodeToPosition(srcNode);
            Vector2 otherPos = map.NodeToPosition(targetNode);

            return (pos - otherPos).sqrMagnitude;
        }
    }

    public TileMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls) {
        mapWidth = _mapWidth;
        mapHeight = _mapHeight;
        TileDim = _tileDim;

        // First generate map array.
        Rows = Mathf.CeilToInt(mapWidth / TileDim);
        Cols = Mathf.CeilToInt(mapHeight / TileDim);

        map = new Node[Cols, Rows];
        for (int x = 0; x < Cols; ++x) {
            for (int y = 0; y < Rows; ++y) {
                map[x, y] = new Node(x, y, (char)0);
            }
        }

        foreach (Transform trans in walls) {
            RectTransform rect = trans.GetComponent<RectTransform>();

            Vector2 size = rect.sizeDelta;

            // Note: tiledim is subtracted from x and y s.t. the map will set adjacent tiles as also blocked
            for (float x = -size.x/2f - TileDim/2f; x <= size.x / 2f + TileDim/2f; x += TileDim) {
                for (float y = -size.y/2f - TileDim/2f; y <= size.y/2f + TileDim/2f; y += TileDim) {
                    Vector2 pos = (Vector2)trans.localPosition + new Vector2(x, y);

                    int[] indices = PositionToIdx(pos);

                    if (indices[0] >= 0 && indices[0] < Cols && indices[1] >= 0 && indices[1] < Rows) {
                        map[indices[0], indices[1]].value = (char)1;
                    }
                }
            }
        }
    }

    public List<Node> FindPath(Vector2 startPos, Vector2 targetPos) {
        int[] startIdx = PositionToIdx(startPos);
        NodeRecord startNodeRecord = new NodeRecord(map[startIdx[0], startIdx[1]]);

        int[] targetIdx = PositionToIdx(targetPos);
        NodeRecord targetNodeRecord = new NodeRecord(map[targetIdx[0], targetIdx[1]]);

        startNodeRecord.estimatedTotalCost = heuristicCost(startNodeRecord.node, targetNodeRecord.node);

        List<NodeRecord> openRecords = new List<NodeRecord>();
        openRecords.Add(startNodeRecord);
        List<NodeRecord> closedRecords = new List<NodeRecord>();

        NodeRecord current = openRecords[0];
        while (openRecords.Count > 0) {
            current = findMinNodeRecord(openRecords);

            if (current.node == targetNodeRecord.node) {
                break;
            }

            List<Connection> connections = findConnectedNodes(current.node);

            foreach (Connection connection in connections) {
                Node node = connection.targetNode;

                float cost = current.costSoFar + connection.Cost(this);

                float nodeHeuristic = 0;

                NodeRecord nodeRecord;

                if (recordListContainsNode(closedRecords, node)) {
                    nodeRecord = findRecordNodeWithNode(closedRecords, node);

                    if (nodeRecord.costSoFar <= cost) {
                        continue;
                    }

                    closedRecords.Remove(nodeRecord);
                    nodeHeuristic = heuristicCost(node, targetNodeRecord.node);
                } else if (recordListContainsNode(openRecords, node)) {
                    nodeRecord = findRecordNodeWithNode(openRecords, node);

                    if (nodeRecord.costSoFar <= cost) {
                        continue;
                    }

                    nodeHeuristic = heuristicCost(node, targetNodeRecord.node);
                } else {
                    nodeRecord = new NodeRecord(node);

                    nodeHeuristic = heuristicCost(node, targetNodeRecord.node);
                }

                nodeRecord.costSoFar = cost;
                nodeRecord.connection = connection;
                nodeRecord.estimatedTotalCost = cost + nodeHeuristic;

                if (!openRecords.Contains(nodeRecord)) {
                    openRecords.Add(nodeRecord);
                }
            }

            openRecords.Remove(current);
            closedRecords.Add(current);
        }

        List<Node> path = new List<Node>();

        bool successful = true;
        if (current.node != targetNodeRecord.node) {
            successful = false;
        } else {
            while (current.node != startNodeRecord.node) {
                path.Add(current.node);

                if (recordListContainsNode(closedRecords, current.connection.srcNode)) {
                    current = findRecordNodeWithNode(closedRecords, current.connection.srcNode);
                } else if (recordListContainsNode(openRecords, current.connection.srcNode)) {
                    current = findRecordNodeWithNode(openRecords, current.connection.srcNode);
                } else {
                    successful = false;
                    break;
                }
            }
        }

        if (!successful) {
            Debug.Log("A* path find was not successful to find path between " + startPos + " and " + targetPos);
        }

        path.Reverse();

        return path;
    }

    private bool recordListContainsNode(List<NodeRecord> records, Node node) {
        return findRecordNodeWithNode(records, node) != null;
    }

    private NodeRecord findRecordNodeWithNode(List<NodeRecord> records, Node node) {
        return records.Find(r => r.node == node);
    }

    private NodeRecord findMinNodeRecord(List<NodeRecord> nodes) {
        NodeRecord minNode = nodes[0];

        for (int i = 1; i < nodes.Count; ++i) {
            NodeRecord node = nodes[i];

            if (node.estimatedTotalCost < minNode.estimatedTotalCost) {
                minNode = node;
            }
        }

        return minNode;
    }

    private float heuristicCost(Node node, Node target) {
        Vector2 pos = IdxToPosition(node.x, node.y);
        Vector2 targetPos = IdxToPosition(target.x, target.y);

        return (pos - targetPos).sqrMagnitude;
    }

    private List<Connection> findConnectedNodes(Node srcNode) {
        List<Connection> connectedNodes = new List<Connection>();

        for (int x = -1; x <= 1; ++x) {
            for (int y = -1; y <= 1; ++y) {
                if (x == 0 && y == 0) {
                    continue;
                }

                int[] indices = new int[] { srcNode.x + x, srcNode.y + y };

                // If idx positions are either invalid or blocked, then not connected, and can be skipped over.
                if (indices[0] < 0 || indices[0] >= Cols || indices[1] < 0 || indices[1] >= Rows || map[indices[0], indices[1]].value != 0) {
                    continue;
                }

                connectedNodes.Add(new Connection(srcNode, map[indices[0], indices[1]]));
            }
        }

        return connectedNodes;
    }

    public char GetValueAtIdx(int x, int y) {
        return map[x, y].value;
    }

    public int[] PositionToIdx(Vector2 position) {
        int[] indices = new int[2];

        indices[0] = Mathf.FloorToInt((position.x - TileDim/2f + mapWidth/2f) / TileDim + 0.5f);
        indices[1] = Mathf.FloorToInt((-position.y - TileDim/2f + mapHeight/2f) / TileDim + 0.5f);

        return indices;
    }

    public Vector2 NodeToPosition(Node node) {
        return IdxToPosition(node.x, node.y);
    }

    public Vector2 IdxToPosition(int x, int y) {
        return new Vector2(TileDim / 2f + x*TileDim - mapWidth / 2f, -TileDim/2f - y*TileDim + mapHeight/2f);
    }
}
