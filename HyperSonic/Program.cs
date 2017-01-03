using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        var map = new Mapa();
        //map.GetBombBlastEffect(new Point(12,0), 3);
        map.GetPositions(new Point(0, 0), 7).ToList().ForEach(p => Console.Error.WriteLine($"Point:{p.Point} Distance:{p.Distance}"));
        map.GetBombBlastEffect(new Point(3, 2), 5);
        //map.GetPositions(new Point(0, 0), 100).Select(point => map.GetBombBlastEffect(point, 3)).OrderByDescending(be => be.BombDamage).ToList().ForEach(be => Console.WriteLine($"{be.Point} - {be.BombDamage}"));
        Console.ReadLine();
    }

    static void Main1(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]);
        int myId = int.Parse(inputs[2]);

        var playerAction = PlayerAction.Lost;
        Point optimalPoint = new Point(-1, -1);

        // game loop
        while (true)
        {
            var mapSource = new string[height];
            for (int i = 0; i < height; i++)
            {
                mapSource[i] = Console.ReadLine();
                Console.Error.WriteLine(mapSource[i]);
            }

            var entities = new List<Entity>();
            int nrOfEntities = int.Parse(Console.ReadLine());
            for (int i = 0; i < nrOfEntities; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int entityType = int.Parse(inputs[0]);
                int owner = int.Parse(inputs[1]);
                int x = int.Parse(inputs[2]);
                int y = int.Parse(inputs[3]);
                int param1 = int.Parse(inputs[4]);
                int param2 = int.Parse(inputs[5]);
                entities.Add(new Entity(entityType, owner, x, y, param1, param2));
            }


            IMap map = new Map(width, height, mapSource, entities, myId);

            if (playerAction == PlayerAction.Lost)
            {
                optimalPoint = GetOptimalPoint(map);
                Console.Error.WriteLine("d");
                Console.WriteLine($"MOVE {optimalPoint.X} {optimalPoint.Y}");
                playerAction = PlayerAction.Walking;
            }
            else if (playerAction == PlayerAction.Walking)
            {
                playerAction = PlayerIsWalking(playerAction, ref optimalPoint, map, myId);
            }
            Console.Error.WriteLine($"Optimal: {optimalPoint}");
        }
    }

    private static PlayerAction PlayerIsWalking(PlayerAction action, ref Point optimalPoint, IMap map, int myId)
    {
        if (map.Me.Point.Equals(optimalPoint) && map.Me.Param1 > 0)
        {
            var optimal = GetOptimalPoint(map);
            if (optimal.Equals(optimalPoint))
            {
                Console.Error.WriteLine("a");
                Console.WriteLine($"BOMB {optimalPoint.X} {optimalPoint.Y}");
                return PlayerAction.Lost;
            }
            else
            {
                Console.Error.WriteLine("b");
                optimalPoint = optimal;
                Console.WriteLine($"MOVE {optimalPoint.X} {optimalPoint.Y}");
                return action;
            }
        }
        else
        {
            Console.Error.WriteLine("c");
            Console.WriteLine($"MOVE {optimalPoint.X} {optimalPoint.Y}");
            return action;
        }
    }

    private static Point GetOptimalPoint(IMap map)
    {
        Point optimalPoint;
        var blastPoints = map.GetPositions(map.Me.Point, 7).Select(pad => new { BlastEffect = map.GetBombBlastEffect(pad.Point, map.Me.Param2), Distance = pad.Distance }).OrderByDescending(bead => bead.BlastEffect.BombDamage).ThenBy(bead => bead.Distance).ToList();
        blastPoints.ToList().ForEach(bpd => Console.Error.WriteLine($"be:{bpd.BlastEffect.BombDamage}, po:{bpd.BlastEffect.Point}, d:{bpd.Distance}"));
        if (blastPoints.First().BlastEffect.BombDamage == 0)
        {
            blastPoints = map.GetPositions(map.Me.Point, 100).Select(pad => new { BlastEffect = map.GetBombBlastEffect(pad.Point, map.Me.Param2), Distance = pad.Distance }).OrderByDescending(bead => bead.BlastEffect.BombDamage).ThenBy(bead => bead.Distance).ToList();
        }
        optimalPoint = map.Me.Param1 > 0 ? blastPoints.First().BlastEffect.Point : blastPoints.Skip(1).First().BlastEffect.Point;
        return optimalPoint;
    }
}
enum PlayerAction
{
    Walking,
    Lost
}

class Entity
{
    public EntityType EntityType { get; }
    public int Owner { get; }
    public Point Point { get; }
    public int Param1 { get; }
    public int Param2 { get; }

    public Entity(int entityType, int owner, int x, int y, int param1, int param2)
    {
        EntityType = entityType == 0 ? EntityType.Player : entityType == 1 ? EntityType.Bomb : EntityType.Item;
        Owner = owner;
        Point = new Point(x, y);
        Param1 = param1;
        Param2 = param2;
    }
}

interface IMap
{
    BlastEffect GetBombBlastEffect(Point o, int blastRadius);
    List<PointAndDistance> GetPositions(Point point, int radius);
    IEnumerable<Entity> GetMyBombs();
    Entity Me { get; }
}

class Map : IMap
{
    private Tile[,] map;
    List<Point> boxes = new List<Point>();
    public IEnumerable<Entity> Entities { get; private set; }
    private Entity me;

    public Map(int width, int height, string[] mapSource, IEnumerable<Entity> entities, int myId)
    {
        map = new Tile[width, height];
        for (int y = 0; y < mapSource.Length; y++)
        {
            var row = mapSource[y].ToCharArray();
            for (int x = 0; x < row.Length; x++)
            {
                var tileChar = row[x];
                var tilePosition = new Point(x, y);
                var tile = new Tile(row[x].GetTile(), new List<Tile>(), tilePosition, entities.Where(entity => entity.Point.Equals(tilePosition)).ToList());
                map[x, y] = tile;
                if (tile.TileType == TileType.Box)
                {
                    boxes.Add(new Point(x, y));
                }
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x > 0)
                    map[x, y].Neighbors.Add(map[x - 1, y]);
                if (x < width - 1)
                    map[x, y].Neighbors.Add(map[x + 1, y]);
                if (y > 0)
                    map[x, y].Neighbors.Add(map[x, y - 1]);
                if (y < height - 1)
                    map[x, y].Neighbors.Add(map[x, y + 1]);
            }
        }
        me = entities.Where(entity => entity.EntityType == EntityType.Player && entity.Owner == myId).FirstOrDefault();
        Entities = entities.Except(new[] { me });
    }
    public BlastEffect GetBombBlastEffect(Point o, int blastRadius)
    {
        var bombs = 0;
        bombs += GetBombEffectLeft(o, blastRadius) ? 1 : 0;
        bombs += GetBombRight(o, blastRadius) ? 1 : 0;
        bombs += GetBombTop(o, blastRadius) ? 1 : 0;
        bombs += GetBombDown(o, blastRadius) ? 1 : 0;

        return new BlastEffect(o, bombs);
    }

    public List<PointAndDistance> GetPositions(Point point, int radius)
    {
        var tile = map[point.X, point.Y];
        var list = new List<PointAndDistance>() { new PointAndDistance(point, 0) };
        return GetPositionsRec(tile, list, 1, radius);
    }

    private List<PointAndDistance> GetPositionsRec(Tile tile, List<PointAndDistance> list, int depth, int radius)
    {
        if (depth == radius)
        {
            return list;
        }
        else
        {
            tile.Neighbors.ForEach(t =>
            {
                if (t.TileType == TileType.Empty && !list.Exists(pad => pad.Point.Equals(t.Position)))
                {
                    list.Add(new PointAndDistance(t.Position, depth));
                    GetPositionsRec(t, list, depth + 1, radius);
                }
            });
            return list;
        }
    }

    public List<PointAndDistance> GetPositions1(Point o, int radius)
    {
        var leftBound = Math.Max(0, o.X - radius);
        var rightBound = Math.Min(o.X + radius, map.GetLength(0));

        var topBound = Math.Max(0, o.Y - radius);
        var downBound = Math.Min(o.Y + radius, map.GetLength(1));

        var points = new List<Point>();
        for (int x = leftBound; x < rightBound; x++)
        {
            for (int y = topBound; y < downBound; y++)
            {
                points.Add(new Point(x, y));
            }
        }
        var ps = points.Where(point => map[point.X, point.Y].TileType != TileType.Box).Except(Entities.Where(entity => entity.EntityType != EntityType.Item).Select(entity => entity.Point).ToList());
        //        ps.ToList().ForEach(p=>Console.Error.WriteLine(p));
        return ps.Select(point => new PointAndDistance(point, GetDistance(o, point))).ToList();
    }

    private int GetDistance(Point p, Point p1)
    {
        var distance = Math.Abs(p.X - p1.X) + Math.Abs(p.Y - p1.Y);
        return distance;
    }

    private bool GetBombEffect(Func<int> init, Func<int, bool> condition, Func<int, int> modifier, Func<int, TileType> getTileType)
    {
        for (int i = init(); condition(i); i = modifier(i))
        {
            var tileType = getTileType(i);
            if (tileType == TileType.Box)
                return true;
            if (tileType == TileType.Wall)
                return false;
        }
        return false;
    }

    private bool GetBombEffectLeft(Point o, int blastRadius)
    {
        var left = Math.Max(0, o.X - blastRadius);
        return GetBombEffect(() => o.X, x => x >= left, x => x - 1, x => map[x, o.Y].TileType);
    }

    private bool GetBombRight(Point o, int blastRadius)
    {
        var right = Math.Min(o.X + blastRadius, map.GetLength(0) - 1);
        return GetBombEffect(() => o.X, x => x <= right, x => x + 1, x => map[x, o.Y].TileType);
    }

    private bool GetBombTop(Point o, int blastRadius)
    {
        var top = Math.Max(0, o.Y - blastRadius);
        return GetBombEffect(() => o.Y, y => y >= top, y => y - 1, y=> map[o.X, y].TileType);
    }

    private bool GetBombDown(Point o, int blastRadius)
    {
        var bottom = Math.Min(o.Y + blastRadius, map.GetLength(1) - 1);
        return GetBombEffect(() => o.Y, y => y <= bottom, y => y + 1, y => map[o.X, y].TileType);
    }

    public IEnumerable<Entity> GetMyBombs()
    {
        return Entities.Where(entity => entity.EntityType == EntityType.Bomb && entity.Owner == Me.Owner);
    }

    public Entity Me => me;
}

struct BlastEffect
{
    public Point Point { get; }
    public int BombDamage { get; }

    public BlastEffect(Point point, int bombDamage)
    {
        Point = point;
        BombDamage = bombDamage;
    }
}

struct PointAndDistance
{
    public PointAndDistance(Point point, int distance)
    {
        this.Point = point;
        this.Distance = distance;
    }

    public int Distance { get; private set; }
    public Point Point { get; private set; }
}

struct Point : IEquatable<Point>
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; private set; }

    public int Y { get; private set; }

    public bool Equals(Point other)
    {
        return other.X == X && other.Y == Y;
    }
    public override string ToString()
    {
        return $"{X}, {Y}";
    }
}

class Tile
{
    public TileType TileType { get; private set; }
    public List<Tile> Neighbors { get; private set; }
    public Point Position { get; private set; }
    public List<Entity> Entities { get; private set; }

    public Tile(TileType tileType, List<Tile> neighbors, Point position, List<Entity> entities)
    {
        TileType = tileType;
        Neighbors = neighbors;
        Position = position;
        Entities = entities;
    }
}

enum TileType
{
    Box,
    Empty,
    Unknown,
    Wall
}

static class Extensions
{
    public static TileType GetTile(this char tileChar)
    {
        switch (tileChar)
        {
            case '.':
                return TileType.Empty;
            case '0':
            case '1':
            case '2':
                return TileType.Box;
            case 'X':
                return TileType.Wall;
            default:
                return TileType.Unknown;
        }
    }

    public static Point AddX(this Point point, int x)
    {
        return new Point(point.X + x, point.Y);
    }

    public static Point AddY(this Point point, int y)
    {
        return new Point(point.X, point.Y + y);
    }
}

enum EntityType
{
    Player,
    Bomb,
    Item
}

class Mapa : Map
{
    public Mapa() : base(13, 11, new string[] {
        "..12.000.21..",
        ".X1X1X.X1X1X.",
        ".20.1.2.1.02.",
        "0X1X0X.X0X1X0",
        ".2.02...20.2.",
        "2X.X.X.X.X.X2",
        ".2.02...20.2.",
        "0X1X0X.X0X1X0",
        ".20.1.2.1.02.",
        ".X1X1X.X1X1X.",
        "..12.000.21.."
    }, Enumerable.Empty<Entity>(), 0)
    {
    }
}