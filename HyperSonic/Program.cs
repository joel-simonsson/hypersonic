using System;
using System.Linq;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    private const int RANGE = 7;

    static void Main(string[] args)
    {
        var map = new Mapa();
        //map.GetBombBlastEffect(new Point(12,0), 3);
        //var m = new Map(map.Width, map.Height, map.MapSource, map.Entities.ToList().Concat(new[] { map.Me, new Entity(EntityType.Bomb, map.Me.Owner, 0, 0, 8, 3) }), 0);
        GetOptimalBlastPoint(map, map.Entities.Where(entity => entity.EntityType == EntityType.Player && entity.Owner == 0).FirstOrDefault(), RANGE).Debug();
        //var safe = m.AmISafe();
        //map.GetPoints(new Point(0, 0), 100).Select(point => map.GetBombBlastEffect(point, 3)).OrderByDescending(be => be.BombDamage).ToList().ForEach(be => PerformeAction($"{be.Point} - {be.BombDamage}"));
        Console.ReadLine();
    }


    //static Point optimalPointStore = new Point(-1, -1);
    //static Point ExplodingPoint = new Point(-1, -1);

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
                mapSource[i].Debug();
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
                entities.Add(new Entity(entityType.GetEntityType(), owner, x, y, param1, param2));
                $"new Entity(EntityType.{entityType.GetEntityType()}, {owner}, {x}, {y}, {param1}, {param2})".Debug();
            }
            IMap map = new Map(mapSource, entities);
            Entity me = entities.Where(entity => entity.EntityType == EntityType.Player && entity.Owner == myId).FirstOrDefault();

            if (playerAction == PlayerAction.Lost)
            {
                optimalPoint = GetOptimalBlastPoint(map, me, RANGE);
                "Lost".Debug();
                $"MOVE {optimalPoint.X} {optimalPoint.Y}".PerformAction();
                playerAction = PlayerAction.Walking;
            }
            else if (playerAction == PlayerAction.Walking)
            {
                playerAction = PlayerIsWalking(playerAction, ref optimalPoint, map, me);
            }

            $"Optimal: {optimalPoint}".Debug();
        }
    }

    private static PlayerAction PlayerIsWalking(PlayerAction playerAction, ref Point optimalPoint, IMap map, Entity me)
    {
        if (me.Point.Equals(optimalPoint) && me.Param1 > 0)
        {
            var optimal = GetOptimalBlastPoint(map, me, RANGE);
            if (optimal.Equals(optimalPoint))
            {
                "At optimal".Debug();
                $"BOMB {optimalPoint.X} {optimalPoint.Y}".PerformAction();
                return PlayerAction.Lost;
            }
            else
            {
                "Optimal no longer optimal, walking".Debug();
                optimalPoint = optimal;
                $"MOVE {optimalPoint.X} {optimalPoint.Y}".PerformAction();
                return playerAction;
            }
        }
        else
        {
            "Walking".Debug();
            var optimalTile = map.GetTile(optimalPoint);
            if (optimalTile.Entities.Where(entity => entity.EntityType == EntityType.Bomb).Any() || optimalTile.Explodes != -1)
            {
                $"optimalPoint:{optimalPoint} no longer optimal!".Debug();
                optimalPoint = GetOptimalBlastPoint(map, me, RANGE);
                $"New optimalPoint:{optimalPoint}".Debug();
            }

            $"MOVE {optimalPoint.X} {optimalPoint.Y}".PerformAction();
            return playerAction;
        }
    }

    private static Point GetOptimalBlastPoint(IMap map, Entity me, int range)
    {
        return GetOptimalBlastPoint(map, me, range, Enumerable.Empty<Point>());
    }

    private static Point GetOptimalBlastPoint(IMap map, Entity me, int range, IEnumerable<Point> exclude)
    {
        Point optimalPoint;
        var blastPoints = map.GetReachablesPoints(me.Point, range).Where(pad => !exclude.Contains(pad.Point)).Select(pad => new { BlastEffect = map.GetBombBlastEffect(pad.Point, me.Param2), Distance = pad.Distance }).OrderByDescending(bead => bead.BlastEffect.BombDamage).ThenBy(bead => bead.Distance).ToList();
        blastPoints.ToList().ForEach(bpd => $"damage:{bpd.BlastEffect.BombDamage}, point:{bpd.BlastEffect.Point}, distance:{bpd.Distance}".Debug());
        if (!blastPoints.Any() || blastPoints.First().BlastEffect.BombDamage == 0)
        {
            blastPoints = map.GetReachablesPoints(me.Point, 100).Where(pad => !exclude.Contains(pad.Point)).Select(pad => new { BlastEffect = map.GetBombBlastEffect(pad.Point, me.Param2), Distance = pad.Distance }).OrderByDescending(bead => bead.BlastEffect.BombDamage).ThenBy(bead => bead.Distance).ToList();
        }

        if (!blastPoints.Any())
        {
            return me.Point;
        }

        var blastPoint = blastPoints.FirstOrDefault(bp => !map.Entities.Where(entity => entity.EntityType == EntityType.Bomb && entity.Owner == me.Owner).Select(bomb => bomb.Point).Contains(bp.BlastEffect.Point));

        if (blastPoint == null)
        {
            blastPoint = blastPoints.First();
        }
        optimalPoint = blastPoint.BlastEffect.Point;

        var nrOfSafePoints = blastPoints.Select(bad => map.GetTile(bad.BlastEffect.Point)).Where(tile => tile.Explodes == -1).Count();

        if (nrOfSafePoints == 1)
        {
            return optimalPoint;
        }

        var imaginaryBomb = new Entity(EntityType.Bomb, me.Owner, optimalPoint.X, optimalPoint.Y, 8, 3, true);
        var m = map.Clone(entities => map.Entities.ToList().Concat(new[] { imaginaryBomb }));
        if (!m.SafePointReachable(me.Point, 8))
        {
            return GetOptimalBlastPoint(map, me, range, exclude.Concat(new[] { optimalPoint }));
        }

        return optimalPoint;
    }
}
enum PlayerAction
{
    Walking,
    Lost,
    Hiding
}

class Entity
{
    public EntityType EntityType { get; }
    public int Owner { get; }
    public Point Point { get; }
    public int Param1 { get; }
    public int Param2 { get; }
    public bool Imaginary { get; }

    public Entity(EntityType entityType, int owner, int x, int y, int param1, int param2) : this(entityType, owner, x, y, param1, param2, false)
    {
    }

    public Entity(EntityType entityType, int owner, int x, int y, int param1, int param2, bool imaginary)
    {
        EntityType = entityType;
        Owner = owner;
        Point = new Point(x, y);
        Param1 = param1;
        Param2 = param2;
        Imaginary = imaginary;
    }

    public Entity Clone()
    {
        return new Entity(EntityType, Owner, Point.X, Point.Y, Param1, Param2, Imaginary);
    }
}

interface IMap
{
    BlastEffect GetBombBlastEffect(Point o, int blastRadius);
    List<PointAndDistance> GetReachablesPoints(Point point, int range);
    Tile GetTile(Point point);
    IEnumerable<Entity> Entities { get; }
    bool SafePointReachable(Point point, int range);
    IMap Clone(Func<IEnumerable<Entity>, IEnumerable<Entity>> modifyEntities);
}

class Map : IMap
{
    private Tile[,] map;
    List<Point> boxes = new List<Point>();
    public IEnumerable<Entity> Entities { get; private set; }
    private string[] MapSource { get; set; }

    public Map(string[] mapSource, IEnumerable<Entity> entities)
    {
        MapSource = mapSource;

        int width = mapSource.First().Length;
        int height = mapSource.Length;
        map = new Tile[width, height];
        for (int y = 0; y < mapSource.Length; y++)
        {
            var row = mapSource[y].ToCharArray();
            for (int x = 0; x < row.Length; x++)
            {
                var tileChar = row[x];
                var tilePoint = new Point(x, y);
                var tile = new Tile(row[x].GetTile(), new List<Tile>(), tilePoint, entities.Where(entity => entity.Point.Equals(tilePoint)).ToList(), -1);
                map[x, y] = tile;
                if (tile.TileType == TileType.Box)
                {
                    boxes.Add(new Point(x, y));
                }
            }
        }

        ModifyNeighbours(width, height);

        Entities = entities.ToList();

        ModifyTileExplosions(Entities);

        map.Debug(tile => tile.Entities.Where(entity => entity.EntityType == EntityType.Bomb).Any() || tile.Explodes != -1);
    }

    public Tile GetTile(Point point)
    {
        return map[point.X, point.Y];
    }

    private void ModifyNeighbours(int width, int height)
    {
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
    }

    private void ModifyTileExplosions(IEnumerable<Entity> entities)
    {
        entities.Where(entity => entity.EntityType == EntityType.Bomb).ToList().ForEach(bomb =>
        {
            new[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down }.ToList().ForEach(direction => WalkOrtogonalOnMap(bomb.Point, direction, 100, tile =>
            {
                Entity b;
                if (tile.TileType == TileType.Floor || tile.TileType == TileType.Box)
                {
                    if (tile.Explodes == -1 || bomb.Param1 < tile.Explodes)
                    {
                        map[tile.Point.X, tile.Point.Y] = new Tile(tile.TileType, tile.Neighbors, tile.Point, tile.Entities, bomb.Param1);
                    }
                }

                if ((b = tile.Entities.Where(entity => entity.EntityType == EntityType.Bomb).OrderBy(b1 => b1.Param1).FirstOrDefault()) != null)
                {
                    if (!bomb.Point.Equals(b.Point) && bomb.Param1 < b.Param1)
                    {
                        ModifyTileExplosions(new[] { new Entity(b.EntityType, b.Owner, b.Point.X, b.Point.Y, bomb.Param1, b.Param2) });
                    }
                }

                if (b == bomb)
                {
                    return WalkAction.Continue;
                }

                if (tile.TileType == TileType.Wall || tile.TileType == TileType.Box || tile.Entities.Any())
                {
                    return WalkAction.Stop;
                }
                return WalkAction.Continue;
            }));

        });
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

    public List<PointAndDistance> GetReachablesPoints(Point point, int radius)
    {
        var tile = map[point.X, point.Y];
        var list = new List<PointAndDistance>() { new PointAndDistance(point, 0) };
        var points = GetPointsRec(tile, list, 1, radius);
        return points.Except(points.Where(p => map[p.Point.X, p.Point.Y].Explodes != -1)).ToList();
    }

    private List<PointAndDistance> GetPointsRec(Tile tile, List<PointAndDistance> list, int depth, int radius)
    {
        if (depth == radius)
        {
            return list;
        }
        else
        {
            tile.Neighbors.ForEach(t =>
            {
                if (t.TileType == TileType.Floor && !list.Exists(pad => pad.Point.Equals(t.Point)) && !t.Entities.Any(item => item.EntityType == EntityType.Bomb && !item.Imaginary))
                {
                    list.Add(new PointAndDistance(t.Point, depth));
                    GetPointsRec(t, list, depth + 1, radius);
                }
            });
            return list;
        }
    }

    private void WalkOrtogonalOnMap(Point start, Direction direction, int maxSteps, Func<Tile, WalkAction> onTile)
    {
        if (direction == Direction.Left)
        {
            var left = Math.Max(0, start.X - maxSteps);
            PerformeOrtogonalWalkOnMap(() => start.X, x => x >= left, x => x - 1, x => map[x, start.Y], onTile);
        }

        if (direction == Direction.Right)
        {
            var right = Math.Min(start.X + maxSteps, map.GetLength(0) - 1);
            PerformeOrtogonalWalkOnMap(() => start.X, x => x <= right, x => x + 1, x => map[x, start.Y], onTile);
        }

        if (direction == Direction.Up)
        {
            var top = Math.Max(0, start.Y - maxSteps);
            PerformeOrtogonalWalkOnMap(() => start.Y, y => y >= top, y => y - 1, y => map[start.X, y], onTile);
        }

        if (direction == Direction.Down)
        {
            var bottom = Math.Min(start.Y + maxSteps, map.GetLength(1) - 1);
            PerformeOrtogonalWalkOnMap(() => start.Y, y => y <= bottom, y => y + 1, y => map[start.X, y], onTile);
        }
    }

    private void PerformeOrtogonalWalkOnMap(Func<int> init, Func<int, bool> condition, Func<int, int> modifier, Func<int, Tile> getTile, Func<Tile, WalkAction> onTile)
    {
        for (int i = init(); condition(i); i = modifier(i))
        {
            var tile = getTile(i);
            if (onTile(tile) == WalkAction.Stop)
                return;
        }
    }

    private bool GetBombEffect(Func<int> init, Func<int, bool> condition, Func<int, int> modifier, Func<int, Tile> getTile)
    {
        for (int i = init(); condition(i); i = modifier(i))
        {
            Entity bomb;
            var tile = getTile(i);
            if (tile.TileType == TileType.Box)
                return true;
            else if (tile.TileType == TileType.Wall)
                return false;
            else if ((bomb = tile.Entities.FirstOrDefault(entity => entity.EntityType == EntityType.Bomb)) != null)
            {
                return false;
            }
        }
        return false;
    }

    private bool GetBombEffectLeft(Point o, int blastRadius)
    {
        var left = Math.Max(0, o.X - blastRadius);
        return GetBombEffect(() => o.X, x => x >= left, x => x - 1, x => map[x, o.Y]);
    }

    private bool GetBombRight(Point o, int blastRadius)
    {
        var right = Math.Min(o.X + blastRadius, map.GetLength(0) - 1);
        return GetBombEffect(() => o.X, x => x <= right, x => x + 1, x => map[x, o.Y]);
    }

    private bool GetBombTop(Point o, int blastRadius)
    {
        var top = Math.Max(0, o.Y - blastRadius);
        return GetBombEffect(() => o.Y, y => y >= top, y => y - 1, y => map[o.X, y]);
    }

    private bool GetBombDown(Point o, int blastRadius)
    {
        var bottom = Math.Min(o.Y + blastRadius, map.GetLength(1) - 1);
        return GetBombEffect(() => o.Y, y => y <= bottom, y => y + 1, y => map[o.X, y]);
    }

    public bool SafePointReachable(Point point, int range)
    {
        var list = GetReachablesPoints(point, range);
        if (!list.Any())
        {
            return false;
        }
        return list.Select(pad => map[pad.Point.X, pad.Point.Y]).Any(tile => tile.Explodes == -1);
    }

    public IMap Clone(Func<IEnumerable<Entity>, IEnumerable<Entity>> modifyEntities)
    {
        var entities = modifyEntities(Entities.Select(entity => entity.Clone()));
        var map = new Map(MapSource, entities);
        return map;
    }
}

enum WalkAction
{
    Continue,
    Stop
}

enum Direction
{
    Up,
    Down,
    Left,
    Right
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
    public Point Point { get; private set; }
    public List<Entity> Entities { get; private set; }
    public int Explodes { get; private set; }

    public Tile(TileType tileType, List<Tile> neighbors, Point point, List<Entity> entities, int explodes)
    {
        TileType = tileType;
        Neighbors = neighbors;
        Point = point;
        Entities = entities;
        Explodes = explodes;
    }

    public override string ToString()
    {
        var entities = Entities.Any() ? $"entities {Entities.Select(entity => entity.EntityType.ToString()).ToList().Aggregate((c, n) => $"{c}, {n}")}" : "";
        return $"point: {Point} type:{TileType} explodes:{Explodes} {entities}";
    }
}

enum TileType
{
    Box,
    Floor,
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
                return TileType.Floor;
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

    public static EntityType GetEntityType(this int entityType)
    {
        return entityType == 0 ? EntityType.Player : entityType == 1 ? EntityType.Bomb : EntityType.Item;
    }


    public static void PerformAction(this string action)
    {
        var now = DateTime.Now;
        $"{now.Minute}:{now.Second}.{now.Millisecond}".Debug();
        Console.WriteLine($"{action}");
    }

    public static void Debug(this object message)
    {
        Console.Error.WriteLine(message);
    }

    public static void Debug(this Tile[,] map, Func<Tile, bool> condition)
    {
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                var tile = map[x, y];
                if (condition(tile))
                {
                    tile.Debug();
                }
            }
        }
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
    public Mapa() : base(new string[] {
".............",
".X2X0X1X0X2X.",
"11.0.2.2.0.11",
"2X.X.X.X.X.X2",
"....21.12....",
"1X.X.X.X.X.X1",
"....21.12....",
"2X.X.X.X.X.X2",
"11.0.2.2.0.11",
".X2X0X1X0X2X.",
"............."
    }, new[] {

new Entity(EntityType.Player, 0, 0, 1, 0, 3),
new Entity(EntityType.Player, 1, 11, 10, 0, 3),
new Entity(EntityType.Player, 2, 12, 1, 0, 3),
new Entity(EntityType.Bomb, 1, 12, 10, 7, 3),
new Entity(EntityType.Bomb, 0, 0, 1, 8, 3),
new Entity(EntityType.Bomb, 2, 12, 1, 8, 3)
        })
    {
    }
}