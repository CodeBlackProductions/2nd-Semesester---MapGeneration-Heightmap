using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenHeightMapNoOpt : MonoBehaviour
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
        PerformanceChecker.CheckMethod(GenerateHeightMap, 10);
    }

    //Base Heightmap
    private void GenerateHeightMap()
    {
        Texture2D perlinTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, true);
        perlinTexture.name = "Procedural Texture";
        Color perlinColor = new Color(0, 0, 0);
        float offset = System.DateTime.Now.Millisecond;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float perlinValue = 0;
                float scale = 1;
                float localFrequency = frequency;

                for (int i = 0; i < octaves; i++)
                {
                    perlinValue += scale * (Mathf.PerlinNoise((x + offset) * localFrequency, (y + offset) * localFrequency) * amplitude + baseLevel);
                    scale *= 0.5f;
                    localFrequency *= 2;
                }

                perlinValue = (baseLevel * octaves) + perlinValue * (((amplitude + baseLevel) * octaves) - (baseLevel * octaves));

                perlinColor = Color.Lerp(Color.black, Color.white, perlinValue);
                perlinTexture.SetPixel(x, y, perlinColor);
            }
        }

        perlinTexture.Apply();
        heightMap = perlinTexture;
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
}