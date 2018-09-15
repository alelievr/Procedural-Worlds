﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralWorlds.Nodes;
using UnityEditor;
using System;
using ProceduralWorlds.Core;

namespace ProceduralWorlds.Editor
{
	[CustomEditor(typeof(NodeTerrainDetail))]
	public class NodeTerrainDetailEditor : BaseNodeEditor
	{
		public NodeTerrainDetail node;

		public override void OnNodeEnable()
		{
			node = target as NodeTerrainDetail;
		}

		public override void OnNodeGUI()
		{
			EditorGUIUtility.labelWidth = 100;
			node.outputDetail.biomeDetailMask = EditorGUILayout.MaskField("details", node.outputDetail.biomeDetailMask, Enum.GetNames(typeof(TerrainDetailType)));
		}
	}
}