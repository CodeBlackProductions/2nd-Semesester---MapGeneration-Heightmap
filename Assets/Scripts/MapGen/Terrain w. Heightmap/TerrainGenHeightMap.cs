using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenHeightMap : MonoBehaviour
{
    #region Fields

    [SerializeField] private int mapWidth = 513;
    [SerializeField] private int mapHeight = 513;
    [SerializeField] [Range(0, 0.025f)] private float frequency = 0.01f;
    [SerializeField] [Range(0, 0.5f)] private float amplitude = 0.20f;
    [SerializeField] [Range(0, 0.20f)] private float baseLevel = 0;
    [SerializeField] [Range(0, 10)] private int octaves = 5;

    private Terrain terrain;

    private Texture2D heightMap;

    private float oldFrequency = 0.01f;
    private float oldAmplitude = 0.20f;
    private float oldBaseLevel = 0;
    private int oldOctaves = 5;

    #endregion Fields

    private void Awake()
    {
        terrain = this.GetComponent<Terrain>();
        GenerateHeightMap();

        heightMap.wrapMode = TextureWrapMode.Repeat;

        ApplyToTerrain();
    }

    private void OnValidate()
    {
        terrain = this.GetComponent<Terrain>();
        GenerateHeightMap();

        heightMap.wrapMode = TextureWrapMode.Repeat;

        ApplyToTerrain();
    }

    private void Update()
    {
        if (oldFrequency != frequency || oldAmplitude != amplitude || oldBaseLevel != baseLevel || oldOctaves != octaves)
        {
            oldFrequency = frequency;
            oldAmplitude = amplitude;
            oldBaseLevel = baseLevel;
            oldOctaves = octaves;
            GenerateHeightMap();
            ApplyToTerrain();
        }
    }

    private void Start()
    {
        PerformanceChecker.CheckMethod(GenerateHeightMap,10);
    }

    //Base Heightmap
    private void GenerateHeightMap()
    {
        Texture2D perlinTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, true);
        perlinTexture.name = "Procedural Texture";
        Color perlinColor = new Color(0, 0, 0);
        float offset = System.DateTime.Now.Millisecond;

        NativeArray<PixelData> values = new NativeArray<PixelData>(mapWidth * mapHeight, Allocator.Persistent);

        int count = 0;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                values[count] = new PixelData(x, y, 0);
                count++;
            }
        }

        var job = new HeightmapJob(values, octaves, offset, baseLevel, amplitude, frequency);

        var jobHandle = job.Schedule(mapWidth * mapHeight, 1);

        jobHandle.Complete();

        for (int i = 0; i < values.Length; i++)
        {
            perlinColor = Color.Lerp(Color.black, Color.white, values[i].P_value);
            perlinTexture.SetPixel(values[i].P_x, values[i].P_y, perlinColor);
        }

        perlinTexture.Apply();
        heightMap = perlinTexture;

        values.Dispose();
    }

    //Apply
    public void ApplyToTerrain()
    {
        float[,] heightArray = new float[heightMap.height, heightMap.width];
        for (int y = 0; y < heightMap.height; y++)
        {
            for (int x = 0; x < heightMap.width; x++)
            {
                heightArray[y, x] = heightMap.GetPixel(x, y).grayscale;
            }
        }
        terrain.terrainData.SetHeights(0, 0, heightArray);
    }

    public struct HeightmapJob : IJobParallelFor
    {
        private int J_octaves;
        private float J_offset;
        private float J_baseLevel;
        private float J_amplitude;
        private float J_frequency;

        private NativeArray<PixelData> J_values;

        public HeightmapJob(NativeArray<PixelData> values, int octaves, float offset, float baseLevel, float amplitude, float frequency)
        {
            J_values = values;

            J_octaves = octaves;
            J_offset = offset;
            J_baseLevel = baseLevel;
            J_amplitude = amplitude;
            J_frequency = frequency;
        }

        public void Execute(int index)
        {
            float perlinValue = 0;
            float scale = 1;
            float localFrequency = J_frequency;

            for (int i = 0; i < J_octaves; i++)
            {
                perlinValue += scale * (Mathf.PerlinNoise((J_values[index].P_x + J_offset) * localFrequency, (J_values[index].P_y + J_offset) * localFrequency) * J_amplitude + J_baseLevel);

                scale *= 0.5f;
                localFrequency *= 2;
            }

            perlinValue = (J_baseLevel * J_octaves) + perlinValue * (((1 * J_amplitude + J_baseLevel) * J_octaves) - (J_baseLevel * J_octaves));
            PixelData result = J_values[index];
            result.P_value = perlinValue;
            J_values[index] = result;
        }
    }

    public struct PixelData
    {
        public int P_x;
        public int P_y;
        public float P_value;

        public PixelData(int x, int y, float value)
        {
            P_x = x;
            P_y = y;
            P_value = value;
        }
    }
}