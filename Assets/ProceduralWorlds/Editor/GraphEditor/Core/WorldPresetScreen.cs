﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralWorlds.Core;
using ProceduralWorlds.Nodes;
using System.IO;

namespace ProceduralWorlds.Editor
{
	public class WorldPresetScreen : PresetScreen
	{
		readonly WorldGraphEditor		worldGraphEditor;
		WorldGraph						worldGraph { get { return worldGraphEditor.worldGraph; } }

		readonly string	graphFilePrefix = "GraphPresets/World/Parts/";
		readonly string biomeAssetPrefix = "GraphPresets/Biome/Full/";
	
		public WorldPresetScreen(WorldGraphEditor worldGraphEditor, bool loadStyle = true)
		{
			this.worldGraphEditor = worldGraphEditor;

			LoadPresetList(LoadPresetBoard());

			if (loadStyle)
				LoadStyle();
		}

		public PresetCellList LoadPresetBoard()
		{
			//loading preset panel images
			Texture2D preset2DSideViewTexture = Resources.Load< Texture2D >("PresetImages/preview2DSideView");
			Texture2D preset2DPlanarTexture = Resources.Load< Texture2D >("PresetImages/preview2DTopDownView");
			Texture2D preset3DPlanarTexture = Resources.Load< Texture2D >("PresetImages/preview3DPlane");
			Texture2D preset3DSphericalTexture = Resources.Load< Texture2D >("PresetImages/preview3DSpherical");
			Texture2D preset3DCubicTexture = Resources.Load< Texture2D >("PresetImages/preview3DCubic");

			//Biomes
			PresetCellList	biomePresets = new PresetCellList
			{
				{"Biome presets"},
				{"Earth like", null, "Biomes/Earth", true}
			};
			
			//ISO surfaces
			PresetCellList terrain2DIsoSurfaces = new PresetCellList
			{
				{"2D Isosurfaces"},
				{"Square", null, "IsoSurfaces/Square", true, biomePresets},
				{"Hexagon", null, "IsoSurfaces/Hexagon", false, biomePresets},
				{"Marching cubes 2D", null, "IsoSurfaces/MarchingCubes2D", false, biomePresets},
				{"Fake voxels", null, "IsoSurfaces/FakeVolxel", false, biomePresets},
			};

			PresetCellList terrain3DIsoSurfaces = new PresetCellList
			{
				{"3D Isosurfaces"},
				{"Marching cubes 3D", null, "IsuSurfaces/MarchingCubes3D", false, biomePresets},
				{"Dual countering 3D", null, "IsoSurfaces/DualCountering", false, biomePresets},
				{"Greedy voxels", null, "IsoSurfaces/GreedyVoxel", false, biomePresets}
			};
			
			//Output type
			PresetCellList terrain3DPresets = new PresetCellList
			{
				{"3D Terrain type"},
				{"3D planar", preset3DPlanarTexture, "TerrainType/Planar3D", false, terrain3DIsoSurfaces},
				{"3D spherical", preset3DSphericalTexture, "TerrainType/Spherical3D", false, terrain3DIsoSurfaces},
				{"3D cubic", preset3DCubicTexture, "TerrainType/Cubic3D", false, terrain3DIsoSurfaces},
			};
			
			PresetCellList	terrain2DPresets = new PresetCellList
			{
				{"2D Terrain type"},
				{"2D flat", preset2DPlanarTexture, "TerrainType/Planar2D", true, terrain2DIsoSurfaces},
				{"2D spherical", null, "TerrainType/Spherical2D", false, terrain2DIsoSurfaces},
				{"2D cubic", preset2DPlanarTexture, "TerrainType/Cubic2D", false, terrain2DIsoSurfaces},
				{"Side view like terraria", preset2DSideViewTexture, "Base/SideView", false},
			};
			
			PresetCellList	outputTypePresets = new PresetCellList
			{
				{"Terrain type"},
				{"2D Terrain like civilization", preset2DPlanarTexture, "Base/2D", true, terrain2DPresets},
				{"3D Terrain like minecraft", preset3DPlanarTexture, "Base/3D", false, terrain3DPresets},
			};

			return outputTypePresets;
		}
	
		List< BiomeGraph > CopyBiomesFromPreset(string biomeFolder)
		{
			List< BiomeGraph > biomes = new List< BiomeGraph >();

			string graphPath = worldGraph.assetFilePath;
			string biomeTargetPath = Path.GetDirectoryName(graphPath) + "/" + GraphFactory.baseGraphBiomeFolderName + "/";
			
			var biomeGraphs = Resources.LoadAll< BiomeGraph >(biomeAssetPrefix + biomeFolder);
			for (int i = 0; i < biomeGraphs.Length; i++)
			{
				string name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(biomeGraphs[i]));
				var bg = biomeGraphs[i].Clone() as BiomeGraph;
				string path = biomeTargetPath + name + ".asset";

				AssetDatabase.CreateAsset(bg, path);
				foreach (var node in bg.allNodes)
					AssetDatabase.AddObjectToAsset(node, bg);
				
				//Set our graph into biome graph input
				(bg.inputNode as NodeBiomeGraphInput).previewGraph = worldGraph;

				biomes.Add(bg);
			}
			
			return biomes;
		}
	
		public override void OnBuildPressed()
		{
			GraphBuilder builder = GraphBuilder.FromGraph(worldGraph);
			List< BiomeGraph > biomes = null;

			foreach (var graphPartFile in graphPartFiles)
			{
				var file = Resources.Load< TextAsset >(graphFilePrefix + graphPartFile);
				builder.ImportCommands(file.text.Split('\n'));

				if (graphPartFile.StartsWith("Biomes/"))
					biomes = CopyBiomesFromPreset(Path.GetFileName(graphPartFile));
			}
			
			builder.Execute();
			
			if (biomes != null)
			{
				var biomeNodes = worldGraph.FindNodesByType< NodeBiome >();
				for (int i = 0; i < biomeNodes.Count; i++)
					biomeNodes[i].biomeGraph = biomes[i];
			}

			builder.GetGraph().Process();
			
			worldGraph.presetChoosed = true;
		}
	
	}
}