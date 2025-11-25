using UnityEngine;

public class NorthSouthRoadGenerator : MonoBehaviour
{
    [Header("Ustawienia wejœciowe")]
    [SerializeField] private Transform player;                 // pojazd / kamera / XRRig
    [SerializeField] private GameObject[] terrainPrefabs;      // prefaby z komponentem Terrain
    [SerializeField] private float segmentLength = 20f;        // fallback; nadpisywane z TerrainData.size.z
    [SerializeField] private Transform parentForSegments;      

    [Header("Losowanie")]
    [Tooltip("Jeœli true, nie wylosuje dwa razy pod rz¹d tego samego prefabu.")]
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Terrain")]
    [Tooltip("Uœrednia wysokoœci na styku po obu stronach, zamiast kopiowaæ jednostronnie.")]
    [SerializeField] private bool stitchAverageBothSides = true;

    // 0=po³udniowy 1=centralny 2=pó³nocny
    private GameObject[] activeSegments = new GameObject[3];
    private int centerIndex;
    private int lastChosenPrefab = -1;

    private float UpperBoundaryZ => (centerIndex + 0.5f) * segmentLength;
    private float LowerBoundaryZ => (centerIndex - 0.5f) * segmentLength;

    private void Reset()
    {
        if (!player && Camera.main) player = Camera.main.transform;
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError($"{nameof(NorthSouthRoadGenerator)}: Brak referencji do 'Player'.");
            enabled = false; return;
        }
        if (terrainPrefabs == null || terrainPrefabs.Length == 0)
        {
            Debug.LogError($"{nameof(NorthSouthRoadGenerator)}: Brak przypisanych 'Terrain Prefabs'.");
            enabled = false; return;
        }

        var firstT = GetTerrain(terrainPrefabs[0]);
        if (firstT == null)
        {
            Debug.LogError("Pierwszy prefab nie ma komponentu Terrain.");
            enabled = false; return;
        }
        var td0 = firstT.terrainData;
        for (int i = 1; i < terrainPrefabs.Length; i++)
        {
            var t = GetTerrain(terrainPrefabs[i]);
            if (t == null) { Debug.LogError($"Prefab [{i}] nie ma komponentu Terrain."); enabled = false; return; }
            var td = t.terrainData;
            if (td.heightmapResolution != td0.heightmapResolution ||
                td.size != td0.size)
            {
                Debug.LogError($"Prefab [{i}] ma inn¹ specyfikacjê TerrainData (resolution/size). Ustandaryzuj kafle.");
                enabled = false; return;
            }
        }

        segmentLength = td0.size.z;

        InitializeSegments();
    }

    private void Update()
    {
        float z = player.position.z;
        if (z > UpperBoundaryZ) ShiftNorth();
        else if (z < LowerBoundaryZ) ShiftSouth();
    }

    private void InitializeSegments()
    {
        centerIndex = Mathf.FloorToInt(player.position.z / segmentLength);

        for (int i = 0; i < activeSegments.Length; i++)
        {
            if (activeSegments[i] != null) Destroy(activeSegments[i]);
            activeSegments[i] = null;
        }

        SpawnAtSlot(0, centerIndex - 1); 
        SpawnAtSlot(1, centerIndex); 
        SpawnAtSlot(2, centerIndex + 1);

        WireNeighbors();
        TryStitchNorthOfCenter();
        TryStitchSouthOfCenter();
    }

    private void ShiftNorth()
    {
        if (activeSegments[0] != null) Destroy(activeSegments[0]);
        activeSegments[0] = activeSegments[1];
        activeSegments[1] = activeSegments[2];
        activeSegments[2] = null;

        centerIndex += 1;

        SpawnAtSlot(2, centerIndex + 1);
        WireNeighbors();
        TryStitchNorthOfCenter();
    }

    private void ShiftSouth()
    {
        if (activeSegments[2] != null) Destroy(activeSegments[2]);
        activeSegments[2] = activeSegments[1];
        activeSegments[1] = activeSegments[0];
        activeSegments[0] = null;

        centerIndex -= 1;

        SpawnAtSlot(0, centerIndex - 1);
        WireNeighbors();
        TryStitchSouthOfCenter();
    }

    private void SpawnAtSlot(int slot, int tileIndex)
    {
        GameObject prefab = PickRandomPrefab();

        Vector3 pos = new Vector3(0f, 0f, tileIndex * segmentLength);
        Quaternion rot = Quaternion.identity;

        GameObject go = Instantiate(prefab, pos, rot, parentForSegments);
        go.name = $"{prefab.name}_tile{tileIndex}";
        activeSegments[slot] = go;

        var t = GetTerrain(go);
        if (t != null)
        {
            t.transform.position = pos;
        }

        if (t != null && activeSegments[1] != null)
        {
            var tCenter = GetTerrain(activeSegments[1]);
            if (slot == 2 && tCenter != null)
            {
                TerrainSeamTools.StitchNorthSouth(tCenter, t, stitchAverageBothSides);
            }
            else if (slot == 0 && tCenter != null)
            {
                TerrainSeamTools.StitchNorthSouth(t, tCenter, stitchAverageBothSides);
            }
        }
    }

    private void WireNeighbors()
    {
        var south = GetTerrain(activeSegments[0]);
        var center = GetTerrain(activeSegments[1]);
        var north = GetTerrain(activeSegments[2]);

        if (south) south.SetNeighbors(null, center, null, null);
        if (center) center.SetNeighbors(null, north, null, south);
        if (north) north.SetNeighbors(null, null, null, center);

        if (south) { south.allowAutoConnect = true; }
        if (center) { center.allowAutoConnect = true; }
        if (north) { north.allowAutoConnect = true; }

        int gid = center ? center.groupingID : 0;
        if (south) south.groupingID = gid;
        if (north) north.groupingID = gid;
    }

    private void TryStitchNorthOfCenter()
    {
        var c = GetTerrain(activeSegments[1]);
        var n = GetTerrain(activeSegments[2]);
        if (c && n)
            TerrainSeamTools.StitchNorthSouth(c, n, stitchAverageBothSides);
    }

    private void TryStitchSouthOfCenter()
    {
        var s = GetTerrain(activeSegments[0]);
        var c = GetTerrain(activeSegments[1]);
        if (s && c)
            TerrainSeamTools.StitchNorthSouth(s, c, stitchAverageBothSides);
    }

    private GameObject PickRandomPrefab()
    {
        if (terrainPrefabs.Length == 1) return terrainPrefabs[0];

        int idx;
        if (avoidImmediateRepeat)
        {
            do { idx = Random.Range(0, terrainPrefabs.Length); }
            while (idx == lastChosenPrefab);
        }
        else { idx = Random.Range(0, terrainPrefabs.Length); }

        lastChosenPrefab = idx;
        return terrainPrefabs[idx];
    }

    private static Terrain GetTerrain(GameObject go)
    {
        if (!go) return null;
        return go.GetComponent<Terrain>();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (segmentLength <= 0f) return;
        int previewCenter = Application.isPlaying ? centerIndex : Mathf.FloorToInt((player ? player.position.z : 0f) / segmentLength);
        float lower = (previewCenter - 0.5f) * segmentLength;
        float upper = (previewCenter + 0.5f) * segmentLength;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-5, 0, lower), new Vector3(5, 0, lower));
        Gizmos.DrawLine(new Vector3(-5, 0, upper), new Vector3(5, 0, upper));
        UnityEditor.Handles.Label(new Vector3(0, 0, lower), "LowerBoundary (S)");
        UnityEditor.Handles.Label(new Vector3(0, 0, upper), "UpperBoundary (N)");
    }
#endif
}

/// <summary>
/// Narzêdzia do zszywania krawêdzi heightmap dla Terrainów (N-S).
/// Zak³ada identyczne heightmapResolution i TerrainData.size po obu stronach.
/// </summary>
public static class TerrainSeamTools
{
    public static void StitchNorthSouth(Terrain southCentral, Terrain northNew, bool averageBothSides)
    {
        if (!southCentral || !northNew) return;

        var a = southCentral.terrainData; // pó³nocna krawêdŸ A
        var b = northNew.terrainData;     // po³udniowa krawêdŸ B

        if (a.heightmapResolution != b.heightmapResolution)
        {
            Debug.LogError("TerrainSeamTools: ró¿ne heightmapResolution – nie mo¿na zszyæ.");
            return;
        }
        if (a.size != b.size)
        {
            Debug.LogError("TerrainSeamTools: ró¿ne TerrainData.size – nie mo¿na zszyæ.");
            return;
        }

        int res = a.heightmapResolution; // liczba próbek (rows/cols)
        int lastRowA = res - 1;          // pó³noc A
        int firstRowB = 0;               // po³udnie B

        // Pobierz paski (height=1, width=res). Uwaga: indeksowanie [y,x].
        float[,] aEdge = a.GetHeights(0, lastRowA, res, 1);
        float[,] bEdge = b.GetHeights(0, firstRowB, res, 1);

        for (int x = 0; x < res; x++)
        {
            float h = averageBothSides ? (aEdge[0, x] + bEdge[0, x]) * 0.5f : aEdge[0, x];
            aEdge[0, x] = h;
            bEdge[0, x] = h;
        }

        a.SetHeights(0, lastRowA, aEdge);
        b.SetHeights(0, firstRowB, bEdge);

        // Po modyfikacjach warto odœwie¿yæ (opcjonalnie).
        Terrain.activeTerrain?.Flush();
    }
}
