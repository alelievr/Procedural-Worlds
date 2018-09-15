using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEditor;
using System.Linq;
using ProceduralWorlds.Core;
using ProceduralWorlds.Nodes;
using System;

namespace ProceduralWorlds.Editor
{
	//Anchor rendering and event processing
	public abstract partial class BaseNodeEditor
	{
		Texture2D		errorIcon;
		Texture2D		anchorTexture;
		
		GUIStyle		inputAnchorLabelStyle;
		GUIStyle		outputAnchorLabelStyle;
		GUIStyle		boxAnchorStyle;

		BaseGraphEditorEventInfo	oldEventInfos;

		void LoadAnchorResources()
		{
			boxAnchorStyle = new GUIStyle(GUI.skin.box);
			boxAnchorStyle.padding = new RectOffset(0, 0, 1, 1);
			anchorTexture = GUI.skin.box.normal.background;
			inputAnchorLabelStyle = GUI.skin.FindStyle("InputAnchorLabel");
			outputAnchorLabelStyle = GUI.skin.FindStyle("OutputAnchorLabel");
			
			//assets:
			errorIcon = Resources.Load< Texture2D >("Icons/ic_error");
		}

		public void RenderAnchors()
		{
			int			windowHeaderSize = nodeStyle.border.top + nodeStyle.margin.top;
			int			windowHorizontalPadding = 15;
			Rect		inputAcnhorRect = new Rect(windowHorizontalPadding - 13, windowHeaderSize, 120, -1);
			Rect		outputAnchorRect = new Rect(nodeRef.rect.width - windowHorizontalPadding, windowHeaderSize, -120, -1);

			foreach (var anchorField in nodeRef.inputAnchorFields)
				RenderAnchorField(anchorField, ref inputAcnhorRect);
			foreach (var anchorField in nodeRef.outputAnchorFields)
				RenderAnchorField(anchorField, ref outputAnchorRect);
			
			nodeRef.viewHeight = (int)Mathf.Max(nodeRef.viewHeight, inputAcnhorRect.yMax);
			nodeRef.viewHeight = (int)Mathf.Max(nodeRef.viewHeight, outputAnchorRect.yMax);
		}

		public void ProcessAnchorEvents()
		{
			BaseGraphEditorEventInfo	editorEvents = graphRef.editorEvents;
			
			oldEventInfos = editorEvents.Clone(oldEventInfos);

			//process events on every anchors:
			foreach (var anchorField in nodeRef.inputAnchorFields)
				ProcessEvent(anchorField);
			foreach (var anchorField in nodeRef.outputAnchorFields)
				ProcessEvent(anchorField);
			
			//link anchor event is we release the mouse with a draggingLink.
			if (editorEvents.isMouseOverAnchor)
				if (e.rawType == EventType.MouseUp && editorEvents.isDraggingLink)
					graphRef.SafeCreateLink(editorEvents.mouseOverAnchor);

			if (editorEvents.isDraggingLink)
			{
				if (oldEventInfos.mouseOverAnchor != editorEvents.mouseOverAnchor)
				{
					if (editorEvents.mouseOverAnchor == null && OnDraggedLinkQuitAnchor != null)
						OnDraggedLinkQuitAnchor(oldEventInfos.mouseOverAnchor);
					else if (OnDraggedLinkOverAnchor != null)
						OnDraggedLinkOverAnchor(editorEvents.mouseOverAnchor);
				}
			}
		}
		
		readonly Dictionary< AnchorHighlight, Color > highlightModeToColor = new Dictionary< AnchorHighlight, Color > {
			{ AnchorHighlight.AttachAdd, Color.green },
			{ AnchorHighlight.AttachNew, Color.blue },
			{ AnchorHighlight.AttachReplace, Color.yellow },
		};

		//the anchor passed to ths function must be in the `anchors` list
		void RenderAnchor(AnchorField anchorField, Anchor anchor, ref Rect renderRect)
		{
			//visual parameters for anchors:
			Vector2		anchorSize = new Vector2(13, 13);
			Vector2		margin = new Vector2(0, 2);

			if (anchor.forcedY != -1)
				renderRect.yMin = anchor.forcedY;
			
			anchor.rect = new Rect(renderRect.min + margin, anchorSize);

			//anchor name:
			string anchorName = anchorField.name;

			if (!String.IsNullOrEmpty(anchor.name))
				anchorName = anchor.name;

			//anchor color:
			if (anchor.color != new Color(0, 0, 0, 0))
				GUI.color = anchor.color;
			else
				GUI.color = ColorTheme.GetAnchorColor(anchor.colorSchemeName);

			//highlight mode to GUI color:
			if (graphRef.editorEvents.isDraggingLink || graphRef.editorEvents.isDraggingNewLink)
			{
				if (anchor.highlighMode != AnchorHighlight.None)
				{
					if (anchor.isLinkable)
						GUI.color = highlightModeToColor[anchor.highlighMode];
					else
						GUI.color = ColorTheme.disabledAnchorColor;
				}
			}

			//Draw the anchor:
			GUI.DrawTexture(anchor.rect, anchorTexture, ScaleMode.ScaleToFit);
			
			//Draw the anchor name if not null
			if (!string.IsNullOrEmpty(anchorName))
			{
				Rect	anchorNameRect = anchor.rect;
				Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(anchorName));
				if (anchorField.anchorType == AnchorType.Input)
					anchorNameRect.position += new Vector2(-6, -2);
				else
					anchorNameRect.position += new Vector2(-textSize.x + 4, -2);
				anchorNameRect.size = textSize + new Vector2(15, 4); //add the anchorLabel size
				GUI.depth = 10;
				GUI.Label(anchorNameRect, anchorName, (anchorField.anchorType == AnchorType.Input) ? inputAnchorLabelStyle : outputAnchorLabelStyle);
			}

			//error display (required unlinked anchors)
			if (anchor.visibility == Visibility.Visible
					&& anchorField.required
					&& !anchorField.multiple
					&& anchor.anchorType == AnchorType.Input
					&& anchor.linkCount == 0)
			{
				Rect errorIconRect = new Rect(anchor.rect);
				errorIconRect.size = Vector2.one * 17;
				errorIconRect.position += new Vector2(-6, -10);
				GUI.color = Color.red;
				GUI.DrawTexture(errorIconRect, errorIcon);
				GUI.color = Color.white;
			}
			
			//debug:
			if (anchor.debug)
			{
				GUIContent debugContent = new GUIContent("c:" + anchor.linkCount + "|i:" + anchor.fieldIndex);

				Rect debugRect = anchor.rect;
					debugRect.xMax += 50;
				if (anchor.anchorType == AnchorType.Output)
					debugRect.x -= EditorStyles.label.CalcSize(debugContent).x;
				EditorGUI.DrawRect(debugRect, Color.white * .8f);
				GUI.Label(debugRect, debugContent);
			}
		}

		public void RenderAnchorField(AnchorField anchorField, ref Rect renderRect)
		{
			int index = 0;
			int	anchorDefaultPadding = 18;

			renderRect.y += anchorField.offset;

			foreach (var anchor in anchorField.anchors)
			{
				//render anchor if visible and linkable
				if (anchor.visibility == Visibility.Visible && anchor.isLinkable)
				{
					RenderAnchor(anchorField, anchor, ref renderRect);
					index++;
				}

				//if anchor is not gone, increment the padding for next anchor
				if (anchor.visibility != Visibility.Gone)
					renderRect.y += anchorField.padding + anchorDefaultPadding;
			}
		}

		public void ProcessEvent(AnchorField anchorField)
		{
			var e = Event.current;
			bool contains = false;

			if (e.type == EventType.ContextClick)
				Debug.Log(e.mousePosition);
			
			foreach (var anchor in anchorField.anchors)
				if (anchor.visibility == Visibility.Visible && anchor.rect.Contains(e.mousePosition))
				{
					editorEvents.mouseOverAnchor = anchor;
					editorEvents.isMouseOverAnchorFrame = true;
					if (e.type == EventType.MouseDown && e.button == 0)
						editorEvents.isMouseClickOnAnchor = true;
					contains = true;
				}
			
			//clean anchor field if the old anchor was in this anchorField
			if (!contains)
			{
				if (anchorField.anchors.Contains(editorEvents.mouseOverAnchor))
					editorEvents.mouseOverAnchor = null;
			}
		}
	}
}