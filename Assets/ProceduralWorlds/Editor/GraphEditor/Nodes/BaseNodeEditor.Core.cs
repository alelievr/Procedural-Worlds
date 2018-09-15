﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using ProceduralWorlds.Core;
using ProceduralWorlds.Nodes;
using System;
using System.Reflection.Emit;

namespace ProceduralWorlds.Editor
{
	//Node core rendering
	public abstract partial class BaseNodeEditor
	{
		static GUIStyle 			renameNodeTextFieldStyle;
		static GUIStyle				innerNodePaddingStyle;
		static GUIStyle				nodeStyle;
		static bool					styleLoadedStatic;
		[System.NonSerialized]
		bool						styleLoaded;

		readonly Dictionary< string, ReflectionUtils.GenericField > bakedChildFieldGetters = new Dictionary< string, ReflectionUtils.GenericField >();

		void LoadCoreResources()
		{
			if (!styleLoaded)
				styleLoaded = true;
			
			if (!styleLoadedStatic)
			{
				//check if style was already initialized:
				if (innerNodePaddingStyle != null)
					return ;
	
				renameNodeTextFieldStyle = GUI.skin.FindStyle("RenameNodetextField");
				innerNodePaddingStyle = GUI.skin.FindStyle("WindowInnerPadding");
				nodeStyle = GUI.skin.FindStyle("Node");
	
				styleLoadedStatic = true;
			}
		}

		IEnumerable< BaseNode > selectedNodeToDrag;

		void RenderNode()
		{
			var e = Event.current;

			Profiler.BeginSample("[PW] " + nodeRef.GetType() + " rendering");
			
			//update the PWGUI window rect with this window rect:
			PWGUI.StartFrame(nodeRef.rect);

			// set the header of the window as draggable:
			int width = (int)nodeRef.rect.width;
			int padding = 8;
			Rect dragRect = new Rect(padding, 0, width - padding * 2, 20);
			
			if (e.type == EventType.MouseDown && e.button == 0 && dragRect.Contains(e.mousePosition))
			{
				selectedNodeToDrag = graphRef.allNodes.Where(n => n.isSelected);
				nodeRef.isDragged = true;
				editorEvents.isDraggingNode = true;
			}
			if (e.rawType == EventType.MouseUp)
			{
				nodeRef.isDragged = false;
				editorEvents.isDraggingNode = false;
			}

			if (editorEvents.isDraggingNode && e.type == EventType.MouseDrag)
			{
				foreach (var node in selectedNodeToDrag)
					node.rect.position += e.delta;
			}

			if (nodeRef.isDragged)
				Undo.RecordObject(nodeRef, "Node " + nodeRef.name + " dragged");
				
			RenderHeader();
			
			//Drag window
			if (e.button == 0 && !windowNameEdit)
				GUI.DragWindow(dragRect);
			

			GUILayout.BeginVertical(innerNodePaddingStyle);
			{
				DrawNullInputGUI();

				if (nodeRef.foldout)
				{
					//Undo/Redo handling:
					undoRedoHelper.Beign();
					
					OnNodeGUI();
			
					undoRedoHelper.End();
				}
				else
					GUILayout.Space(EditorGUIUtility.singleLineHeight);

				EditorGUIUtility.labelWidth = 0;
			}
			GUILayout.EndVertical();

			int viewH = (int)GUILayoutUtility.GetLastRect().height;
			if (e.type == EventType.Repaint)
				nodeRef.viewHeight = viewH;

			nodeRef.viewHeight = Mathf.Max(nodeRef.viewHeight, maxAnchorRenderHeight);
				
			if (e.type == EventType.Repaint)
				nodeRef.viewHeight += 24;
			
			RenderAnchors();
			ProcessAnchorEvents();

			Profiler.EndSample();

			delayedChanges.Update();

			Rect selectRect = new Rect(10, 18, rect.width - 20, rect.height - 18);
			if (e.type == EventType.MouseDown && e.button == 0 && selectRect.Contains(e.mousePosition))
			{
				nodeRef.isSelected = !nodeRef.isSelected;
				if (nodeRef.isSelected)
				{
					graphEditor.RaiseNodeSelected(nodeRef);
					if (nodeInspectorGUIOverloaded)
						Selection.activeObject = nodeRef;
				}
				else
					graphEditor.RaiseNodeUnselected(nodeRef);
			}
		}


		void RenderInspector()
		{
			OnNodeInspectorGUI();
		}

		void DrawNullInputGUI()
		{
			foreach (var inputAnchor in nodeRef.inputAnchors)
			{
				if (!inputAnchor.required)
					continue ;
				
				if (!bakedChildFieldGetters.ContainsKey(inputAnchor.fieldName))
					bakedChildFieldGetters[inputAnchor.fieldName] = ReflectionUtils.CreateGenericField(nodeRef.GetType(), inputAnchor.fieldName);

				if (bakedChildFieldGetters[inputAnchor.fieldName].GetValue(nodeRef) == null)
					EditorGUILayout.HelpBox("Null parameter: " + inputAnchor.fieldName, MessageType.Error);
			}
		}
	}

}