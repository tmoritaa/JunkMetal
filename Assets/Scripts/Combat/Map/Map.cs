using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class Map
{
    protected class NodeRecord {
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

    public float MapWidth
    {
        get; private set;
    }

    public float MapHeight
    {
        get; private set;
    }

    // NOTE: public for debug reasons
    public Node[,] MapArray
    {
        get; private set;
    }

    public Map(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls = null) {
        MapWidth = _mapWidth;
        MapHeight = _mapHeight;
        TileDim = _tileDim;

        // First generate map array.
        Rows = Mathf.CeilToInt(MapWidth / TileDim);
        Cols = Mathf.CeilToInt(MapHeight / TileDim);

        initMap();

        markWallsOnMap(walls);
    }

    public Map(Map map) {
        MapWidth = map.MapWidth;
        MapHeight = map.MapHeight;
        TileDim = map.TileDim;
        Rows = map.Rows;
        Cols = map.Cols;

        initMap();

        for (int x = 0; x < Cols; ++x) {
            for (int y = 0; y < Rows; ++y) {
                MapArray[x, y].fullyBlocked = map.MapArray[x, y].fullyBlocked;
            }
        }
    }

    public void ResetNodeValues() {
        foreach (Node n in MapArray) {
            n.ResetNodeValues();
        }
    }

    public Node GetNodeAtIdx(int x, int y) {
        return MapArray[x, y];
    }

    public int[] PositionToIdx(Vector2 position) {
        int[] indices = new int[2];

        indices[0] = Mathf.FloorToInt((position.x - TileDim / 2f + MapWidth / 2f) / TileDim + 0.5f);
        indices[1] = Mathf.FloorToInt((-position.y - TileDim / 2f + MapHeight / 2f) / TileDim + 0.5f);

        return indices;
    }

    public Node PositionToNode(Vector2 position) {
        int[] indices = PositionToIdx(position);
        return MapArray[indices[0], indices[1]];
    }

    public Vector2 NodeToPosition(Node node) {
        return IdxToPosition(node.x, node.y);
    }

    public Vector2 IdxToPosition(int x, int y) {
        return new Vector2(TileDim / 2f + x * TileDim - MapWidth / 2f, -TileDim / 2f - y * TileDim + MapHeight / 2f);
    }

    public void MarkPositionAsTempBlocked(Vector2 pos, bool block) {
        Node node = PositionToNode(pos);
        node.tempBlocked = block;
    }

    public List<Node> FindPath(Vector2 startPos, Vector2 _targetPos, int xDir = 0, int yDir = 0) {
        int[] startIdx = PositionToIdx(startPos);
        NodeRecord startNodeRecord = new NodeRecord(MapArray[startIdx[0], startIdx[1]]);

        Vector2 searchDir = (_targetPos - startPos).normalized;
        Vector2 targetPos = findNonblockedPosInProximity(_targetPos);
        int[] targetIdx = PositionToIdx(targetPos);
        NodeRecord targetNodeRecord = new NodeRecord(MapArray[targetIdx[0], targetIdx[1]]);

        startNodeRecord.estimatedTotalCost = calcHeuristicCost(startNodeRecord.node, targetNodeRecord.node);

        List<NodeRecord> openRecords = new List<NodeRecord>();
        openRecords.Add(startNodeRecord);
        List<NodeRecord> closedRecords = new List<NodeRecord>();

        NodeRecord current = openRecords[0];
        while (openRecords.Count > 0) {
            current = findMinNodeRecord(openRecords);

            if (current.node == targetNodeRecord.node) {
                break;
            }

            List<Connection> connections = FindConnectedNodes(current.node);

            foreach (Connection connection in connections) {
                Node node = connection.targetNode;

                float cost = current.costSoFar + calculateConnectionCost(connection);

                float nodeHeuristic = 0;

                NodeRecord nodeRecord;

                if (recordListContainsNode(closedRecords, node)) {
                    nodeRecord = findRecordNodeWithNode(closedRecords, node);

                    if (nodeRecord.costSoFar <= cost) {
                        continue;
                    }

                    closedRecords.Remove(nodeRecord);
                    nodeHeuristic = calcHeuristicCost(node, targetNodeRecord.node);
                } else if (recordListContainsNode(openRecords, node)) {
                    nodeRecord = findRecordNodeWithNode(openRecords, node);

                    if (nodeRecord.costSoFar <= cost) {
                        continue;
                    }

                    nodeHeuristic = calcHeuristicCost(node, targetNodeRecord.node);
                } else {
                    nodeRecord = new NodeRecord(node);

                    nodeHeuristic = calcHeuristicCost(node, targetNodeRecord.node);
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

    public List<Connection> FindConnectedNodes(Node srcNode, bool ignoreBlocked = false, int xDir = 0, int yDir = 0) {
        List<Connection> connectedNodes = new List<Connection>();

        int xStart = xDir == 0 ? -1 : xDir;
        int xEnd = xDir == 0 ? 1 : xDir;

        int yStart = yDir == 0 ? -1 : yDir;
        int yEnd = yDir == 0 ? 1 : yDir;

        for (int x = xStart; x <= xEnd; ++x) {
            for (int y = yStart; y <= yEnd; ++y) {
                if (x == 0 && y == 0) {
                    continue;
                }

                int[] indices = new int[] { srcNode.x + x, srcNode.y + y };

                // If idx positions are either invalid or blocked, then not connected, and can be skipped over.
                if (indices[0] < 0 || indices[0] >= Cols || indices[1] < 0 || indices[1] >= Rows || (!ignoreBlocked && !MapArray[indices[0], indices[1]].NodeTraversable())) {
                    continue;
                }

                connectedNodes.Add(new Connection(srcNode, MapArray[indices[0], indices[1]]));
            }
        }

        return connectedNodes;
    }

    public List<Connection> FindStraightPath(Node from, Node to, out bool wasBlocked) {
        List<Connection> pathNodes = new List<Connection>();
        Node curNode = from;
        wasBlocked = false;

        while (curNode != to) {
            int xDiff = to.x - curNode.x;
            int yDiff = to.y - curNode.y;

            int xDir = (xDiff >= yDiff) ? Math.Sign(xDiff) : 0;
            int yDir = (yDiff > xDiff) ? Math.Sign(yDiff) : 0;

            List<Connection> adjNodes = FindConnectedNodes(curNode, true, xDir, yDir);

            Connection closestConnection = null;
            foreach(Connection connection in adjNodes) {
                if (closestConnection == null) {
                    closestConnection = connection;
                } else {
                    float curClosestDiff = Math.Abs(closestConnection.targetNode.x - to.x) + Math.Abs(closestConnection.targetNode.y - to.y);
                    float conDiff = Math.Abs(connection.targetNode.x - to.x) + Math.Abs(connection.targetNode.y - to.y);

                    if (curClosestDiff > conDiff) {
                        closestConnection = connection;
                    }
                }
            }

            curNode = closestConnection.targetNode;
            pathNodes.Add(closestConnection);

            if (!closestConnection.targetNode.NodeTraversable()) {
                wasBlocked = true;
            }
        }
        
        return pathNodes;
    }

    protected virtual float calculateConnectionCost(Connection connection) {
        Vector2 pos = NodeToPosition(connection.srcNode);
        Vector2 otherPos = NodeToPosition(connection.targetNode);

        return (pos - otherPos).sqrMagnitude;
    }

    protected virtual float calcHeuristicCost(Node node, Node target) {
        Vector2 pos = NodeToPosition(node);
        Vector2 targetPos = NodeToPosition(target);

        return (pos - targetPos).sqrMagnitude;
    }

    protected virtual Node createNode(int x, int y, params object[] values) {
        return new Node(x, y);
    }

    protected Vector2 findNonblockedPosInProximity(Vector2 targetPos) {
        Node node = PositionToNode(targetPos);

        if (!node.NodeTraversable()) {
            List<Node> openNodes = new List<Node>();
            List<Node> closedNodes = new List<Node>();
            openNodes.Add(node);
            while (openNodes.Count > 0) {
                node = openNodes[0];
                List<Connection> connections = FindConnectedNodes(node, true);

                if (connections.Count == 0) {
                    Debug.Assert(false, "FindNonBlockedPosInProximity was unable to find any valid nodes");
                    break;
                }

                bool foundSafePos = false;
                foreach (Connection con in connections) {
                    if (con.targetNode.NodeTraversable()) {
                        foundSafePos = true;
                        node = con.targetNode;
                        break;
                    }
                }

                if (foundSafePos) {
                    break;
                } else {
                    foreach (Connection con in connections) {
                        if (openNodes.Find(n => n == con.targetNode) == null && closedNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add(con.targetNode);
                        }
                    }
                }

                closedNodes.Add(node);
                openNodes.RemoveAt(0);
            }
        }

        return NodeToPosition(node);
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

    protected void markWallsOnMap(List<Transform> walls) {
        if (walls == null) {
            return;
        }

        foreach (Transform trans in walls) {
            RectTransform rect = trans.GetComponent<RectTransform>();

            Vector2 size = rect.sizeDelta;

            // Note: tiledim is subtracted from x and y s.t. the map will set adjacent tiles as also blocked
            List<Node> markedNodes = new List<Node>();
            for (float x = -size.x / 2f + 1; x < size.x / 2f; x += TileDim / 2f) {
                for (float y = -size.y / 2f + 1; y < size.y / 2f; y += TileDim / 2f) {
                    Vector2 pos = (Vector2)trans.localPosition + new Vector2(x, y);

                    int[] indices = PositionToIdx(pos);

                    if (indices[0] >= 0 && indices[0] < Cols && indices[1] >= 0 && indices[1] < Rows) {
                        Node node = MapArray[indices[0], indices[1]];
                        node.fullyBlocked = true;
                        markedNodes.Add(node);
                    }
                }
            }

            foreach (Node node in markedNodes) {
                List<Connection> connections = FindConnectedNodes(node);

                foreach (Connection connection in connections) {
                    connection.targetNode.fullyBlocked = true;
                }
            }
        }
    }

    protected void initMap() {
        MapArray = new Node[Cols, Rows];
        for (int x = 0; x < Cols; ++x) {
            for (int y = 0; y < Rows; ++y) {
                MapArray[x, y] = createNode(x, y);
            }
        }
    }
}
