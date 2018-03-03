﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{


	public int mapWidth;
	public int mapHeight;

	public float noiseScale;

	public int octaves;
	[Range(0, 1)] public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;


	public Terrain _terrain;
	public int HeightMapResolution;


	// Update is called once per frame
	public void GenerateTerrain()
	{
		float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity,
			new Vector2(0, 0) + offset);

		TerrainData terrainData = new TerrainData();
		
		
		terrainData.heightmapResolution = HeightMapResolution;
		//terrainData.baseMapResolution = 1024;
		//terrainData.SetDetailResolution(1024,terrainData.detailResolution);

		terrainData.size = new Vector3(mapWidth,meshHeightMultiplier, mapHeight);
		terrainData.SetHeights(0, 0, noiseMap);

		

		_terrain.terrainData = terrainData;
		_terrain.GetComponent<TerrainCollider>().terrainData = _terrain.terrainData;

	}

}