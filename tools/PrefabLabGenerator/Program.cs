using System.Numerics;
using TruckLib.HashFs;
using TruckLib.Models.Ppd;
using TruckLib.ScsMap;

var outputRoot = args.Length > 0
    ? args[0]
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Euro Truck Simulator 2",
        "mod",
        "user_map",
        "map");

var gameRoot = args.Length > 1
    ? args[1]
    : @"C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2";

var mode = args.Length > 2 ? args[2] : "prefab-only";
var mapName = args.Length > 3 ? args[3] : ModeToMapName(mode);

var map = new Map
{
    NormalScale = 4,
    CityScale = 4
};

using var baseArchive = HashFsReader.Open(Path.Combine(gameRoot, "base.scs"));
var descriptor = PrefabDescriptor.Open("/prefab/cross/road1_x_road1_t.ppd", baseArchive);

var prefab = Prefab.Add(
    map,
    Vector3.Zero,
    "56",
    descriptor,
    Quaternion.Identity);

prefab.Variant = "default";
prefab.Look = "default";

AddModeRoads(map, prefab, mode);

map.Save(outputRoot, mapName, true);

Console.WriteLine($"Generated {mapName} into: {outputRoot}");
Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"Prefab nodes: {prefab.Nodes.Count}");

static string ModeToMapName(string mode) => mode switch
{
    "prefab-only" => "prefab_lab",
    "node0" => "prefab_lab_node0",
    "node1" => "prefab_lab_node1",
    "node2" => "prefab_lab_node2",
    "node0_node1" => "prefab_lab_node0_node1",
    "node0_node1_radial" => "prefab_lab_node0_node1_radial",
    "node0_node1_rotation" => "prefab_lab_node0_node1_rotation",
    "all" => "prefab_lab_all",
    _ => "prefab_lab_custom"
};

static void AddModeRoads(Map map, Prefab prefab, string mode)
{
    switch (mode)
    {
        case "prefab-only":
            return;
        case "node0":
            AddRoadFromNode(prefab, 0, new Vector3(-220, 0, 0));
            return;
        case "node1":
            AddRoadFromNode(prefab, 1, new Vector3(0, 0, -220));
            return;
        case "node2":
            AddRoadFromNode(prefab, 2, new Vector3(220, 0, 0));
            return;
        case "node0_node1":
            AddRoadFromNode(prefab, 0, new Vector3(-220, 0, 0));
            AddRoadFromNode(prefab, 1, new Vector3(0, 0, -220));
            return;
        case "node0_node1_radial":
            AddRoadFromNodeRadial(prefab, 0, 260);
            AddRoadFromNodeRadial(prefab, 1, 260);
            return;
        case "node0_node1_rotation":
            AddRoadFromNodeRotation(prefab, 0, 260);
            AddRoadFromNodeRotation(prefab, 1, 260);
            return;
        case "all":
            AddRoadFromNode(prefab, 0, new Vector3(-220, 0, 0));
            AddRoadFromNode(prefab, 1, new Vector3(0, 0, -220));
            AddRoadFromNode(prefab, 2, new Vector3(220, 0, 0));
            return;
        default:
            throw new ArgumentException($"Modo desconhecido: {mode}");
    }

    static void AddRoadFromNode(Prefab prefab, ushort nodeIndex, Vector3 end)
    {
        var road = prefab.AppendRoad(nodeIndex, end, "ger1", 10, 10);
        road.Right.Look = "ger_1";
        road.Right.Variant = "broken_de";
        road.Right.LeftEdge = "ger_sh_15";
        road.Right.RightEdge = "ger_sh_15";
        road.Left.Look = "ger_1";
        road.Left.Variant = "broken_de";
        road.Left.LeftEdge = "ger_sh_15";
        road.Left.RightEdge = "ger_sh_15";

        foreach (var side in new[] { road.Left, road.Right })
        {
            side.Terrain.QuadData.BrushMaterials[0] = new Material("34");
            side.Terrain.Profile = "profile12";
            side.Terrain.Noise = TerrainNoise.Percent0;
            side.Terrain.Coefficient = 0.25f;
        }
    }

    static void AddRoadFromNodeRadial(Prefab prefab, ushort nodeIndex, float length)
    {
        var node = prefab.Nodes[nodeIndex];
        var direction = Vector3.Normalize(node.Position - prefab.Nodes.Aggregate(Vector3.Zero, (sum, n) => sum + n.Position) / prefab.Nodes.Count);
        if (float.IsNaN(direction.X) || float.IsNaN(direction.Z) || direction.LengthSquared() < 0.1f)
        {
            direction = nodeIndex switch
            {
                0 => new Vector3(-1, 0, 0),
                1 => new Vector3(0, 0, -1),
                _ => new Vector3(1, 0, 0)
            };
        }

        AddRoadFromNode(prefab, nodeIndex, node.Position + direction * length);
    }

    static void AddRoadFromNodeRotation(Prefab prefab, ushort nodeIndex, float length)
    {
        var node = prefab.Nodes[nodeIndex];
        var direction = Vector3.Transform(new Vector3(0, 0, 1), node.Rotation);
        direction.Y = 0;

        if (float.IsNaN(direction.X) || float.IsNaN(direction.Z) || direction.LengthSquared() < 0.1f)
        {
            direction = nodeIndex switch
            {
                0 => new Vector3(-1, 0, 0),
                1 => new Vector3(0, 0, -1),
                _ => new Vector3(1, 0, 0)
            };
        }

        direction = Vector3.Normalize(direction);
        AddRoadFromNode(prefab, nodeIndex, node.Position + direction * length);
    }
}
