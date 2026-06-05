using System.Globalization;
using System.Numerics;
using TruckLib.HashFs;
using TruckLib.Models.Ppd;
using TruckLib.ScsMap;

const string MapName = "projeto_brasil";
const double Scale = 4.0;

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
var enablePrefabExperiment = args.Contains("--enable-prefab-experiment");
var disableStartPrefab = args.Contains("--no-start-prefab");
var enableStartPrefab = args.Contains("--with-start-prefab");
var useRealJunction = args.Contains("--with-junction") || args.Contains("--real-junction");

var map = new Map
{
    NormalScale = 4,
    CityScale = 4
};

var origin = new GeoPoint(-30.03920, -51.23100);

if (enableStartPrefab && !disableStartPrefab)
{
    AddPilotNetwork(map, origin, gameRoot);
}
else if (useRealJunction)
{
    AddRealTraceWithJunction(map, origin, gameRoot, args);
}
else
{
    foreach (var road in LoadRealTraceRoads())
    {
        AddRoad(map, road, origin);
    }
}

if (enablePrefabExperiment)
{
    AddFirstTjunction(map, origin, gameRoot);
}

map.Save(outputRoot, MapName, true);

Console.WriteLine($"Generated {MapName} into: {outputRoot}");
Console.WriteLine("Open the map in ETS2 editor and run Map > Recompute map before visual inspection.");
if (useRealJunction)
{
    Console.WriteLine("Mode: real trace split across T-junction (prefab 56) + oriented side branch.");
    Console.WriteLine("Test with: -edit projeto_brasil -noworkshop  then Map > Recompute map");
    Console.WriteLine("Tune: --junction-index N  --side-length M");
    Console.WriteLine("Rollback to pure trace: .\\tools\\generate_map.ps1   (no flags) + install");
}

static void AddRoad(Map map, RoadBlueprint blueprint, GeoPoint origin)
{
    if (blueprint.Points.Length < 2)
    {
        return;
    }

    var first = ToGamePosition(blueprint.Points[0], origin);
    var second = ToGamePosition(blueprint.Points[1], origin);
    var road = Road.Add(map, first, second, blueprint.RoadTemplate, blueprint.TerrainSize, blueprint.TerrainSize);
    ApplyUrbanRoadStyle(road, blueprint);

    for (var i = 2; i < blueprint.Points.Length; i++)
    {
        road = road.Append(ToGamePosition(blueprint.Points[i], origin));
        ApplyUrbanRoadStyle(road, blueprint);
    }
}

static List<GeoPoint> LoadRealTracePoints()
{
    var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "zone01a_real_trace.csv");
    var points = new List<GeoPoint>();
    if (!File.Exists(csvPath))
    {
        Console.WriteLine($"Real trace CSV not found at {csvPath}; falling back to built-in pilot points.");
        // Return empty; callers that need real trace will fallback.
        return points;
    }

    foreach (var rawLine in File.ReadLines(csvPath))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
        {
            continue;
        }

        var parts = line.Split(",");
        if (parts.Length < 3 || !parts[0].Equals("zone01a", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        points.Add(new GeoPoint(
            double.Parse(parts[1], CultureInfo.InvariantCulture),
            double.Parse(parts[2], CultureInfo.InvariantCulture)));
    }

    return points;
}

static RoadBlueprint[] LoadRealTraceRoads()
{
    var points = LoadRealTracePoints();
    if (points.Count < 2)
    {
        Console.WriteLine("Real trace CSV did not contain enough points; falling back to built-in route.");
        return PortoAlegrePilot.Roads;
    }

    return
    [
        new RoadBlueprint(
            "Zona 01-A real trace",
            PortoAlegrePilot.MainRoadStyle.RoadTemplate,
            PortoAlegrePilot.MainRoadStyle.Look,
            PortoAlegrePilot.MainRoadStyle.Variant,
            PortoAlegrePilot.MainRoadStyle.Edge,
            PortoAlegrePilot.MainRoadStyle.TerrainSize,
            points.ToArray())
    ];
}

static void AddPilotNetwork(Map map, GeoPoint origin, string gameRoot)
{
    var prefab = AddStartTjunction(map, origin, gameRoot);

    if (prefab is null || prefab.Nodes.Count < 2)
    {
        AddRoad(map, PortoAlegrePilot.Roads[0], origin);
        return;
    }

    AddRoadFromNodeRotation(prefab, 0, 520, PortoAlegrePilot.SideRoadStyle);
    AddRoadFromNodeRotation(prefab, 1, 1100, PortoAlegrePilot.MainRoadStyle);
}

static Prefab? AddStartTjunction(Map map, GeoPoint origin, string gameRoot)
{
    var baseScs = Path.Combine(gameRoot, "base.scs");
    if (!File.Exists(baseScs))
    {
        Console.WriteLine($"Skipping start prefab: base.scs not found at {baseScs}");
        return null;
    }

    using var baseArchive = HashFsReader.Open(baseScs);
    var descriptor = PrefabDescriptor.Open("/prefab/cross/road1_x_road1_t.ppd", baseArchive);
    var position = ToGamePosition(PortoAlegrePilot.Roads[0].Points[0], origin);

    var prefab = Prefab.Add(
        map,
        position,
        "56",
        descriptor,
        Quaternion.Identity);

    prefab.Variant = "default";
    prefab.Look = "default";

    return prefab;
}

static Road AddRoadFromNodeRotation(Prefab prefab, ushort nodeIndex, float length, RoadBlueprint style)
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
    var road = prefab.AppendRoad(nodeIndex, node.Position + direction * length, style.RoadTemplate, style.TerrainSize, style.TerrainSize);
    ApplyUrbanRoadStyle(road, style);
    return road;
}

static void AddFirstTjunction(Map map, GeoPoint origin, string gameRoot)
{
    var baseScs = Path.Combine(gameRoot, "base.scs");
    if (!File.Exists(baseScs))
    {
        Console.WriteLine($"Skipping prefab test: base.scs not found at {baseScs}");
        return;
    }

    using var baseArchive = HashFsReader.Open(baseScs);
    var descriptor = PrefabDescriptor.Open("/prefab/cross/road1_x_road1_t.ppd", baseArchive);
    var position = ToGamePosition(new GeoPoint(-30.03280, -51.24230), origin);

    var prefab = Prefab.Add(
        map,
        position,
        "56",
        descriptor,
        Quaternion.CreateFromYawPitchRoll(1.5708f, 0, 0));

    prefab.Variant = "default";
    prefab.Look = "default";

    if (prefab.Nodes.Count < 3)
    {
        Console.WriteLine($"Prefab test skipped road append: expected at least 3 nodes, got {prefab.Nodes.Count}");
        return;
    }

    var east = ToGamePosition(new GeoPoint(-30.03360, -51.23860), origin);
    var west = ToGamePosition(new GeoPoint(-30.03220, -51.24500), origin);
    var south = ToGamePosition(new GeoPoint(-30.03520, -51.24280), origin);

    ApplyUrbanRoadStyle(prefab.AppendRoad(0, east, "ger1", 10, 10), PortoAlegrePilot.SideRoadStyle);
    ApplyUrbanRoadStyle(prefab.AppendRoad(1, west, "ger1", 10, 10), PortoAlegrePilot.SideRoadStyle);
    ApplyUrbanRoadStyle(prefab.AppendRoad(2, south, "ger1", 10, 10), PortoAlegrePilot.SideRoadStyle);
}

static void AddRealTraceWithJunction(Map map, GeoPoint origin, string gameRoot, string[] cmdArgs)
{
    var points = LoadRealTracePoints();
    if (points.Count < 3)
    {
        Console.WriteLine("Not enough real trace points for junction split; falling back to single continuous road.");
        foreach (var road in LoadRealTraceRoads())
        {
            AddRoad(map, road, origin);
        }
        return;
    }

    // Parse tuning from command line (supports quick experiments from generate_map.ps1 --with-junction --junc 7 --side 220)
    int juncIdx = 5;
    float sideLength = 180f;
    for (int i = 0; i < cmdArgs.Length; i++)
    {
        if ((cmdArgs[i] == "--junction-index" || cmdArgs[i] == "--junc") && i + 1 < cmdArgs.Length && int.TryParse(cmdArgs[i + 1], out var j))
            juncIdx = j;
        if ((cmdArgs[i] == "--side-length" || cmdArgs[i] == "--side") && i + 1 < cmdArgs.Length && float.TryParse(cmdArgs[i + 1], out var l))
            sideLength = l;
    }

    // Junction index chosen on the early Orla / Edvaldo Pereira Paiva section (near Gasômetro).
    // This allows the "before" leg (Gasômetro side) + "after" leg (Praia de Belas / Centro direction)
    // to be proper separate roads attached to the prefab nodes.
    // Adjust with --junction-index N (or --junc N) after visual inspection.
    // Example: .\tools\generate_map.ps1 --with-junction --junction-index 6 --side-length 250
    var juncGeo = points[juncIdx];
    var juncPos = ToGamePosition(juncGeo, origin);

    Console.WriteLine($"[Junction] Placing prefab 56 (road1_x_road1_t) at trace index {juncIdx} ~ {juncGeo.Latitude:F6}, {juncGeo.Longitude:F6} (use --junction-index to change)");

    var baseScs = Path.Combine(gameRoot, "base.scs");
    if (!File.Exists(baseScs))
    {
        Console.WriteLine($"base.scs not found at {baseScs}; cannot load prefab descriptor. Falling back to continuous trace.");
        foreach (var road in LoadRealTraceRoads())
        {
            AddRoad(map, road, origin);
        }
        return;
    }

    using var baseArchive = HashFsReader.Open(baseScs);
    var descriptor = PrefabDescriptor.Open("/prefab/cross/road1_x_road1_t.ppd", baseArchive);

    // Compute approximate avenue direction (bearing) at the junction point from the trace.
    // This lets us orient the whole prefab so the "through" roughly follows the real road,
    // making the third arm (side) stick out more visibly as a T-branch instead of continuing straight.
    // We also use the same vector to compute a true perpendicular for the side branch.
    float prefabYaw = 0f;
    double east = 0;
    double north = 0;
    if (juncIdx > 0 && juncIdx + 1 < points.Count)
    {
        var pPrev = points[juncIdx - 1];
        var pNext = points[juncIdx + 1];
        double dLat = pNext.Latitude - pPrev.Latitude;
        double dLon = pNext.Longitude - pPrev.Longitude;
        const double metersPerDegLat = 111_320.0;
        double metersPerDegLon = metersPerDegLat * Math.Cos(pPrev.Latitude * Math.PI / 180.0);
        east = dLon * metersPerDegLon;
        north = dLat * metersPerDegLat;
        // Game: +X = east, +Z = -north (see ToGamePosition). Atan2(east, north) gives a reasonable yaw; sign may need visual tweak.
        prefabYaw = (float)Math.Atan2(east, north);
    }

    var prefab = Prefab.Add(
        map,
        juncPos,
        "56",
        descriptor,
        Quaternion.CreateFromYawPitchRoll(prefabYaw, 0, 0));

    prefab.Variant = "default";
    prefab.Look = "default";

    // Note: Prefab in this TruckLib version doesn't expose direct Terrain for setting (caused build error).
    // We rely on the attached roads' terrain (with increased TerrainSize to 20) to paint asphalt over the area and push grass back.
    // This is the standard way to control terrain around prefabs.

    // Diagnostics: print node world positions and outgoing directions (very useful when looking at screenshots like the current U)
    Console.WriteLine("  Prefab nodes after oriented placement:");
    for (int n = 0; n < Math.Min(3, prefab.Nodes.Count); n++)
    {
        var nd = prefab.Nodes[n];
        var dir = Vector3.Transform(new Vector3(0, 0, 1), nd.Rotation);
        dir.Y = 0;
        if (dir.LengthSquared() > 0.0001f) dir = Vector3.Normalize(dir);
        Console.WriteLine($"    node{n}: pos=({nd.Position.X:F1}, {nd.Position.Z:F1})  dir≈({dir.X:F2}, {dir.Z:F2})");
    }

    // Split the trace: before (reversed so we "append outward" from junction toward the start of trace)
    var beforeGeo = points.Take(juncIdx + 1).Reverse().Skip(1).ToArray();
    var afterGeo = points.Skip(juncIdx + 1).ToArray();

    var beforeWorld = beforeGeo.Select(p => ToGamePosition(p, origin)).ToArray();
    var afterWorld = afterGeo.Select(p => ToGamePosition(p, origin)).ToArray();

    // Side direction (company access / bay): choose the perpendicular that BEST aligns with the remaining prefab node
    // after the two main legs are assigned. This fixes cases where side got dot=-1 (anti-aligned) leading to
    // poor encaixe or the side branch going "wrong way" at the T (contributing to overlaps/gaps in screenshots).
    // We try both left and right perps from the trace bearing, pick the sign with highest dot to the leftover node.
    // The geo points for sideMid/companyEntrance are computed with the *chosen* perp so the bay is on the good side.
    double sideMeters = sideLength;

    // The two possible perps (one will point "out" toward a useful company area without immediately crossing main)
    double[,] perpCandidates = new double[,] {
        { -north, east },   // original
        {  north, -east }   // flipped
    };

    // Compute desired for mains (before/after) from real trace
    Vector3 juncW = juncPos;
    Vector3 dirAfter = Vector3.Zero;
    if (afterWorld.Length > 0) dirAfter = Vector3.Normalize(afterWorld[0] - juncW);
    Vector3 dirBefore = Vector3.Zero;
    if (beforeWorld.Length > 0) dirBefore = Vector3.Normalize(beforeWorld[0] - juncW);

    // Node outgoing directions from the oriented prefab
    var nodeDirs = new Vector3[3];
    for (int n = 0; n < 3; n++)
    {
        var nd = prefab.Nodes[n];
        var d = Vector3.Transform(new Vector3(0, 0, 1), nd.Rotation);
        d.Y = 0;
        if (d.LengthSquared() > 0.0001f) d = Vector3.Normalize(d);
        nodeDirs[n] = d;
    }

    // Assign mains first (they have priority for the through trace)
    var assignedNode = new ushort[3];
    var used = new bool[3];
    var mainLegs = new[] { new {dir=dirBefore, name="before", idx=0}, new {dir=dirAfter, name="after", idx=1} };
    foreach (var leg in mainLegs)
    {
        float bestDot = -2;
        int bestN = -1;
        for (int n = 0; n < 3; n++)
        {
            if (used[n]) continue;
            float dot = Vector3.Dot(leg.dir, nodeDirs[n]);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestN = n;
            }
        }
        assignedNode[leg.idx] = (ushort)bestN;
        used[bestN] = true;
        Console.WriteLine($"  Assigned {leg.name} leg to node {bestN} (dot {bestDot:F2})");
    }

    // Now pick best perp sign for the SIDE using the single remaining node (guarantees highest possible alignment for company access)
    int remaining = -1;
    for (int n = 0; n < 3; n++) if (!used[n]) { remaining = n; break; }
    float bestSideDot = -2f;
    int bestSign = 0;
    Vector3 bestDirSide = Vector3.Zero;
    for (int s = 0; s < 2; s++)
    {
        double se = perpCandidates[s, 0];
        double sn = perpCandidates[s, 1];
        var ds = new Vector3((float)se, 0, (float)sn);
        if (ds.LengthSquared() > 0.0001f) ds = Vector3.Normalize(ds);
        float dot = Vector3.Dot(ds, nodeDirs[remaining]);
        if (dot > bestSideDot)
        {
            bestSideDot = dot;
            bestSign = s;
            bestDirSide = ds;
        }
    }
    double sideEast = perpCandidates[bestSign, 0];
    double sideNorth = perpCandidates[bestSign, 1];
    Vector3 dirSide = bestDirSide;

    double dLatPerp = sideNorth / 111_320.0;
    double dLonPerp = sideEast / (111_320.0 * Math.Cos(juncGeo.Latitude * Math.PI / 180.0));
    var sideMidGeo = new GeoPoint(juncGeo.Latitude + dLatPerp * 0.6, juncGeo.Longitude + dLonPerp * 0.6);
    var companyEntranceGeo = new GeoPoint(juncGeo.Latitude + dLatPerp, juncGeo.Longitude + dLonPerp);

    assignedNode[2] = (ushort)remaining;
    Console.WriteLine($"  Assigned side leg to node {remaining} (dot {bestSideDot:F2})  [auto-chose perp sign {bestSign} for best alignment]");
    if (bestSideDot < 0.3f)
    {
        Console.WriteLine("  WARNING: side still has low/negative dot even after trying both perps -- junction angle may need CSV tweak or manual editor fix.");
    }

    // Attach using the assigned nodes.
    // Mains use the full AttachPrefabricatedLeg (which does launch in node dir + trace points).
    AttachPrefabricatedLeg(map, prefab, assignedNode[1], afterWorld, PortoAlegrePilot.MainRoadStyle); // after
    AttachPrefabricatedLeg(map, prefab, assignedNode[0], beforeWorld, PortoAlegrePilot.MainRoadStyle); // before

    // For side, use the launch from exact assigned node pos in its rotation (crash-safe, no AppendRoad on bad nodes if any).
    int sideNodeIdx = assignedNode[2];
    var sNode = prefab.Nodes[sideNodeIdx];
    var sDir = Vector3.Transform(new Vector3(0, 0, 1), sNode.Rotation);
    sDir.Y = 0;
    if (float.IsNaN(sDir.X) || float.IsNaN(sDir.Z) || sDir.LengthSquared() < 0.1f) sDir = new Vector3(0, 0, -1);
    sDir = Vector3.Normalize(sDir);
    var sLaunch = sNode.Position + sDir * 25f;
    var sMidW = ToGamePosition(sideMidGeo, origin);
    var sCompW = ToGamePosition(companyEntranceGeo, origin);

    var s1 = Road.Add(map, sNode.Position, sLaunch, "ger1", 10, 10);
    ApplyUrbanRoadStyle(s1, PortoAlegrePilot.SideRoadStyle);
    var s2 = s1.Append(sMidW);
    ApplyUrbanRoadStyle(s2, PortoAlegrePilot.SideRoadStyle);
    var s3 = s2.Append(sCompW);
    ApplyUrbanRoadStyle(s3, PortoAlegrePilot.SideRoadStyle);

    // Add L-shaped parking / delivery bay at the company entrance (perpendicular for truck maneuvering).
    // Increased sizes for better coverage.
    double bayScale = 120 / 111_320.0;
    double bayDLat = -dLonPerp * bayScale * 1.2;
    double bayDLon = dLatPerp * bayScale * 1.2;
    var bayMidGeo = new GeoPoint(companyEntranceGeo.Latitude + bayDLat * 0.5, companyEntranceGeo.Longitude + bayDLon * 0.5);
    var bayEndGeo = new GeoPoint(companyEntranceGeo.Latitude + bayDLat, companyEntranceGeo.Longitude + bayDLon);

    var bayMidWorld = ToGamePosition(bayMidGeo, origin);
    var bayEndWorld = ToGamePosition(bayEndGeo, origin);

    var parking1 = Road.Add(map, sCompW, bayMidWorld, "ger1", 20, 20);
    ApplyUrbanRoadStyle(parking1, PortoAlegrePilot.SideRoadStyle);

    var parking2 = parking1.Append(bayEndWorld);
    ApplyUrbanRoadStyle(parking2, PortoAlegrePilot.SideRoadStyle);

    // --- Realistic multi-truck delivery bay (L + short fingers/stubs) ---
    // ?? POR QUE ESSE LOOP (for dos fingers/stubs)?
    // - Regra do projeto: "nem todas empresas, mas as maiores onde pode ter entrega de mercadorias".
    // - Acesso lateral que vira "rua cega" nao simula bem area de carga real de empresa grande
    //   (ex: local de eventos na Orla, retail/distribuicao na Praia de Belas, etc.).
    // - Feedback anterior: bay pequeno demais. Agora criamos fingers curtos perpendiculares
    //   no final do L-bay para dar espaco de manobra/estacionamento para varios caminhoes
    //   carregando ao mesmo tempo (realismo de entrega).
    // - O loop (FINGER_COUNT=2) gera stubs espaçados usando direcao do proprio bay (bayDir)
    //   + rotacao 90 graus (fingerPerp). Isso mantem os fingers "presos" ao bay sem
    //   cruzar o trace principal ou outras ruas (causa do "rua passando por cima" circulado).
    // - Diferente do codigo anterior (magic Vector3(40,0,-30) arbitrario que ignorava
    //   orientacao local do bay e gerava overlaps).
    // - Tamanhos pequenos + TerrainSize=12/20 ajudam a pintar asfalto; ainda assim,
    //   grama em cima do asfalto em juncoes complexas costuma precisar de retoque manual
    //   no editor (ferramentas de terrain: lower/flatten/smooth + paint) apos Recompute map.
    //   Isso e limitacao do TruckLib (foca em items/roads/prefabs) + heightmap do jogo.
    var bayDir = Vector3.Normalize(bayEndWorld - bayMidWorld);
    var fingerPerp = new Vector3(-bayDir.Z, 0, bayDir.X); // 90deg; se cruzar main no futuro, troque o sinal aqui ou compute cross com sideDir
    const int FINGER_COUNT = 2;
    float fingerLen = 22f;
    float fingerStep = 28f;
    var fingerStartBase = bayEndWorld + bayDir * 5f;
    for (int s = 0; s < FINGER_COUNT; s++)
    {
        var basePos = fingerStartBase + bayDir * (s * fingerStep);
        var fingerEnd = basePos + fingerPerp * fingerLen;
        var finger = Road.Add(map, basePos, fingerEnd, "ger1", 12, 12);
        ApplyUrbanRoadStyle(finger, PortoAlegrePilot.SideRoadStyle);
    }

    Console.WriteLine($"[Junction] Real trace split + T-junction prefab + FIRST COMPANY ACCESS generated.");
    Console.WriteLine($"  Junction at index {juncIdx}");
    Console.WriteLine($"  Side uses smart node assignment + launch in node rotation for best encaixe.");
    Console.WriteLine($"  Company entrance + L-shaped parking bay + {FINGER_COUNT} delivery fingers (stubs) for major company maneuvering.");

    // Additional company access (example: near Praia de Belas for retail delivery)
    // Focus on major delivery points as requested.
    if (points.Count > 15)
    {
        var c2Idx = 15; // adjust as needed for real location
        var c2Geo = points[c2Idx];
        var c2Pos = ToGamePosition(c2Geo, origin);

        // Small perpendicular side for company (using similar logic, simplified)
        // Direction roughly perpendicular (tune sign for side)
        // REDUCED magnitudes vs previous to avoid "rua passando por cima" (large arbitrary offsets crossed main trace).
        // This is still placeholder; later pick exact lat/lon of real large delivery spots from OSM near the trace.
        var c2SideMid = c2Pos + new Vector3(-30, 0, 12);
        var c2Company = c2Pos + new Vector3(-55, 0, 20);

        var c2First = Road.Add(map, c2Pos, c2SideMid, "ger1", 10, 10);
        ApplyUrbanRoadStyle(c2First, PortoAlegrePilot.SideRoadStyle);

        var c2Second = c2First.Append(c2Company);
        ApplyUrbanRoadStyle(c2Second, PortoAlegrePilot.SideRoadStyle);

        // Small bay + one finger for this company too (short, safe, consistent style)
        var c2BayEnd = c2Company + new Vector3(-12, 0, 18);
        var c2BayRoad = Road.Add(map, c2Company, c2BayEnd, "ger1", 10, 10);
        ApplyUrbanRoadStyle(c2BayRoad, PortoAlegrePilot.SideRoadStyle);

        Console.WriteLine($"  Additional company access added near trace index {c2Idx} (Praia area retail).");
    }

    Console.WriteLine("Tune with --junction-index and --side-length. This is now the recommended path for first playable company access (crash-safe).");
    Console.WriteLine("Use --no-start-prefab (or omit --with-junction) for pure real trace without any prefab.");
}

static void AttachPrefabricatedLeg(Map map, Prefab prefab, ushort nodeIndex, Vector3[] worldPoints, RoadBlueprint style)
{
    if (worldPoints.Length == 0) return;

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

    // Launch a *short attached stub* from the prefab node using its rotation.
    // This registers the proper attachment ("anexar estrada ao nó do prefab") which is required
    // for the editor to treat it as a real junction instead of just overlapping geometry.
    float launchDist = 30f;
    var launchTarget = node.Position + direction * launchDist;

    var attachedStub = prefab.AppendRoad(nodeIndex, launchTarget, style.RoadTemplate, style.TerrainSize, style.TerrainSize);
    ApplyUrbanRoadStyle(attachedStub, style);

    // Easing segment in the exact node direction (longer straight-ish part).
    // This lets the road exit the prefab geometry cleanly before following the real trace points.
    // This is the key to avoiding bad encaixe, gaps, and roads crossing the prefab or each other (as marked in your screenshot).
    var easingPoint = launchTarget + direction * 50f;
    var cont = Road.Add(map, launchTarget, easingPoint, style.RoadTemplate, style.TerrainSize, style.TerrainSize);
    ApplyUrbanRoadStyle(cont, style);

    // Now continue with the real trace points.
    var first = worldPoints[0];
    cont = cont.Append(first);
    ApplyUrbanRoadStyle(cont, style);

    for (int i = 1; i < worldPoints.Length; i++)
    {
        cont = cont.Append(worldPoints[i]);
        ApplyUrbanRoadStyle(cont, style);
    }
}

static void ApplyUrbanRoadStyle(Road road, RoadBlueprint blueprint)
{
    road.Right.Look = blueprint.Look;
    road.Right.Variant = blueprint.Variant;
    road.Right.LeftEdge = blueprint.Edge;
    road.Right.RightEdge = blueprint.Edge;
    road.Left.Look = blueprint.Look;
    road.Left.Variant = blueprint.Variant;
    road.Left.LeftEdge = blueprint.Edge;
    road.Left.RightEdge = blueprint.Edge;

    foreach (var side in new[] { road.Left, road.Right })
    {
        side.Terrain.QuadData.BrushMaterials[0] = new Material("34");
        side.Terrain.Profile = "profile12";
        side.Terrain.Noise = TerrainNoise.Percent0;
        side.Terrain.Coefficient = 0.25f;
    }
}

static Vector3 ToGamePosition(GeoPoint point, GeoPoint origin)
{
    const double metersPerDegreeLat = 111_320.0;
    var metersPerDegreeLon = metersPerDegreeLat * Math.Cos(origin.Latitude * Math.PI / 180.0);

    var eastMeters = (point.Longitude - origin.Longitude) * metersPerDegreeLon;
    var northMeters = (point.Latitude - origin.Latitude) * metersPerDegreeLat;

    return new Vector3(
        (float)(eastMeters / Scale),
        0,
        (float)(-northMeters / Scale));
}

internal readonly record struct GeoPoint(double Latitude, double Longitude);

internal sealed record RoadBlueprint(
    string Name,
    string RoadTemplate,
    string Look,
    string Variant,
    string Edge,
    float TerrainSize,
    GeoPoint[] Points);

internal static class PortoAlegrePilot
{
    private const string RoadTemplate = "ger4";
    private const string Look = "ger_1";
    private const string Variant = "broken_de";
    private const string Edge = "ger_sh_15";

    public static readonly RoadBlueprint SideRoadStyle = new(
        "Urban side road style",
        "ger1",
        Look,
        Variant,
        Edge,
        20,  // increased for better terrain coverage around junctions/prefabs to prevent grass leaking on asphalt
        []);

    public static readonly RoadBlueprint MainRoadStyle = new(
        "Urban main road style",
        RoadTemplate,
        Look,
        Variant,
        Edge,
        20,  // increased for better terrain coverage around junctions/prefabs to prevent grass leaking on asphalt
        []);

    public static readonly RoadBlueprint[] Roads =
    [
        new(
            "Corredor inicial Porto Alegre 01-A",
            RoadTemplate,
            Look,
            Variant,
            Edge,
            12,
            [
                // Gasometro / ponta oeste
                new GeoPoint(-30.03280, -51.24230),
                // Orla Moacyr Scliar
                new GeoPoint(-30.03510, -51.24040),
                new GeoPoint(-30.03930, -51.23720),
                new GeoPoint(-30.04460, -51.23370),
                new GeoPoint(-30.04950, -51.23070),
                // Praia de Belas / shopping
                new GeoPoint(-30.05010, -51.22870),
                new GeoPoint(-30.04820, -51.22680),
                new GeoPoint(-30.04520, -51.22640),
                // Borges / Centro
                new GeoPoint(-30.04020, -51.22620),
                new GeoPoint(-30.03480, -51.22610),
                new GeoPoint(-30.03020, -51.22600),
                // retorno simplificado por Loureiro/Presidente Joao Goulart.
                // Nao repetir o primeiro ponto no final: isso cria um no duplicado
                // no Map Editor e gera remendos de terreno/asfalto.
                new GeoPoint(-30.03120, -51.23180),
                new GeoPoint(-30.03240, -51.23660)
            ])
    ];
}
