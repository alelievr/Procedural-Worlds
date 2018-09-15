﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralWorlds.Nodes;
using ProceduralWorlds.Core;
using ProceduralWorlds.Biomator;
using UnityEditor;
using System;

namespace ProceduralWorlds.Editor
{
	[CustomEditor(typeof(NodeDebugInfo))]
	public class NodeDebugInfoEditor : BaseNodeEditor
	{
		public NodeDebugInfo		node;

		[System.NonSerialized]
		bool						firstRender = false;

		readonly BiomeDataDrawer	biomeDataDrawer = new BiomeDataDrawer();

		public override void OnNodeEnable()
		{
			node = target as NodeDebugInfo;
		}

		public override void OnNodeGUI()
		{
			if (node.obj != null)
			{
				if (!firstRender && e.type != EventType.Layout)
					return ;
				
				firstRender = true;

				Type	objType = node.obj.GetType();
				EditorGUILayout.LabelField(node.obj.ToString());
				if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(PWArray<>))
				{
					var pwv = node.obj as PWArray< object >;

					for (int i = 0; i < pwv.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("[" + i + "] " + pwv.NameAt(i) + ": " + pwv.At(i), GUILayout.Width(300));
						EditorGUILayout.EndHorizontal();
					}
				}
				else if (objType == typeof(Vector2))
					EditorGUILayout.Vector2Field("vec2", (Vector2)node.obj);
				else if (objType == typeof(Vector3))
					EditorGUILayout.Vector2Field("vec3", (Vector3)node.obj);
				else if (objType == typeof(Vector4))
					EditorGUILayout.Vector2Field("vec4", (Vector4)node.obj);
				else if (objType == typeof(Sampler2D))
					PWGUI.Sampler2DPreview(node.obj as Sampler2D);
				else if (objType == typeof(Sampler3D))
				{
					//TODO: 3D sampler preview
				}
				else if (objType == typeof(BiomeData))
				{
					if (!biomeDataDrawer.isEnabled)
						biomeDataDrawer.OnEnable(node.obj as BiomeData);
					biomeDataDrawer.OnGUI(rect);
				}
			}
			else
				EditorGUILayout.LabelField("null");
		}
	}
}