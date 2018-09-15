﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralWorlds.Nodes;

namespace ProceduralWorlds.Editor
{
	[CustomEditor(typeof(NodeBiomeSwitch))]
	public class NodeBiomeSwitchEditor : BaseNodeEditor
	{
		public NodeBiomeSwitch			node;

		readonly BiomeSwitchListDrawer	switchListDrawer = new BiomeSwitchListDrawer();

		const string					delayedUpdateKey = "BiomeSwitchListUpdate";

		public override void OnNodeEnable()
		{
			node = target as NodeBiomeSwitch;

			delayedChanges.BindCallback(delayedUpdateKey, (unused) => { NotifyReload(); });

			node.switchList.OnBiomeDataAdded = (unused) => {
				UpdateAnchorCount();
				UpdateSwitchList();
				delayedChanges.UpdateValue(delayedUpdateKey);
			};
			node.switchList.OnBiomeDataModified = (unused) => {
				node.alreadyModified = true;
				UpdateSwitchList();
				delayedChanges.UpdateValue(delayedUpdateKey);
			};
			node.switchList.OnBiomeDataRemoved = () => {
				UpdateAnchorCount();
				UpdateSwitchList();
				delayedChanges.UpdateValue(delayedUpdateKey);
			};
			node.switchList.OnBiomeDataReordered = () => {
				UpdateSwitchList();
				delayedChanges.UpdateValue(delayedUpdateKey);
			};

			switchListDrawer.OnEnable(node.switchList);
			
			node.CheckForBiomeSwitchErrors();
			UpdateSwitchList();
		}

		void UpdateAnchorCount()
		{
			node.SetMultiAnchor("outputBiomes", node.switchList.Count, null);
		}

		void UpdateSwitchList()
		{
			node.UpdateSwitchList();
			switchListDrawer.UpdateBiomeRepartitionPreview(node.inputBiome);
		}

		public override void OnNodeGUI()
		{
			//return if input biome is null
			if (node.inputBiome == null)
			{
				EditorGUILayout.LabelField("null biome input !");
				return ;
			}

			//display popup field to choose the switch source
			EditorGUI.BeginChangeCheck();
			{
				EditorGUIUtility.labelWidth = 80;
				node.selectedBiomeSamplerName = EditorGUILayout.Popup("switch parameter", node.selectedBiomeSamplerName, node.samplerNames);
				node.samplerName = node.samplerNames[node.selectedBiomeSamplerName];
			}
			if (EditorGUI.EndChangeCheck())
			{
				node.CheckForBiomeSwitchErrors();
				node.UpdateSwitchMode();
				delayedChanges.UpdateValue(delayedUpdateKey);
			}

			EditorGUILayout.LabelField((node.currentSampler != null) ? "min: " + node.relativeMin + ", max: " + node.relativeMax : "");

			if (node.error)
			{
				Rect errorRect = EditorGUILayout.GetControlRect(false, GUI.skin.label.lineHeight * 3.5f);
				EditorGUI.LabelField(errorRect, node.errorString);
				return ;
			}

			switchListDrawer.OnGUI(node.inputBiome);
		}

		public override void OnNodePreProcess()
		{
			UpdateSwitchList();
		}

	}
}