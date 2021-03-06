﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using ProceduralWorlds.Core;
using ProceduralWorlds.Biomator;
using ProceduralWorlds.Node;
using System;
using System.Linq;
using System.Diagnostics;
using ProceduralWorlds.Biomator.SwitchGraph;

using Sampler = ProceduralWorlds.Core.Sampler;
using Debug = UnityEngine.Debug;

namespace ProceduralWorlds.Biomator
{
	public class BiomeParamRange
	{
		public Vector2[]	ranges = new Vector2[BiomeData.maxBiomeSamplers];

		public void Clear()
		{
			for (int i = 0; i < ranges.Length; i++)
				ranges[i] = new Vector2(float.MaxValue, float.MinValue);
		}
	}

	public class BiomeSwitchGraph
	{
		public BiomeSwitchCell				rootCell;
		public BiomeSwitchCell				lastCell;
		public List< BiomeSwitchCell >		cells = new List< BiomeSwitchCell >();
		public List< Sampler >				builtSamplers = new List< Sampler >();

		[System.NonSerialized]
		public bool							isBuilt = false;

		[System.NonSerialized]
		short								biomeIdCount = 0;

		[System.NonSerialized]
		Dictionary< string, PartialBiome >	partialBiomePerName = new Dictionary< string, PartialBiome >();
		[System.NonSerialized]
		Dictionary< short, PartialBiome >	partialBiomePerId = new Dictionary< short, PartialBiome >();
		[System.NonSerialized]
		Dictionary< int, float >			biomeCoverage = new Dictionary< int, float >();

		[System.NonSerialized]
		public BiomeParamRange				paramRanges = new BiomeParamRange();

		public bool BuildGraph(BiomeData biomeData)
		{
			ResetGraph();

			BaseNode rootNode = biomeData.biomeSwitchGraphStartPoint;

			Stack< BaseNode > biomeNodes = new Stack< BaseNode >();

			biomeNodes.Push(rootNode);

			BiomeSwitchCell currentCell = null;

			//fill param range dictionary (used to compute weights)
			FillParamRange(biomeData);

			while (biomeNodes.Count > 0)
			{
				BaseNode	currentNode = biomeNodes.Pop();
				Type		nodeType = currentNode.GetType();

				if (nodeType == typeof(NodeBiome))
				{
					//get all precedent switch data in the order of the tree (start from rootNode)
					var precedentSwitchDatas = GetPrecedentSwitchDatas(currentNode, rootNode);

					var biomeNode = currentNode as NodeBiome;
					var lastSwitch = precedentSwitchDatas.Last();
					currentCell = new BiomeSwitchCell();
					
					//Set cell and partial biome remaining empty params
					currentCell.name = lastSwitch.name;
					biomeNode.outputBiome.name  = lastSwitch.name;
					biomeNode.outputBiome.id = biomeIdCount++;
					currentCell.id = biomeNode.outputBiome.id;
					biomeNode.outputBiome.previewColor = lastSwitch.color;
					currentCell.color = lastSwitch.color;
					currentCell.weight = currentCell.GetWeight(paramRanges);

					//add the partial biome to utility dictionary accessors:
					partialBiomePerName[currentCell.name] = biomeNode.outputBiome;
					partialBiomePerId[currentCell.id] = biomeNode.outputBiome;

					//load all access condition for this biome switch
					foreach (var sd in precedentSwitchDatas)
					{
						int index = biomeData.GetBiomeIndex(sd.samplerName);
						var param = currentCell.switchParams.switchParams[index];

						param.enabled = true;
						param.min = sd.min;
						param.max = sd.max;
						biomeCoverage[index] += (sd.max - sd.min) / (sd.absoluteMax - sd.absoluteMin);
					}
				}
				else if (nodeType == typeof(NodeBiomeBlender))
				{
					if (currentCell == null)
						throw new InvalidOperationException("[PWBiomeSwitchGraph] idk what happened but this is really bad");
					
					//if the flow reaches the biomeblender everything is OK and add the current cell to the graph
					cells.Add(currentCell);
					continue ;
				}

				foreach (var outputNode in currentNode.GetOutputNodes())
					biomeNodes.Push(outputNode);
			}

			//Generate links between all linkable nodes
			BuildLinks();
			
			if (cells.Count != 0)
				rootCell = cells.First();

			if (!CheckValid())
				return false;

			isBuilt = true;

			return true;
		}

		//Create a switchGraph that always return this biome Id
		public void BuildTestGraph(short biomeId)
		{
			BiomeSwitchCell bsc = new BiomeSwitchCell();

			bsc.id = biomeId;
			rootCell = bsc;
			cells.Add(bsc);
		}

		void ResetGraph()
		{
			isBuilt = false;
			biomeIdCount = 0;
			rootCell = null;
			lastCell = null;

			cells.Clear();
			partialBiomePerId.Clear();
			partialBiomePerName.Clear();
			paramRanges.Clear();

			for (int i = 0; i < BiomeData.maxBiomeSamplers; i++)
				biomeCoverage[i] = 0;
		}

		void FillParamRange(BiomeData biomeData)
		{
			builtSamplers.Clear();

			for (int i = 0; i < biomeData.length; i++)
			{
				var dataSampler = biomeData.GetDataSampler(i);

				paramRanges.ranges[i] = new Vector2(dataSampler.dataRef.min, dataSampler.dataRef.max);
				builtSamplers.Add(dataSampler.dataRef);
			}
		}

		List< BiomeSwitchData > GetPrecedentSwitchDatas(BaseNode currentNode, BaseNode rootNode)
		{
			var precedentSwitchDatas = new List< BiomeSwitchData >();

			BaseNode n = currentNode;
			while (n != rootNode)
			{
				BaseNode prevNode = n;
				n = n.GetInputNodes().First();
				if (n.GetType() != typeof(NodeBiomeSwitch))
					break ;
				
				var switches = (n as NodeBiomeSwitch).switchList;
				
				int index = n.outputAnchors.ToList().FindIndex(a => a.links.Any(l => l.toNode == prevNode));

				if (index == -1)
					throw new InvalidOperationException("[PWBiomeSwitchGraph] IMPOSSIBRU !!!!! check your node API !");
				
				precedentSwitchDatas.Add(switches[index]);
			}

			precedentSwitchDatas.Reverse();

			return precedentSwitchDatas;
		}

		void BuildLinks()
		{
			foreach (var cell1 in cells)
			{
				foreach (var cell2 in cells)
				{
					if (cell1 == cell2)
						continue ;
					
					bool b = (cell1.Overlaps(cell2.switchParams));

					if (b)
					{
						if (!cell1.links.Any(c1 => c1 == cell2))
							cell1.links.Add(cell2);
					}
				}

				cell1.links.Sort((c1, c2) => {
					//reverse sort:
					return c2.weight.CompareTo(c1.weight);
				});
			}
		}

		bool CheckValid()
		{
			var	checkedCells = new List< BiomeSwitchCell >();
			var currentCells = new Stack< BiomeSwitchCell >();

			if (rootCell == null)
				return false;

			currentCells.Push(rootCell);

			while (currentCells.Count > 0)
			{
				var currentCell = currentCells.Pop();

				if (!checkedCells.Contains(currentCell))
					checkedCells.Add(currentCell);
				
				foreach (var link in currentCell.links)
					if (!checkedCells.Contains(link))
						currentCells.Push(link);
			}
			
			//if all graph cells are contained in the checkedCell list then it's good
			if (cells.All(cell => checkedCells.Contains(cell)))
				return true;
			
			return false;
		}

		BiomeSwitchCell		CheckForBetterCell(BiomeSwitchCell cell, BiomeSwitchValues values)
		{
			//check the current node links and find if there is a better solution than the current
			foreach (var link in cell.links)
				if (link.Matches(values))
					if (link.weight < cell.weight)
						return link;
			
			return null;
		}

		BiomeSwitchCell		FindBestCell(BiomeSwitchCell cell, BiomeSwitchValues values)
		{
			BiomeSwitchCell	bestCell = null;
			float			minGap = 1e20f;

			//return the best node to get close to the final one
			foreach (var link in cell.links)
			{
				if (link.Matches(values))
					return link;
				
				float gap = link.GapWidth(cell);
				if (gap > 0 && gap < minGap)
				{
					minGap = gap;
					bestCell = link;
				}
			}

			return bestCell;
		}

		public BiomeSwitchCell	FindBiome(BiomeSwitchValues values)
		{
			BiomeSwitchCell currentCell = (lastCell == null) ? rootCell : lastCell;

			if (!isBuilt || currentCell == null)
				return null;
			
			//since there is no default state, we can do this:
			if (currentCell.Matches(values))
				return currentCell;
			
			int maxSearchDepth = 100;
			int	i = 0;
			
			while (true)
			{
				if (i > maxSearchDepth)
				{
					Debug.LogError("[BiomeSurfaceGraph] Infinite loop detected, exiting ...");
					return null;
				}
				
				i++;
				if (currentCell.Matches(values))
				{
					//try to find a better link
					var betterCell = CheckForBetterCell(currentCell, values);
					
					//if there is nothing better, we've the solution
					if (betterCell == null)
					{
						lastCell = currentCell;
						return currentCell;
					}
					else
						currentCell = betterCell;
				}
				else
				{
					//find the best link to take to get near from the node we look for
					currentCell = FindBestCell(currentCell, values);

					if (currentCell == null)
						return null;
				}
			}
		}

		public void FillBiomeMap(BiomeData biomeData, BiomeBlendList blendMatrix, float blendPercent = .15f)
		{
			Sampler	terrain = biomeData.GetSampler(BiomeSamplerName.terrainHeight);
			var		biomeSwitchValues = new BiomeSwitchValues();

			if (!isBuilt)
			{
				Debug.LogError("Biome switch graph is not built, you can't fill the biome map !");
				return ;
			}

			//TODO: 3D Biome fill map
			
			if (biomeData.biomeMap == null || biomeData.biomeMap.NeedResize(terrain.size, terrain.step))
				biomeData.biomeMap = new BiomeMap2D(terrain.size, terrain.step);

			Profiler.BeginSample("FillBiomeMap");

			var blendParams = new BiomeSwitchCellParams();

			//getter for range (faster than dictionary)
			float[] ranges = new float[biomeData.length];
			for (int i = 0; i < biomeData.length; i++)
				ranges[i] = paramRanges.ranges[i].y - paramRanges.ranges[i].x;

			for (int x = 0; x < terrain.size; x++)
				for (int y = 0; y < terrain.size; y++)
				{
					//fill biomeSwitchValue and blendParams with the current biomeData sampler values
					for (int i = 0; i < biomeData.length; i++)
					{
						biomeSwitchValues[i] = biomeData.biomeSamplers[i].data2D[x, y];
						var val = biomeSwitchValues[i];
						var r = ranges[i];
						var spc = blendParams.switchParams[i];
						spc.min = val - (r * blendPercent);
						spc.max = val + (r * blendPercent);
						spc.enabled = true;
					}

					var		biomeSwitchCell = FindBiome(biomeSwitchValues);

					if (biomeSwitchCell == null)
					{
						Debug.LogError("Biome can't be found for values: " + biomeSwitchValues);

						foreach (var c in cells)
							Debug.Log("c: " + c.name + " -> " + c.switchParams);
						return ;
					}

					short	biomeId = biomeSwitchCell.id;
					
					biomeData.ids.Add(biomeId);
					
					biomeData.biomeMap.SetPrimaryBiomeId(x, y, biomeId);

					//add biome that can be blended with the primary biome,
					if (blendPercent > 0)
						foreach (var link in biomeSwitchCell.links)
						{
							if (link.Overlaps(blendParams))
							{
								float blend = link.ComputeBlend(blendMatrix, paramRanges, biomeSwitchValues, blendPercent);
								if (blend > 0.001f)
									biomeData.biomeMap.AddBiome(x, y, link.id, blend);
							}
						}
				}

			Profiler.EndSample();
		}

		public int GetBiomeCount()
		{
			return cells.Count;
		}

		public PartialBiome GetBiome(string name)
		{
			PartialBiome	ret;

			partialBiomePerName.TryGetValue(name, out ret);

			return ret;
		}

		public PartialBiome GetBiome(short id)
		{
			PartialBiome	ret;

			partialBiomePerId.TryGetValue(id, out ret);

			return ret;
		}

		public Dictionary< int, float > GetBiomeCoverage()
		{
			return biomeCoverage;
		}
	
	}
}