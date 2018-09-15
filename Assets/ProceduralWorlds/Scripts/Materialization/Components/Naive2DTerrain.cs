﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProceduralWorlds;
using ProceduralWorlds.Core;
using ProceduralWorlds.Biomator;
using ProceduralWorlds.IsoSurfaces;

public class Naive2DTerrain : BaseTerrain< TopDownChunkData >
{
	public float						yPosition;
	public Naive2DIsoSurfaceSettings	isoSettings = new Naive2DIsoSurfaceSettings();

	Gradient	rainbow;

	readonly Naive2DIsoSurface	isoSurface = new Naive2DIsoSurface();

	protected override void OnTerrainEnable()
	{
		//global settings, not depending from the editor
		generateBorders = true;
		neighbourMessageMode = NeighbourMessageMode.Mode2DXY;
		isoSettings.normalMode = NormalGenerationMode.Shared;
	}

	void	UpdateMeshDatas(Mesh mesh, BiomeMap2D biomes)
	{
		List< Vector4 >		blendInfos = new List< Vector4 >();

		for (int x = 0; x < chunkSize; x++)
			for (int z = 0; z < chunkSize; z++)
			{
				Vector4 biomeInfo = Vector4.zero;
				blendInfos.Add(biomeInfo);
			}
		mesh.SetUVs(1, blendInfos);
	}
	
	protected override object	OnChunkCreate(TopDownChunkData chunk, Vector3 pos)
	{
		if (rainbow == null)
			rainbow = Utils.CreateRainbowGradient();

		pos = GetChunkWorldPosition(pos);
		
		GameObject g = CreateChunkObject(pos);
		
		MeshRenderer mr = g.AddComponent< MeshRenderer >();
		MeshFilter mf = g.AddComponent< MeshFilter >();
		
		isoSettings.Update(chunk.size, chunk.terrain as Sampler2D);

		Mesh m = isoSurface.Generate(isoSettings);
			
		UpdateMeshDatas(m, chunk.biomeMap);

		mf.sharedMesh = m;

		Shader standardShader = Shader.Find("Standard");
		Material mat = new Material(standardShader);
		mr.sharedMaterial = mat;
		return g;
	}

	protected override void 	OnChunkDestroy(TopDownChunkData terrainData, object userStoredObject, Vector3 pos)
	{
		GameObject g = userStoredObject as GameObject;

		if (g != null)
			DestroyImmediate(g);
	}

	protected override void	OnChunkRender(TopDownChunkData chunk, object chunkGameObject, Vector3 pos)
	{
		if (chunk == null)
			return ;
		
		GameObject		g = chunkGameObject as GameObject;

		if (g == null) //if gameobject have been destroyed by user and reference was lost.
			RequestCreate(chunk, pos);
	}
	
	protected override Vector3 GetChunkPosition(Vector3 pos)
	{
		pos.y = yPosition;

		return pos;
	}
}
