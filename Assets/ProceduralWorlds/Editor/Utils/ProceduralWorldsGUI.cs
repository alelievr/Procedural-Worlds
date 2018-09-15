﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ProceduralWorlds.Biomator;
using ProceduralWorlds.Core;
using UnityEditor.AnimatedValues;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using ProceduralWorlds.Nodes;

namespace ProceduralWorlds.Editor
{
	public enum PWGUIStyleType
	{
		PrefixLabelWidth,
		FieldWidth,
	}

	public class PWGUIStyle
	{
		
		public int				data;
		public PWGUIStyleType	type;

		public PWGUIStyle(int data, PWGUIStyleType type)
		{
			this.data = data;
			this.type = type;
		}

		public PWGUIStyle SliderLabelWidth(int pixels)
		{
			return new PWGUIStyle(pixels, PWGUIStyleType.PrefixLabelWidth);
		}
	}
	
	[System.Serializable]
	public class ProceduralWorldsGUI
	{
		public static bool	displaySamplerStepBounds = false;

		Rect				currentWindowRect;

		static Texture2D	icColor;
		static Texture2D	icEdit;
		static Texture2D	icSettingsOutline;
		readonly Dictionary< PWGUIFieldType, int > currentSettingIndices = new Dictionary< PWGUIFieldType, int >();

		List< PWGUISettings > settingsStorage = new List< PWGUISettings >();

		BaseNode			attachedNode;
		
		[System.NonSerializedAttribute]
		static MethodInfo	gradientField;

		public void SetNode(BaseNode node)
		{
			if (node != null)
			{
				attachedNode = node;
				node.OnPostProcess += ReloadTextures;
				settingsStorage = node.PWGUIStorage.settingsStorage;
				EditorApplication.playModeStateChanged += PlayModeChangedCallback;
			}
		}

		~ProceduralWorldsGUI()
		{
			if (attachedNode != null)
			{
				attachedNode.OnPostProcess -= ReloadTextures;
				EditorApplication.playModeStateChanged -= PlayModeChangedCallback;
			}
		}

		void PlayModeChangedCallback(PlayModeStateChange mode)
		{
			if (mode == PlayModeStateChange.EnteredEditMode)
				ReloadTextures();
		}

		public void ReloadTextures()
		{
			settingsStorage = attachedNode.PWGUIStorage.settingsStorage;
			if (settingsStorage == null)
				return ;
			
			foreach (var setting in settingsStorage)
			{
				switch (setting.fieldType)
				{
					case PWGUIFieldType.Sampler2DPreview:
						UpdateSampler2D(setting);
						break ;
					case PWGUIFieldType.BiomeMapPreview:
						UpdateBiomeMap2D(setting);
						break ;
					default:
						break ;
				}
			}
		}
		
	#region Color field

		public void ColorPicker(string prefix, ref Color c, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			Rect colorFieldRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
			ColorPicker(prefix, colorFieldRect, ref c, displayColorPreview, previewOnIcon);
		}
		
		public void ColorPicker(ref Color c, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			Rect colorFieldRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
			ColorPicker("", colorFieldRect, ref c, displayColorPreview, previewOnIcon);
		}

		public void ColorPicker(Rect rect, ref Color c, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			ColorPicker("", rect, ref c, displayColorPreview, previewOnIcon);
		}

		public void ColorPicker(Rect rect, ref SerializableColor c, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			Color color = c;
			ColorPicker("", rect, ref color, displayColorPreview, previewOnIcon);
			c = (SerializableColor)color;
		}
		
		public void ColorPicker(string prefix, Rect rect, ref SerializableColor c, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			ColorPicker(new GUIContent(prefix), rect, ref c, displayColorPreview, previewOnIcon);
		}

		public void ColorPicker(GUIContent prefix, Rect rect, ref SerializableColor c, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			Color color = c;
			ColorPicker(prefix, rect, ref color, displayColorPreview, previewOnIcon);
			c = (SerializableColor)color;
		}
		
		public void ColorPicker(string prefix, Rect rect, ref Color color, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			ColorPicker(new GUIContent(prefix), rect, ref color, displayColorPreview, previewOnIcon);
		}
	
		public void ColorPicker(GUIContent prefix, Rect rect, ref Color color, bool displayColorPreview = true, bool previewOnIcon = false)
		{
			var		e = Event.current;
			Rect	iconRect = rect;
			int		icColorSize = 18;
			Color	localColor = color;

			var fieldSettings = GetGUISettingData(PWGUIFieldType.Color, () => {
				PWGUISettings colorSettings = new PWGUISettings();

				colorSettings.c = (SerializableColor)localColor;

				return colorSettings;
			});

			if (e.type == EventType.ExecuteCommand && e.commandName == "ColorPickerUpdate")
			{
				if (fieldSettings.GetHashCode() == ColorPickerPopup.controlId)
				{
					ColorPickerPopup.UpdateDatas(fieldSettings);
					GUI.changed = true;
				}
			}
			
			color = fieldSettings.c;
			
			//draw the icon
			Rect colorPreviewRect = iconRect;
			if (displayColorPreview)
			{
				int width = (int)rect.width;
				int colorPreviewPadding = 5;
				
				Vector2 prefixSize = Vector2.zero;
				if (prefix != null && !String.IsNullOrEmpty(prefix.text))
				{
					prefixSize = GUI.skin.label.CalcSize(prefix);
					prefixSize.x += 5; //padding of 5 pixels
					colorPreviewRect.position += new Vector2(prefixSize.x, 0);
					Rect prefixRect = new Rect(iconRect.position, prefixSize);
					GUI.Label(prefixRect, prefix);
				}
				colorPreviewRect.size = new Vector2(width - icColorSize - prefixSize.x - colorPreviewPadding, 16);
				iconRect.position += new Vector2(colorPreviewRect.width + prefixSize.x + colorPreviewPadding, 0);
				iconRect.size = new Vector2(icColorSize, icColorSize);
				EditorGUIUtility.DrawColorSwatch(colorPreviewRect, color);
			}
			
			//actions if clicked on/outside of the icon
			if (previewOnIcon)
				GUI.color = color;
			GUI.DrawTexture(iconRect, icColor);
			GUI.color = Color.white;
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (iconRect.Contains(e.mousePosition) || colorPreviewRect.Contains(e.mousePosition))
				{
					ColorPickerPopup.OpenPopup(color, fieldSettings);
					e.Use();
				}
			}
		}
	
	#endregion

	#region Gradient field

	public Gradient GradientField(Gradient gradient)
	{
		return GradientField((GUIContent)null, gradient);
	}

	public Gradient GradientField(string label, Gradient gradient)
	{
		return GradientField(new GUIContent(label), gradient);
	}

	public Gradient GradientField(GUIContent content, Gradient gradient)
	{
		if (content != null && content.text != null)
			EditorGUILayout.PrefixLabel(content);

		if (gradientField == null)
		{
			gradientField = typeof(EditorGUILayout).GetMethod(
				"GradientField",
				BindingFlags.NonPublic | BindingFlags.Static,
				null,
				new[] { typeof(string), typeof(Gradient), typeof(GUILayoutOption[]) },
				null
			);
		}

		gradient = (Gradient)gradientField.Invoke(null, new object[] {"", gradient, null});

		return gradient;
	}

	#endregion

	#region Text field
		
		public void TextField(string prefix, ref string text, bool editable = false, GUIStyle textStyle = null)
		{
			TextField(prefix, EditorGUILayout.GetControlRect().position, ref text, editable, textStyle);
		}

		public void TextField(ref string text, bool editable = false, GUIStyle textStyle = null)
		{
			TextField(null, EditorGUILayout.GetControlRect().position, ref text, editable, textStyle);
		}

		public void TextField(Vector2 position, ref string text, bool editable = false, GUIStyle textStyle = null)
		{
			TextField(null, position, ref text, editable, textStyle);
		}

		public void TextField(string prefix, Vector2 textPosition, ref string text, bool editable = false, GUIStyle textFieldStyle = null)
		{
			Rect	textRect = new Rect(textPosition, Vector2.zero);
			var		e = Event.current;

			string	controlName = "textfield-" + text.GetHashCode().ToString();

			var fieldSettings = GetGUISettingData(PWGUIFieldType.Text, () => {
				return new PWGUISettings();
			});
			
			Vector2 nameSize = textFieldStyle.CalcSize(new GUIContent(text + " ")); //add a space for the edit icon beside the text
			textRect.size = nameSize;

			if (!String.IsNullOrEmpty(prefix))
			{
				Vector2 prefixSize = textFieldStyle.CalcSize(new GUIContent(prefix));
				Rect prefixRect = textRect;

				textRect.position += new Vector2(prefixSize.x, 0);
				prefixRect.size = prefixSize;
				GUI.Label(prefixRect, prefix);
			}
			
			Rect iconRect = new Rect(textRect.position + new Vector2(nameSize.x, 0), new Vector2(17, 17));
			bool editClickIn = (editable && e.type == EventType.MouseDown && e.button == 0 && iconRect.Contains(e.mousePosition));
			bool doubleClickText = (textRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.clickCount == 2);

			if (editClickIn)
				fieldSettings.editing = !fieldSettings.editing;
			if (doubleClickText)
				fieldSettings.editing = true;
			
			if (editable)
			{
				GUI.color = (fieldSettings.editing) ? ColorTheme.selectedColor : Color.white;
				GUI.DrawTexture(iconRect, icEdit);
				GUI.color = Color.white;
			}

			if (fieldSettings.editing)
			{
				Color oldCursorColor = GUI.skin.settings.cursorColor;
				GUI.skin.settings.cursorColor = Color.white;
				GUI.SetNextControlName(controlName);
				text = GUI.TextField(textRect, text, textFieldStyle);
				GUI.skin.settings.cursorColor = oldCursorColor;
				if (e.isKey && fieldSettings.editing)
				{
					if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
					{
						fieldSettings.editing = false;
						e.Use();
					}
				}
			}
			else
				GUI.Label(textRect, text, textFieldStyle);			
			
			bool editClickOut = (editable && e.rawType == EventType.MouseDown && e.button == 0 && !iconRect.Contains(e.mousePosition));

			if (editClickOut && fieldSettings.editing)
			{
				fieldSettings.editing = false;
				e.Use();
			}

			if ((editClickIn || doubleClickText) && fieldSettings.editing)
			{
				GUI.FocusControl(controlName);
				var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				te.SelectAll();
				e.Use();
			}
		}

	#endregion

	#region Slider and IntSlider field

		public float Slider(float value, ref float min, ref float max, float step = 0.01f, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			return Slider("", value, ref min, ref max, step, editableMin, editableMax, styles);
		}
		
		public float Slider(float value, float min, float max, float step = 0.01f, params PWGUIStyle[] styles)
		{
			return Slider("", value, ref min, ref max, step, false, false, styles);
		}
		
		public float Slider(string name, float value, float min, float max, float step = 0.01f, params PWGUIStyle[] styles)
		{
			return Slider(new GUIContent(name), value, min, max, step, styles);
		}
		
		public float Slider(GUIContent name, float value, float min, float max, float step = 0.01f, params PWGUIStyle[] styles)
		{
			return Slider(name, value, ref min, ref max, step, false, false, styles);
		}
		
		public float Slider(string name, float value, ref float min, ref float max, float step = 0.01f, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			return Slider(new GUIContent(name), value, ref min, ref max, step, editableMin, editableMax, styles);
		}
	
		public float Slider(GUIContent name, float value, ref float min, ref float max, float step = 0.01f, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			return Slider(name, value, ref min, ref max, step, editableMin, editableMax, false, styles);
		}
		
		float Slider(GUIContent name, float value, ref float min, ref float max, float step = 0.01f, bool editableMin = true, bool editableMax = true, bool intMode = false, params PWGUIStyle[] styles)
		{
			int		sliderLabelWidth = 30;
			var		e = Event.current;

			foreach (var style in styles)
				if (style.type == PWGUIStyleType.PrefixLabelWidth)
					sliderLabelWidth = style.data;

			if (name == null)
				name = new GUIContent();

			var fieldSettings = GetGUISettingData((intMode) ? PWGUIFieldType.IntSlider : PWGUIFieldType.Slider, () => {
				return new PWGUISettings();
			});
			
			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup(!editableMin);
						min = EditorGUILayout.FloatField(min, GUILayout.Width(sliderLabelWidth));
					EditorGUI.EndDisabledGroup();
					
					if (step != 0)
					{
						float m = 1 / step;
						value = Mathf.Round(GUILayout.HorizontalSlider(value, min, max) * m) / m;
					}
					else
						value = GUILayout.HorizontalSlider(value, min, max);
	
					EditorGUI.BeginDisabledGroup(!editableMax);
						max = EditorGUILayout.FloatField(max, GUILayout.Width(sliderLabelWidth));
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();
				
				GUILayout.Space(-4);
				EditorGUILayout.BeginHorizontal();
				{
					if (!fieldSettings.editing)
					{
						name.text += value.ToString();
						GUILayout.Label(name, Styles.centeredLabel);
						Rect valueRect = GUILayoutUtility.GetLastRect();
						if (valueRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
						{
							e.Use();
							if (e.clickCount == 2)
								fieldSettings.editing = true;
						}
					}
					else
					{
						GUI.SetNextControlName("slider-value-" + value.GetHashCode());
						GUILayout.FlexibleSpace();
						value = EditorGUILayout.FloatField(value, GUILayout.Width(50));
						Rect valueRect = GUILayoutUtility.GetLastRect();
						GUILayout.FlexibleSpace();
						if ((!valueRect.Contains(e.mousePosition) && e.type == EventType.MouseDown) || (e.isKey && e.keyCode == KeyCode.Return))
							{ fieldSettings.editing = false; e.Use(); }
						if (e.isKey && e.keyCode == KeyCode.Escape)
							{ fieldSettings.editing = false; e.Use(); }
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			return value;
		}
		
		public int IntSlider(int value, int min, int max, int step = 1, params PWGUIStyle[] styles)
		{
			return IntSlider((GUIContent)null, value, ref min, ref max, step, false, false, styles);
		}
		
		public int IntSlider(string name, int value, int min, int max, int step = 1, params PWGUIStyle[] styles)
		{
			return IntSlider(new GUIContent(name), value, min, max, step, styles);
		}

		public int IntSlider(GUIContent name, int value, int min, int max, int step = 1, params PWGUIStyle[] styles)
		{
			return IntSlider(name, value, ref min, ref max, step, false, false, styles);
		}
		
		public int IntSlider(string name, int value, ref int min, ref int max, int step = 1, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			return IntSlider(new GUIContent(name), value, ref min, ref max, step, editableMin, editableMax, styles);
		}
	
		public int IntSlider(GUIContent name, int value, ref int min, ref int max, int step = 1, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			float		v = value;
			float		m_min = min;
			float		m_max = max;
			value = (int)Slider(name, v, ref m_min, ref m_max, step, editableMin, editableMax, true, styles);
			min = (int)m_min;
			max = (int)m_max;
			return value;
		}
	
	#endregion

	#region TexturePreview field

		public void TexturePreview(Texture tex, bool settings = true)
		{
			TexturePreview(tex, settings, true, false);
		}

		public void TexturePreview(Rect previewRect, Texture tex, bool settings = true)
		{
			TexturePreview(previewRect, tex, settings, true, false);
		}
		
		Rect TexturePreview(Texture tex, bool settings, bool settingsStorage, bool debug)
		{
			Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(0));
			previewRect.size = (currentWindowRect.width - 20 - 10) * Vector2.one;
			GUILayout.Space(previewRect.width);
			TexturePreview(previewRect, tex, settings, settingsStorage, debug);
			return previewRect;
		}

		void TexturePreview(Rect previewRect, Texture tex, bool settings, bool settingsStorage, bool debug)
		{
			var e = Event.current;

			if (!settingsStorage)
			{
				EditorGUI.DrawPreviewTexture(previewRect, tex);
				if (debug)
					DisplayTextureDebug(previewRect, tex as Texture2D);
				return ;
			}

			//create or load texture settings
			var fieldSettings = GetGUISettingData(PWGUIFieldType.TexturePreview, () => {
				var state = new PWGUISettings();
				state.filterMode = FilterMode.Bilinear;
				state.scaleMode = ScaleMode.ScaleToFit;
				state.scaleAspect = 1;
				state.material = null;
				state.debug = false;
				return state;
			});

			if (e.type == EventType.Repaint)
				fieldSettings.savedRect = previewRect;

			EditorGUI.DrawPreviewTexture(previewRect, tex, fieldSettings.material, fieldSettings.scaleMode, fieldSettings.scaleAspect);

			if (!settings)
				return ;

			//render the texture settings window
			if (e.type == EventType.ExecuteCommand && e.commandName == "TextureSettingsUpdate")
			{
				TextureSettingsPopup.UpdateDatas(fieldSettings);
				tex.filterMode = fieldSettings.filterMode;
			}

			//render debug:
			if (fieldSettings.frameSafeDebug)
				DisplayTextureDebug(fieldSettings.savedRect, tex as Texture2D);

			int		icSettingsSize = 16;
			Rect	icSettingsRect = new Rect(previewRect.x + previewRect.width - icSettingsSize, previewRect.y, icSettingsSize, icSettingsSize);
			GUI.DrawTexture(icSettingsRect, icSettingsOutline);
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (icSettingsRect.Contains(e.mousePosition))
				{
					TextureSettingsPopup.OpenPopup(fieldSettings);
					e.Use();
				}
			}
		}

		void DisplayTextureDebug(Rect textureRect, Texture2D tex)
		{
			var e = Event.current;

			Vector2 pixelPos = e.mousePosition - textureRect.position;

			if (textureRect.width > 0)
				pixelPos *= tex.width / textureRect.width;

			if (pixelPos.x >= 0 && pixelPos.y >= 0 && pixelPos.x < tex.width && pixelPos.y < tex.height)
			{
				try {
					Color pixel = tex.GetPixel((int)pixelPos.x, (int)pixelPos.y);
					EditorGUILayout.LabelField("pixel(" + (int)pixelPos.x + ", " + (int)pixelPos.y + ")");
					EditorGUILayout.LabelField(pixel.ToString("F2"));
				} catch {
					EditorGUILayout.LabelField("Texture is not readble !");
				}
			}
		}

	#endregion

	#region Sampler2DPreview field
		
		public void Sampler2DPreview(Sampler2D samp, bool settings = true, FilterMode fm = FilterMode.Bilinear)
		{
			Sampler2DPreview((GUIContent)null, samp, settings, fm);
		}
		
		public void Sampler2DPreview(string prefix, Sampler2D samp, bool settings = true, FilterMode fm = FilterMode.Bilinear)
		{
			Sampler2DPreview(new GUIContent(prefix), samp, settings, fm);
		}
		
		public void Sampler2DPreview(GUIContent prefix, Sampler2D samp, bool settings = true, FilterMode fm = FilterMode.Bilinear)
		{
			int previewSize = (int)currentWindowRect.width - 20 - 20; //padding + texture margin
			previewSize = (int)Mathf.Clamp(previewSize, 0, currentWindowRect.width);
			var e = Event.current;

			if (samp == null)
				return ;

			if (prefix != null && !String.IsNullOrEmpty(prefix.text))
				EditorGUILayout.LabelField(prefix);

			var fieldSettings = GetGUISettingData(PWGUIFieldType.Sampler2DPreview, () => {
				var state = new PWGUISettings();
				state.filterMode = fm;
				state.debug = false;
				state.gradient = new SerializableGradient(
					Utils.CreateGradient(
						new KeyValuePair< float, Color >(0, Color.black),
						new KeyValuePair< float, Color >(1, Color.white)
					)
				);
				state.serializableGradient = (SerializableGradient)state.gradient;
				return state;
			});
		
			fieldSettings.sampler2D = samp;

			//avoid unity's ArgumentException for control position is the sampler value is set outside of the layout event:
			if (fieldSettings.firstRender && e.type != EventType.Layout)
				return ;
			fieldSettings.firstRender = false;

			//recreated texture if it has been destoryed:
			if (fieldSettings.texture == null)
			{
				fieldSettings.texture = new Texture2D(previewSize, previewSize, TextureFormat.RGBA32, false);
				fieldSettings.texture.wrapMode = TextureWrapMode.Clamp;
				fieldSettings.texture.filterMode = fieldSettings.filterMode;
				fieldSettings.samplerTextureUpdated = false;
			}

			//same for the gradient:
			if (fieldSettings.gradient == null)
				fieldSettings.gradient = fieldSettings.serializableGradient;
				
			Texture2D	tex = fieldSettings.texture;

			if (samp.size != tex.width)
				UpdateSampler2D(fieldSettings);
			
			//if the preview texture of the sampler have not been updated, we try to update it
			if (!fieldSettings.samplerTextureUpdated || fieldSettings.update)
				UpdateSampler2D(fieldSettings);
			
			Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(0));
			if (previewRect.width > 2)
				fieldSettings.savedRect = previewRect;

			TexturePreview(tex, false, false, false);
			
			if (settings)
			{
				//if the gradient value have been modified, we update the texture
				if (SamplerSettingsPopup.controlId == fieldSettings.GetHashCode() && (e.type == EventType.ExecuteCommand && e.commandName == "SamplerSettingsUpdate"))
				{
					SamplerSettingsPopup.UpdateDatas(fieldSettings);

					UpdateSampler2D(fieldSettings);
					
					if (e.type == EventType.ExecuteCommand)
						e.Use();
				}

				//draw the setting icon and manage his events
				int icSettingsSize = 16;
				int	icSettingsPadding = 4;
				Rect icSettingsRect = new Rect(previewRect.x + previewRect.width - icSettingsSize - icSettingsPadding, previewRect.y + icSettingsPadding, icSettingsSize, icSettingsSize);
	
				GUI.DrawTexture(icSettingsRect, icSettingsOutline);
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					if (icSettingsRect.Contains(e.mousePosition))
					{
						SamplerSettingsPopup.OpenPopup(fieldSettings);
						e.Use();
					}
				}
			}

			if (!settings && fieldSettings.texture.filterMode != fm)
				fieldSettings.texture.filterMode = fm;

			if (fieldSettings.frameSafeDebug)
			{
				Vector2 pixelPos = e.mousePosition - fieldSettings.savedRect.position;

				pixelPos *= samp.size / fieldSettings.savedRect.width;
				pixelPos.y = samp.size - pixelPos.y;

				EditorGUILayout.LabelField("Sampler2D min: " + samp.min + ", max: " + samp.max);

				if (pixelPos.x >= 0 && pixelPos.y >= 0 && pixelPos.x < samp.size && pixelPos.y < samp.size)
				{
					if (e.type == EventType.MouseMove)
						e.Use();
					EditorGUILayout.LabelField("(" + (int)pixelPos.x + ", " + (int)pixelPos.y + "): " + samp[(int)pixelPos.x, (int)pixelPos.y]);
				}
				else
					EditorGUILayout.LabelField("(NA, NA): NA");
			}
		}

		void UpdateSampler2D(PWGUISettings fieldSettings)
		{
			if (fieldSettings.sampler2D == null || fieldSettings.texture == null)
				return ;
			
			var tex = fieldSettings.texture;
			if (fieldSettings.sampler2D.size != tex.width)	
				tex.Resize(fieldSettings.sampler2D.size, fieldSettings.sampler2D.size, TextureFormat.RGBA32, false);

			int scale = (int)(fieldSettings.sampler2D.size / fieldSettings.sampler2D.step);

			if (scale == 0)
				scale = 1;
			
			fieldSettings.sampler2D.Foreach((x, y, val) => {
				if (displaySamplerStepBounds && (x % scale == 0 || y % scale == 0))
					tex.SetPixel(x, y, Color.black);
				else
					tex.SetPixel(x, y, fieldSettings.gradient.Evaluate(Mathf.Clamp01(val)));
			}, true);
			tex.Apply();
			fieldSettings.update = false;
			fieldSettings.samplerTextureUpdated = true;
		}

	#endregion

	#region Sampler field

	public void SamplerPreview(Sampler sampler, bool settings = true)
	{
		SamplerPreview(null, sampler, settings);
	}

	public void SamplerPreview(string name, Sampler sampler, bool settings = true)
	{
		if (sampler == null)
			return ;
		switch (sampler.type)
		{
			case SamplerType.Sampler2D:
				Sampler2DPreview(name, sampler as Sampler2D, settings);
				break ;
			default:
				break ;
		}
	}

	#endregion

	#region BiomeMapPreview field
	
		public void BiomeMap2DPreview(BiomeData map, bool settings = true, bool debug = true)
		{
			BiomeMap2DPreview(new GUIContent(), map, settings, debug);
		}

		public void BiomeMap2DPreview(GUIContent prefix, BiomeData biomeData, bool settings = true, bool debug = true)
		{
			Event e = Event.current;
			
			if (biomeData.biomeMap == null)
			{
				
				Debug.Log("biomeData does not contains biome map 2D");
				return ;
			}

			int texSize = biomeData.biomeMap.size;
			var fieldSettings = GetGUISettingData(PWGUIFieldType.BiomeMapPreview, () => {
				var state = new PWGUISettings();
				state.filterMode = FilterMode.Point;
				state.debug = debug;
				return state;
			});

			fieldSettings.biomeData = biomeData;

			if (fieldSettings.texture == null)
			{
				fieldSettings.texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
				fieldSettings.texture.wrapMode = TextureWrapMode.Clamp;
				fieldSettings.update = true;
				fieldSettings.texture.filterMode = FilterMode.Point;
			}

			if (fieldSettings.update)
				UpdateBiomeMap2D(fieldSettings);
				
			Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(0));

			TexturePreview(fieldSettings.texture, false, false, false);

			if (settings)
			{
				if (previewRect.width > 2)
					fieldSettings.savedRect = previewRect;

				//draw the setting icon and manage his events
				int icSettingsSize = 16;
				int	icSettingsPadding = 4;
				Rect icSettingsRect = new Rect(previewRect.x + previewRect.width - icSettingsSize - icSettingsPadding, previewRect.y + icSettingsPadding, icSettingsSize, icSettingsSize);
	
				GUI.DrawTexture(icSettingsRect, icSettingsOutline);
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					if (icSettingsRect.Contains(e.mousePosition))
					{
						BiomeMapSettingsPopup.OpenPopup(fieldSettings);
						e.Use();
					}
				}
			}
			
			//Copy the parameters of the opened popup when modified
			if (BiomeMapSettingsPopup.controlId == fieldSettings.GetHashCode() && (e.type == EventType.ExecuteCommand && e.commandName == "BiomeMapSettingsUpdate"))
			{
				BiomeMapSettingsPopup.UpdateDatas(fieldSettings);
			}

			if (fieldSettings.frameSafeDebug && fieldSettings.debug)
			{
				Vector2 pixelPos = e.mousePosition - fieldSettings.savedRect.position;
				Sampler terrain = biomeData.GetSampler(0);

				pixelPos *= terrain.size / fieldSettings.savedRect.width;
				pixelPos.y = terrain.size - pixelPos.y;

				if (pixelPos.x > 0 && pixelPos.y > 0 && pixelPos.x < terrain.size && pixelPos.y < terrain.size)
					DrawBiomeMapDebugBox(biomeData, pixelPos, terrain);
			}
		}

		void DrawBiomeMapDebugBox(BiomeData biomeData, Vector2 pixelPos, Sampler terrain)
		{
			int x = (int)Mathf.Clamp(pixelPos.x, 0, terrain.size - 1);
			int y = (int)Mathf.Clamp(pixelPos.y, 0, terrain.size - 1);
			BiomeBlendPoint point = biomeData.biomeMap.GetBiomeBlendInfo(x, y);

			EditorGUILayout.BeginVertical(Styles.debugBox);
			{
				for (int i = 0; i < point.length; i++)
				{
					short biomeId = point.biomeIds[i];
					float biomeBlend = point.biomeBlends[i];
					PartialBiome biome = biomeData.biomeSwitchGraph.GetBiome(biomeId);

					if (biome == null)
						continue ;

					EditorGUILayout.LabelField("Biome " + i + " (id: " + biomeId + "):" + biome.name);
					EditorGUI.indentLevel++;
					for (int j = 0; j < biomeData.length; j++)
					{
						float val = biomeData.GetSampler2D(j)[x, y];
						EditorGUILayout.LabelField(biomeData.GetBiomeKey(j) + ": " + val);
					}
					EditorGUILayout.LabelField("blend: " + (biomeBlend * 100).ToString("F1") + "%");
					EditorGUI.indentLevel--;
				}
			}
			EditorGUILayout.EndVertical();
		}

		void UpdateBiomeMap2D(PWGUISettings fieldSettings)
		{
			if (fieldSettings.biomeData == null || fieldSettings.texture == null)
				return ;
			
			var map = fieldSettings.biomeData.biomeMap;
			int texSize = map.size;
			
			if (texSize != fieldSettings.texture.width)
				fieldSettings.texture.Resize(texSize, texSize, TextureFormat.RGBA32, false);
			
			var switchGraph = fieldSettings.biomeData.biomeSwitchGraph;
			
			int scale = (int)(map.size / map.step);

			if (scale == 0)
				scale = 1;

			for (int x = 0; x < texSize; x++)
				for (int y = 0; y < texSize; y++)
				{
					if (displaySamplerStepBounds && (x % scale == 0 || y % scale == 0))
					{
						fieldSettings.texture.SetPixel(x, y, Color.black);
						continue ;
					}

					var blendInfo = map.GetBiomeBlendInfo(x, y);
					var firstBiome = switchGraph.GetBiome(blendInfo.firstBiomeId);

					if (firstBiome == null)
					{
						fieldSettings.texture.SetPixel(x, y, new Color(0, 0, 0, 0));
						continue ;
					}
					
					Color finalColor = firstBiome.previewColor;

					//start from 1 because the first biome have already been retreived
					for (int i = 1; i < blendInfo.length; i++)
					{
						int id = blendInfo.biomeIds[i];
						float blend = blendInfo.biomeBlends[i];

						var biome = switchGraph.GetBiome((short)id);

						finalColor = Color.Lerp(finalColor, biome.previewColor, blend);
					}
					
					Color pixel = finalColor;
					pixel.a = 1;

					fieldSettings.texture.SetPixel(x, y, pixel);
				}
			fieldSettings.texture.Apply();
			fieldSettings.update = false;
		}
	#endregion

	#region ObjectPreview field
		
		public void ObjectPreview(object obj, bool update)
		{
			ObjectPreview((GUIContent)null, obj, update);
		}
		
		public void ObjectPreview(string name, object obj, bool update)
		{
			ObjectPreview(new GUIContent(name), obj, update);
		}

		public void ObjectPreview(GUIContent name, object obj, bool update)
		{
			Type objType = obj.GetType();

			if (objType == typeof(Sampler2D))
				Sampler2DPreview(name, obj as Sampler2D, update);
			else if (obj.GetType().IsSubclassOf(typeof(Object)))
			{
				//unity object preview
			}
			else
				Debug.LogWarning("can't preview the object of type: " + obj.GetType());
		}

	#endregion

	#region Texture2DArray preview field

		public void Texture2DArrayPreview(Texture2DArray textureArray, bool update)
		{
			if (textureArray == null)
				return ;
			var	fieldSettings = GetGUISettingData(PWGUIFieldType.Texture2DArrayPreview, () => new PWGUISettings());
			if (update)
			{
				if (fieldSettings.textures == null || fieldSettings.textures.Length < textureArray.depth)
					fieldSettings.textures = new Texture2D[textureArray.depth];
				for (int i = 0; i < textureArray.depth; i++)
				{
					Texture2D tex = new Texture2D(textureArray.width, textureArray.height, TextureFormat.ARGB32, false);
					tex.wrapMode = TextureWrapMode.Clamp;
					tex.SetPixels(textureArray.GetPixels(i));
					tex.Apply();
					fieldSettings.textures[i] = tex;
				}
			}
		
			if (fieldSettings.textures != null)
				foreach (var tex in fieldSettings.textures)
					TexturePreview(tex, false, false, false);
		}
	
	#endregion

	#region PWArray< T > field

	public void PWArrayField< T >(PWArray< T > array)
	{
		var names = array.GetNames();
		var values = array.GetValues();

		EditorGUILayout.LabelField("names: [" + names.Count + "]");
		for (int i = 0; i < values.Count; i++)
		{
			if (i < names.Count && names[i] != null)
			{
				if (values[i] != null)
					EditorGUILayout.LabelField(names[i] + " <" + values[i].GetType() + ": " + values[i] + ">");
				else
					EditorGUILayout.LabelField(names[i]);
			}
			else
				EditorGUILayout.LabelField("null");
		}
	}

	#endregion

	#region FadeGroup block
	
		public bool BeginFade(string header, GUIStyle style = null)
		{
			bool checkbox = false;
			
			return BeginFade(new GUIContent(header), style, ref checkbox, false);
		}

		public bool BeginFade(string header, ref bool checkbox, bool checkboxEnabled = true)
		{
			return BeginFade(new GUIContent(header), null, ref checkbox, checkboxEnabled);
		}
	
		public bool BeginFade(string header, GUIStyle style, ref bool checkbox)
		{
			return BeginFade(new GUIContent(header), style, ref checkbox);
		}

		public bool BeginFade(GUIContent header, GUIStyle style, ref bool checkbox, bool checkboxEnabled = true)
		{
			var e = Event.current;
			var settings = GetGUISettingData(PWGUIFieldType.FadeBlock, () => {
				return new PWGUISettings();
			});

			if (style == null)
				style = Styles.box;

			if (settings.fadeStatus == null)
				settings.fadeStatus = new AnimBool(settings.faded);
			
			AnimBool fadeStatus = settings.fadeStatus as AnimBool;
				
			settings.faded = checkbox;

			EditorGUILayout.BeginVertical(style);

			//header
			EditorGUILayout.BeginHorizontal();
			{
				if (checkboxEnabled)
				{
					EditorGUI.BeginChangeCheck();
					checkbox = EditorGUILayout.Toggle(checkbox);
					if (EditorGUI.EndChangeCheck())
						settings.faded = checkbox;
				}
				EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
			}
			EditorGUILayout.EndHorizontal();

			//click in the header to expand block
			Rect headerRect = GUILayoutUtility.GetLastRect();
			if (headerRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
			{
				settings.faded = !settings.faded;
				e.Use();
			}

			fadeStatus.target = settings.faded;
			checkbox = settings.faded;
			
			bool display = EditorGUILayout.BeginFadeGroup(fadeStatus.faded);
			
			return display;
		}

		public void EndFade()
		{
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.EndVertical();
		}

	#endregion

	#region MinMaxSlope field

	public void MinMaxSlope(float min, float max, ref float inputMin, ref float inputMax)
	{
		EditorGUILayout.BeginVertical();
		{
			EditorGUIUtility.labelWidth = 60;
			inputMin = EditorGUILayout.FloatField("Min", inputMin);
			inputMax = EditorGUILayout.FloatField("Max", inputMax);
			EditorGUIUtility.labelWidth = 0;
		}
		EditorGUILayout.EndVertical();
		Rect r = EditorGUILayout.GetControlRect(false, 60);
		MinMaxSlope(r, ref inputMin, ref inputMax);

		inputMax = Mathf.Clamp(inputMax, min, max);
		inputMin = Mathf.Clamp(inputMin, min, max);
	}

	void MinMaxSlope(Rect drawRect, ref float inputMin, ref float inputMax)
	{
		float gapWidth = drawRect.width / 2;
		float minSlopeWidth = Mathf.Cos(inputMin * Mathf.Deg2Rad) * gapWidth;
		float maxSlopwWidth = Mathf.Cos(inputMax * Mathf.Deg2Rad) * gapWidth;
		float minSlopeHeight = Mathf.Sin(inputMin * Mathf.Deg2Rad) * drawRect.height;
		float maxSlopeHeight = Mathf.Sin(inputMax * Mathf.Deg2Rad) * drawRect.height;

		Color minSlopeColor = Color.blue;
		Color maxSlopeColor = Color.red;
		Color baseColor = Color.black;

		Vector2 b1 = new Vector2(drawRect.xMin, drawRect.yMax);
		Vector2 b2 = new Vector2(b1.x + drawRect.width / 4, drawRect.yMax);

		Vector2 m1 = new Vector2((drawRect.width / 4) + minSlopeWidth + drawRect.xMin, drawRect.yMax - minSlopeHeight);
		Vector2 m2 = new Vector2(drawRect.xMax, drawRect.yMax - minSlopeHeight);

		Vector2 n1 = new Vector2((drawRect.width / 4) + maxSlopwWidth + drawRect.xMin, drawRect.yMax - maxSlopeHeight);
		Vector2 n2 = new Vector2(drawRect.xMax, drawRect.yMax - maxSlopeHeight);

		Handles.color = baseColor;
		Handles.DrawAAPolyLine(b1, b2);
		Handles.DrawAAPolyLine(m1, m2);
		Handles.DrawAAPolyLine(n1, n2);

		Vector2 min1 = b2;
		Vector2 min2 = m1;
		
		Handles.color = minSlopeColor;
		Handles.DrawAAPolyLine(min1, min2);

		Vector2 max1 = b2;
		Vector2 max2 = n1;

		Handles.color = maxSlopeColor;
		Handles.DrawAAPolyLine(max1, max2);
	}

	#endregion

	#region Utils

		private T		GetGUISettingData< T >(PWGUIFieldType fieldType, Func< T > newGUISettings) where T : PWGUISettings
		{
			if (!currentSettingIndices.ContainsKey(fieldType))
				currentSettingIndices[fieldType] = 0;
			
			int fieldIndex = currentSettingIndices[fieldType];

			//TODO: a more intelligent system to get back the stored GUI setting
			if (fieldIndex == settingsStorage.Count(s => s.fieldType == fieldType))
			{
				var s = newGUISettings();
				s.fieldType = fieldType;

				settingsStorage.Add(s);
			}

			int i = 0;
			var fieldSetting = settingsStorage.FirstOrDefault(s => {
				if (i == fieldIndex && s.fieldType == fieldType)
					return true;
				if (s.fieldType == fieldType)
					i++;
				return false;
			});

			currentSettingIndices[fieldType]++;
			return fieldSetting as T;
		}

		public void	StartFrame(Rect currentWindowRect)
		{
			currentSettingIndices.Clear();

			this.currentWindowRect = currentWindowRect;

			if (icColor != null)
				return ;

			icColor = Resources.Load("Icons/ic_color") as Texture2D;
			icEdit = Resources.Load("Icons/ic_edit") as Texture2D;
			icSettingsOutline = Resources.Load("Icons/ic_settings_outline") as Texture2D;
		}

		PWGUISettings FindSetting(PWGUIFieldType fieldType, int fieldIndex)
		{
			int i = 0;
			if (fieldIndex >= 0)
				return settingsStorage.FirstOrDefault(s => {
					if (i == fieldIndex && s.fieldType == fieldType)
						return true;
					if (s.fieldType == fieldType)
						i++;
					return false;
				});
			else
				return settingsStorage.LastOrDefault(s => {
					if (i == -fieldIndex - 1)
						return true;
					if (s.fieldType == fieldType)
						i++;
					return false;
				});
		}

		//A negative value of fieldIndex will take the objectat the specified index starting from the end
		public void SetGradientForField(PWGUIFieldType fieldType, int fieldIndex, Gradient g)
		{
			PWGUISettings setting = FindSetting(fieldType, fieldIndex);

			if (setting != null)
			{
				setting.gradient = g;
				setting.serializableGradient = (SerializableGradient)g;
				setting.update = true;
			}
		}

		public void SetDebugForField(PWGUIFieldType fieldType, int fieldIndex, bool value)
		{
			PWGUISettings setting = FindSetting(fieldType, fieldIndex);

			if (setting != null)
			{
				setting.debug = value;
			}
		}

		public void SetScaleModeForField(PWGUIFieldType fieldType, int fieldIndex, ScaleMode mode)
		{
			PWGUISettings setting = FindSetting(fieldType, fieldIndex);

			if (setting != null)
			{
				setting.scaleMode = mode;
			}
		}
		
		public void SetScaleAspectForField(PWGUIFieldType fieldType, int fieldIndex, float aspect)
		{
			PWGUISettings setting = FindSetting(fieldType, fieldIndex);

			if (setting != null)
			{
				setting.scaleAspect = aspect;
			}
		}
		
		public void SetMaterialForField(PWGUIFieldType fieldType, int fieldIndex, Material mat)
		{
			PWGUISettings setting = FindSetting(fieldType, fieldIndex);

			if (setting != null)
			{
				setting.material = mat;
			}
		}
		
		public void SetUpdateForField(PWGUIFieldType fieldType, int fieldIndex, bool update)
		{
			PWGUISettings setting = FindSetting(fieldType, fieldIndex);

			if (setting != null)
			{
				setting.update = update;
			}
		}

		public void SpaceSkipAnchors()
		{
			if (attachedNode == null)
				return ;
			
			float height = 0;
	
			foreach (var anchorField in attachedNode.anchorFields)
				foreach (var anchor in anchorField.anchors)
					if (!String.IsNullOrEmpty(anchorField.name))
						height = Mathf.Max(height, anchor.rect.yMin - 5);
			
			if (height > 0)
				EditorGUILayout.GetControlRect(false, height, GUILayout.ExpandWidth(true));
		}
	
	#endregion
	}
}
