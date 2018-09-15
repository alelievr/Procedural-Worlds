﻿using UnityEngine;
using UnityEditor;
using ProceduralWorlds.Core;

namespace ProceduralWorlds.Editor
{
	public class ResizablePanelSeparator : LayoutSeparator
	{
		readonly bool		vertical;

		[SerializeField]
		Rect				lastRect;

		Rect				separatorRect;

		public bool			draggingHandler { get; private set; }

		public ResizablePanelSeparator(LayoutOrientation orientation)
		{
			this.vertical = orientation == LayoutOrientation.Vertical;
		}
	
		public override Rect Begin()
		{
			int internHandlerPosition = (int)layoutSetting.separatorPosition;
			if (vertical)
			{
				//TODO
			}
			else
			{
				if (layoutSetting.leftBar)
					DrawHandleBar();
				
				Rect r = EditorGUILayout.BeginHorizontal(GUILayout.Width(internHandlerPosition), GUILayout.ExpandHeight(true));
				if (e.type == EventType.Repaint)
					lastRect = r;
			}

			lastRect.width = internHandlerPosition;

			return lastRect;
		}

		public override void End()
		{
			if (vertical)
			{
			}
			else
			{
				EditorGUILayout.EndHorizontal();
	
				if (!layoutSetting.leftBar)
					DrawHandleBar();
			}
		}

		public override Rect GetSeparatorRect()
		{
			return separatorRect;
		}

		void DrawHandleBar()
		{
			Rect sepRect = EditorGUILayout.BeginHorizontal(GUILayout.Width(layoutSetting.separatorWidth), GUILayout.ExpandHeight(true));
			GUILayout.Space(layoutSetting.separatorWidth);
			EditorGUI.DrawRect(sepRect, Color.white);
			EditorGUILayout.EndHorizontal();

			if (e.type == EventType.Repaint)
				this.separatorRect = sepRect;

			EditorGUIUtility.AddCursorRect(sepRect, MouseCursor.ResizeHorizontal);

			if (e.type == EventType.MouseDown && e.button == 0)
				if (sepRect.Contains(e.mousePosition))
					draggingHandler = true;

				
			if (e.type == EventType.MouseDrag && e.button == 0 && draggingHandler && e.delta != Vector2.zero)
				layoutSetting.separatorPosition += (layoutSetting.leftBar) ? -e.delta.x : e.delta.x;
			
			float p = layoutSetting.separatorPosition - lastRect.x;
			layoutSetting.separatorPosition = Mathf.Clamp(p, layoutSetting.minWidth - lastRect.x, layoutSetting.maxWidth - lastRect.x) + lastRect.x;
			
			if (e.rawType == EventType.MouseUp)
				draggingHandler = false;
		}

		public override LayoutSetting UpdateLayoutSetting(LayoutSetting ls)
		{
			LayoutSetting ret;

			ret = base.UpdateLayoutSetting(ls);

			if (ret == null && ls != null)
				ls.vertical = vertical;
			
			return ret;
		}

		public override void Resize(Rect newWindow)
		{
			base.Resize(newWindow);
		}
	}
}