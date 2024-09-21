using Google.Protobuf.Protocol;
using Server.Game.Object;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Game.Room
{
    public struct Pos
    {
        public Pos(int y, int x) { Y = y; X = x; }
        public int Y;
        public int X;
    }

    public struct PQNode : IComparable<PQNode>
    {
        public int F;
        public int G;
        public int Y;
        public int X;

        public int CompareTo(PQNode other)
        {
            if (F == other.F)
                return 0;
            return F < other.F ? 1 : -1;
        }
    }

    public struct Vector2Int
    {
        public int x;
        public int y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public static Vector2Int up { get { return new Vector2Int(0, 1); } }
        public static Vector2Int down { get { return new Vector2Int(0, -1); } }
        public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
        public static Vector2Int right { get { return new Vector2Int(1, 0); } }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x + b.x, a.y + b.y);
        }

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x - b.x, a.y - b.y);
        }
        public float magnitude { get { return MathF.Sqrt(sqrMagnitude); }}
        public int sqrMagnitude { get { return (x * x + y * y); }}

        // (0, 0)에서 내가 우측으로 몇 번, 위로 몇 번만에 이동할 수 있오?
        // ex) (2, 3)이라면 우측으로 2칸 위쪽으로 3칸 만에 갈 수 있네? 5 뱉어줌. 갈수 있는 곳이네가 됨. 일단 계속 강의를 들어야 이해가 될 덧
        public int cellDistFromZeroPos { get { return Math.Abs(x) + Math.Abs(y); } }
    }


    public class Map
    {
        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        public int SizeX { get { return MaxX - MinX + 1; } }
        public int SizeY { get { return MaxY - MinY + 1; } }

        bool[,] _collisionGrid;
        GameObject[,] _objectCollisionGrid;
        public bool IsCanGo(Vector2Int cellPos, bool isCheckObjects = true)
        {
            if (cellPos.x < MinX || cellPos.x > MaxX)
                return false;
            if (cellPos.y < MinY || cellPos.y > MaxY)
                return false;
            Pos pos = Cell2Pos(cellPos);
            return !_collisionGrid[pos.Y, pos.X] && (!isCheckObjects || _objectCollisionGrid[pos.Y, pos.X] == null);
        }


        public GameObject GetGameObjectFromSpecifiedPositionOrNull(Vector2Int cellPos)
        {
            if (!IsInBoundary(cellPos))
                return null;
            Pos pos = Cell2Pos(cellPos);
            return _objectCollisionGrid[pos.Y, pos.X];
        }

        public bool ApplyLeaveFromGrid(GameObject go)
        {
            if (go.Room == null || go.Room.Map != this)
                return false;
            PositionInfo posInfo = go.PosInfo;
            Vector2Int cellPos = new Vector2Int(posInfo.PosX, posInfo.PosY);
            if (!IsInBoundary(cellPos))
                return false;
            Pos pos = Cell2Pos(cellPos);
            if (_objectCollisionGrid[pos.Y, pos.X] == go)
                _objectCollisionGrid[pos.Y, pos.X] = null;
            return true;
        }
        public bool ApplyMove(GameObject go, Vector2Int dest)
        {
            if (go.Room == null || go.Room.Map != this)
                return false;

            ApplyLeaveFromGrid(go);
            PositionInfo posInfo = go.PosInfo;
            if (IsCanGo(dest, true) == false)
                return false;
            {
                Pos pos = Cell2Pos(dest);
                _objectCollisionGrid[pos.Y, pos.X] = go;
            }

            // 이곳에서 드디어 실제 좌표 이동 
            posInfo.PosX = dest.x;
            posInfo.PosY = dest.y;
            return true;
        }

        public void LoadMap(int mapId, string pathPrefix = "../../../../../Common/MapData/")
        {
            string mapName = $"Map_{mapId.ToString("000")}";
            string text = File.ReadAllText($"{pathPrefix}{mapName}.txt");
            // Collision 관련 파일
            StringReader readear = new StringReader(text);
            MinX = int.Parse(readear.ReadLine());
            MaxX = int.Parse(readear.ReadLine());
            MinY = int.Parse(readear.ReadLine());
            MaxY = int.Parse(readear.ReadLine());

            int xCount = MaxX - MinX + 1;
            int yCount = MaxY - MinY + 1;
            _collisionGrid = new bool[yCount, xCount];
            _objectCollisionGrid = new GameObject[yCount, xCount];

            for (int y = 0; y < yCount; ++y)
            {
                string line = readear.ReadLine();
                for (int x = 0; x < xCount; ++x)
                {
                    _collisionGrid[y, x] = line[x] == '1';
                }
            }
        }

        // U D L R
        int[] _dy = new int[] { 1, -1, 0, 0 };
        int[] _dx = new int[] { 0, 0, -1, 1 };
        int[] _costs = new int[] { 10, 10, 10, 10 };

        public List<Vector2Int> FindPath(Vector2Int startCellPos, Vector2Int destCellPos, bool isCheckObjectCollision = true)
        {
            List<Pos> path = new List<Pos>();

            // 점수 매기기
            // F = G + H
            // F = 최종 점수 (작을 수록 좋음, 경로에 따라 달라짐)
            // G = 시작점에서 해당 좌표까지 이동하는데 드는 비용 (작을 수록 좋음, 경로에 따라 달라짐)
            // H = 목적지에서 얼마나 가까운지 (작을 수록 좋음, 고정)

            // (y, x) 이미 방문했는지 여부 (방문 = closed 상태)
            bool[,] closed = new bool[SizeY, SizeX]; // CloseList

            // (y, x) 가는 길을 한 번이라도 발견했는지
            // 발견X => MaxValue
            // 발견O => F = G + H
            int[,] open = new int[SizeY, SizeX]; // OpenList
            for (int y = 0; y < SizeY; y++)
                for (int x = 0; x < SizeX; x++)
                    open[y, x] = int.MaxValue;

            Pos[,] parent = new Pos[SizeY, SizeX];

            // 오픈리스트에 있는 정보들 중에서, 가장 좋은 후보를 빠르게 뽑아오기 위한 도구
            PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

            // CellPos -> ArrayPos
            Pos pos = Cell2Pos(startCellPos);
            Pos dest = Cell2Pos(destCellPos);

            // 시작점 발견 (예약 진행)
            open[pos.Y, pos.X] = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X));
            pq.Push(new PQNode() { F = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)), G = 0, Y = pos.Y, X = pos.X });
            parent[pos.Y, pos.X] = new Pos(pos.Y, pos.X);

            while (pq.Count > 0)
            {
                // 제일 좋은 후보를 찾는다
                PQNode node = pq.Pop();
                // 동일한 좌표를 여러 경로로 찾아서, 더 빠른 경로로 인해서 이미 방문(closed)된 경우 스킵
                if (closed[node.Y, node.X])
                    continue;

                // 방문한다
                closed[node.Y, node.X] = true;
                // 목적지 도착했으면 바로 종료
                if (node.Y == dest.Y && node.X == dest.X)
                    break;

                // 상하좌우 등 이동할 수 있는 좌표인지 확인해서 예약(open)한다
                for (int i = 0; i < _dy.Length; i++)
                {
                    Pos next = new Pos(node.Y + _dy[i], node.X + _dx[i]);

                    // 유효 범위를 벗어났으면 스킵
                    // 벽으로 막혀서 갈 수 없으면 스킵
                    if (next.Y != dest.Y || next.X != dest.X)
                    {
                        if (IsCanGo(Pos2Cell(next), isCheckObjectCollision) == false) // CellPos
                            continue;
                    }

                    // 이미 방문한 곳이면 스킵
                    if (closed[next.Y, next.X])
                        continue;

                    // 비용 계산
                    int g = 0;// node.G + _cost[i];
                    int h = 10 * ((dest.Y - next.Y) * (dest.Y - next.Y) + (dest.X - next.X) * (dest.X - next.X));
                    // 다른 경로에서 더 빠른 길 이미 찾았으면 스킵
                    if (open[next.Y, next.X] < g + h)
                        continue;

                    // 예약 진행
                    open[dest.Y, dest.X] = g + h;
                    pq.Push(new PQNode() { F = g + h, G = g, Y = next.Y, X = next.X });
                    parent[next.Y, next.X] = new Pos(node.Y, node.X);
                }
            }

            return CalcCellPathFromParent(parent, dest);
        }

        List<Vector2Int> CalcCellPathFromParent(Pos[,] parent, Pos dest)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            int y = dest.Y;
            int x = dest.X;

            while (parent[y, x].Y != y || parent[y, x].X != x)
            {
                cells.Add(Pos2Cell(new Pos(y, x)));
                Pos pos = parent[y, x];
                y = pos.Y;
                x = pos.X;
            }
            cells.Add(Pos2Cell(new Pos(y, x)));
            cells.Reverse();
            return cells;
        }

        Pos Cell2Pos(Vector2Int cell)
        {
            return new Pos(MaxY - cell.y, cell.x - MinX);
        }

        Vector2Int Pos2Cell(Pos pos)
        {
            return new Vector2Int(pos.X + MinX, MaxY - pos.Y);
        }

        bool IsInBoundary(Vector2Int cellPos)
        {
            if (cellPos.x < MinX || cellPos.x > MaxX)
                return false;
            if (cellPos.y < MinY || cellPos.y > MaxY)
                return false;
            return true;
        }
    }
}
