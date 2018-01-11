﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PW.Core;
using PW.Biomator;

namespace PW.Node
{
	public class PWNodeBiome : PWNode
	{

		[PWOutput]
		public Biome		outputBiome;

		[PWInput]
		public BiomeData	inputBiomeData;

		[SerializeField]
		PWBiomeGraph		biomeGraph;
	
		string propUpdateKey = "PWNodeBiome";

		public override void OnNodeCreation()
		{
			name = "your node name";
		}

		public override void OnNodeEnable()
		{
		}

		public override void OnNodeGUI()
		{
			EditorGUILayout.LabelField("Biome Graph reference");
			biomeGraph = EditorGUILayout.ObjectField(biomeGraph, typeof(PWBiomeGraph), false) as PWBiomeGraph;

			if (biomeGraph != null)
			{
				if (GUILayout.Button("Open " + biomeGraph.name))
					AssetDatabase.OpenAsset(biomeGraph);
			}
		}

		public override void OnNodeProcessOnce()
		{
			if (biomeGraph == null)
			{
				Debug.LogError("NUll biome graph when processing once a biome node");
				return ;
			}
			
			biomeGraph.ProcessOnce();
		}

		public override void OnNodeProcess()
		{
			if (biomeGraph == null)
			{
				Debug.LogError("NUll biome graph when processing a biome node");
				return ;
			}

			biomeGraph.Process();
		}
		
	}
}