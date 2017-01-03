using System;
using System.Linq;
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
        map.GetPositions(new Point(0, 0), 7).ToList().ForEach(p => $"Point:{p.Point} Distance:{p.Distance}".Debug());
        map.GetBombBlastEffect(new Point(3, 2), 5);
        //map.GetPositions(new Point(0, 0), 100).Select(point => map.GetBombBlastEffect(point, 3)).OrderByDescending(be => be.BombDamage).ToList().ForEach(be => PerformeAction($"{be.Point} - {be.BombDamage}"));
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
            }
            IMap map = new Map(width, height, mapSource, entities, myId);

            if (playerAction == PlayerAction.Lost)
            {
                optimalPoint = GetOptimalPoint(map);
                "Lost".Debug();
                $"MOVE {optimalPoint.X} {optimalPoint.Y}".PerformAction();
                playerAction = PlayerAction.Walking;
            }
            else if (playerAction == PlayerAction.Walking)
            {
                playerAction = PlayerIsWalking(playerAction, ref optimalPoint, map, myId);
            }

            $"Optimal: {optimalPoint}".Debug();
        }
    }

    private static PlayerAction PlayerIsWalking(PlayerAction playerAction, ref Point optimalPoint, IMap map, int myId)
    {
        if (map.Me.Point.Equals(optimalPoint) && map.Me.Param1 > 0)
        {
            var optimal = GetOptimalPoint(map);
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
                optimalPoint = GetOptimalPoint(map);
                $"New optimalPoint:{optimalPoint}".Debug();
            }

            $"MOVE {optimalPoint.X} {optimalPoint.Y}".PerformAction();
            return playerAction;
        }
    }

    private static Point GetOptimalPoint(IMap map)
    {
        Point optimalPoint;
        var blastPoints = map.GetPositions(map.Me.Point, 10).Select(pad => new { BlastEffect = map.GetBombBlastEffect(pad.Point, map.Me.Param2), Distance = pad.Distance }).OrderByDescending(bead => bead.BlastEffect.BombDamage).ThenBy(bead => bead.Distance).ToList();
        blastPoints.ToList().ForEach(bpd => $"be:{bpd.BlastEffect.BombDamage}, po:{bpd.BlastEffect.Point}, d:{bpd.Distance}".Debug());
        if (blastPoints.First().BlastEffect.BombDamage == 0)
        {
            blastPoints = map.GetPositions(map.Me.Point, 100).Select(pad => new { BlastEffect = map.GetBombBlastEffect(pad.Point, map.Me.Param2), Distance = pad.Distance }).OrderByDescending(bead => bead.BlastEffect.BombDamage).ThenBy(bead => bead.Distance).ToList();
        }
        var blastPoint = blastPoints.FirstOrDefault(bp => !map.GetMyBombs().Select(bomb => bomb.Point).Contains(bp.BlastEffect.Point));
        if (blastPoint == null)
        {
            blastPoint = blastPoints.First();
        }
        optimalPoint = blastPoint.BlastEffect.Point;
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

    public Entity(EntityType entityType, int owner, int x, int y, int param1, int param2)
    {
        EntityType = entityType;
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
    Tile GetTile(Point point);
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
                var tile = new Tile(row[x].GetTile(), new List<Tile>(), tilePosition, entities.Where(entity => entity.Point.Equals(tilePosition)).ToList(), -1);
                map[x, y] = tile;
                if (tile.TileType == TileType.Box)
                {
                    boxes.Add(new Point(x, y));
                }
            }
        }

        ModifyNeighbours(width, height);
        me = entities.Where(entity => entity.EntityType == EntityType.Player && entity.Owner == myId).FirstOrDefault();

        Entities = entities.Except(new[] { me });

        ModifyTileExplosions(Entities);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = map[x, y];
                if (tile.Entities.Where(entity => entity.EntityType == EntityType.Bomb).Any() || tile.Explodes != -1)
                    tile.ToString().Debug();
            }
        }
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
                if ((b = tile.Entities.Where(entity => entity.EntityType == EntityType.Bomb).OrderBy(b1 => b1.Param1).FirstOrDefault()) != null)
                {
                    if (bomb.Param1 < b.Param1)
                    {
                        ModifyTileExplosions(new[] { new Entity(b.EntityType, b.Owner, b.Point.X, b.Point.Y, bomb.Param1, b.Param2) });
                    }
                }
                else if (tile.TileType == TileType.Floor || tile.TileType == TileType.Box)
                {
                    if (tile.Explodes == -1 || bomb.Param1 < tile.Explodes)
                    {
                        map[tile.Position.X, tile.Position.Y] = new Tile(tile.TileType, tile.Neighbors, tile.Position, tile.Entities, bomb.Param1);
                    }
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

    public List<PointAndDistance> GetPositions(Point point, int radius)
    {
        var tile = map[point.X, point.Y];
        var list = new List<PointAndDistance>() { new PointAndDistance(point, 0) };
        var positions = GetPositionsRec(tile, list, 1, radius);
        return positions.Except(positions.Where(position => map[position.Point.X, position.Point.Y].Explodes > -1)).ToList();
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
                if (t.TileType == TileType.Floor && !list.Exists(pad => pad.Point.Equals(t.Position)) && !t.Entities.Any(item => item.EntityType == EntityType.Bomb))
                {
                    list.Add(new PointAndDistance(t.Position, depth));
                    GetPositionsRec(t, list, depth + 1, radius);
                }
            });
            return list;
        }
    }

    private void WalkOrtogonalOnMap(Point start, Direction direction, int maxSteps, Func<Tile, WalkAction> onTile)
    {
        if (direction == Direction.Left && start.X > 0)
        {
            var left = Math.Max(0, start.X - maxSteps);
            PerformeOrtogonalWalkOnMap(() => start.X - 1, x => x >= left, x => x - 1, x => map[x, start.Y], onTile);
        }

        if (direction == Direction.Right && start.X < map.GetLength(0) - 1)
        {
            var right = Math.Min(start.X + maxSteps, map.GetLength(0) - 1);
            PerformeOrtogonalWalkOnMap(() => start.X + 1, x => x <= right, x => x + 1, x => map[x, start.Y], onTile);
        }

        if (direction == Direction.Up && start.Y > 0)
        {
            var top = Math.Max(0, start.Y - maxSteps);
            PerformeOrtogonalWalkOnMap(() => start.Y - 1, y => y >= top, y => y - 1, y => map[start.X, y], onTile);
        }

        if (direction == Direction.Down && start.Y > map.GetLength(1) - 1)
        {
            var bottom = Math.Min(start.Y + maxSteps, map.GetLength(1) - 1);
            PerformeOrtogonalWalkOnMap(() => start.Y + 1, y => y <= bottom, y => y + 1, y => map[start.X, y], onTile);
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

    public IEnumerable<Entity> GetMyBombs()
    {
        return Entities.Where(entity => entity.EntityType == EntityType.Bomb && entity.Owner == Me.Owner);
    }

    public Entity Me => me;
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
    public Point Position { get; private set; }
    public List<Entity> Entities { get; private set; }
    public int Explodes { get; private set; }

    public Tile(TileType tileType, List<Tile> neighbors, Point position, List<Entity> entities, int explodes)
    {
        TileType = tileType;
        Neighbors = neighbors;
        Position = position;
        Entities = entities;
        Explodes = explodes;
    }

    public override string ToString()
    {
        var entities = Entities.Any() ? $"entities {Entities.Select(entity => entity.EntityType.ToString()).ToList().Aggregate((c, n) => $"{c}, {n}")}" : "";
        return $"point: {Position} type:{TileType} explodes:{Explodes}";
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

    public static void Debug(this string message)
    {
        Console.Error.WriteLine(message);
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
    }, new[] { new Entity(EntityType.Bomb, 0, 5, 4, 3, 3), new Entity(EntityType.Bomb, 0, 7, 4, 4, 3) }, 0)
    {
    }
}