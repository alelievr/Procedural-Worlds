﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProceduralWorlds.Core;

namespace ProceduralWorlds
{
	[System.Serializable]
	public abstract class BaseTerrain< T > : GenericBaseTerrain  where T : ChunkData, new()
	{
		//Generic to specif bindings:
		protected override ChunkData RequestChunkGeneric(Vector3 pos, int seed) { return RequestChunk(pos, seed); }
		protected override object OnChunkCreateGeneric(ChunkData terrainData, Vector3 pos) { return OnChunkCreate(terrainData as T, pos); } 
		protected override void OnChunkRenderGeneric(ChunkData terrainData, object userStoredObject, Vector3 pos) { OnChunkRender(terrainData as T, userStoredObject, pos); } 
		protected override void OnChunkDestroyGeneric(ChunkData terrainData, object userStoredObject, Vector3 pos) { OnChunkDestroy(terrainData as T, userStoredObject, pos); } 
		protected override void OnChunkHideGeneric(ChunkData terrainData, object userStoredObject, Vector3 pos) { OnChunkHide(terrainData as T, userStoredObject, pos); }
		protected override object RequestCreateGeneric(ChunkData terrainData, Vector3 pos) { return RequestCreate(terrainData as T, pos); }

		protected T RequestChunk(Vector3 pos, int seed)
		{
			if (seed != oldSeed)
				graph.seed = seed;

			graph.chunkPosition = pos;
			graph.Process();

			oldSeed = seed;
			WorldChunk worldChunk = graph.GetOutputTerrain();

			if (worldChunk == null)
			{
				Debug.LogWarning("[BaseTerrain] Graph output terrain is null !");
				return null;
			}

			seamlessTerrain.AddChunk(pos, worldChunk);
			
			return CreateChunkData(worldChunk, pos);
		}

		protected virtual T CreateChunkData(WorldChunk terrain, Vector3 pos)
		{
			T chunk = new T();

			FillChunkData(chunk, terrain, pos);
			return chunk;
		}

		protected abstract object OnChunkCreate(T terrainData, Vector3 pos);
		protected abstract void OnChunkRender(T terrainData, object userStoredObject, Vector3 pos);
		protected abstract void OnChunkDestroy(T terrainData, object userStoredObject, Vector3 pos);

		protected virtual void OnChunkHide(T terrainData, object userStoredObject, Vector3 pos) {}

		protected object RequestCreate(T terrainData, Vector3 pos)
		{
			var userData = OnChunkCreate(terrainData, pos);
			if (terrainStorage == null)
				return userData;
			if (terrainStorage.isLoaded(pos))
				terrainStorage[pos].userData = userData;
			else
				terrainStorage.AddChunk(pos, terrainData, userData);
			return userData;
		}

		protected void FillChunkData(ChunkData chunk, WorldChunk worldChunk, Vector3 pos)
		{
			chunk.size = worldChunk.mergedTerrain.size;
			chunk.materializerType = worldChunk.materializerType;
			chunk.terrain = worldChunk.mergedTerrain.Clone(null);
			chunk.biomeMap = worldChunk.biomeData.biomeMap;
			chunk.position = pos;
			chunk.biomeMap3D = null;
		}
	}
}