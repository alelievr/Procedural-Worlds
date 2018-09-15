﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralWorlds.Nodes;
using ProceduralWorlds.Core;
using UnityEditor;

namespace ProceduralWorlds.Editor
{
	[CustomEditor(typeof(NodeGraphOutput))]
	public class NodeGraphOutputEditor : BaseNodeEditor
	{
		public NodeGraphOutput node;

		public override void OnNodeEnable()
		{
			node = target as NodeGraphOutput;
		}

		public override void OnNodeGUI()
		{
			PWGUI.PWArrayField(node.inputValues);
		}
	}
}