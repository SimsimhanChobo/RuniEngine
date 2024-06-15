#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;
using BridgeAdvancedDropdown = RuniEngine.Editor.APIBridge.UnityEditor.IMGUI.Controls.AdvancedDropdown;

namespace RuniEngine.Editor
{
    public class RuniAdvancedDropdown : AdvancedDropdown
    {
        public RuniAdvancedDropdown() : base(new AdvancedDropdownState())
        {
            bridgeAdvancedDropdown = BridgeAdvancedDropdown.GetInstance(this);
            minimumSize = new Vector2(0, 300);
        }

        public bool isComplicacy { get; private set; }

        public BridgeAdvancedDropdown bridgeAdvancedDropdown;

        string[]? selectedPaths;
        Enum? selectedEnum;
        SerializedProperty? selectedProperty;
        RuniAdvancedDropdownItem? selectedItem;

        public void BuildRoot(string[] paths, bool isComplicacy = false)
        {
            selectedPaths = paths;
            selectedEnum = null;
            selectedProperty = null;
            this.isComplicacy = isComplicacy;

            BuildRoot();
        }

        public void BuildRoot(Enum selected)
        {
            selectedPaths = null;
            selectedEnum = selected;
            selectedProperty = null;
            isComplicacy = false;

            BuildRoot();
        }

        public void BuildRoot(SerializedProperty property)
        {
            selectedPaths = null;
            selectedEnum = null;
            selectedProperty = property;
            isComplicacy = false;

            BuildRoot();
        }

        public void Clear()
        {
            bridgeAdvancedDropdown.m_State = new AdvancedDropdownState();

            selectedPaths = null;
            selectedEnum = null;
            selectedProperty = null;
            isComplicacy = false;

            BuildRoot();
        }

        public int DrawLayout(int index, string[] values, params GUILayoutOption[] options) => DrawLayout(index, values, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public int DrawLayout(int index, string[] values, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(index, values, focusType, EditorStyles.miniPullDown, options);
        public int DrawLayout(int index, string[] values, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            BuildRoot(values);
            DrawLayoutButton(index >= 0 && index < values.Length ? values[index] : "", focusType, style, options);

            int result = selectedItem?.index ?? index;
            selectedItem = null;

            return result;
        }

        public int Draw(Rect position, int index, string[] values) => Draw(position, index, values, FocusType.Keyboard, EditorStyles.miniPullDown);
        public int Draw(Rect position, int index, string[] values, FocusType focusType) => Draw(position, index, values, focusType, EditorStyles.miniPullDown);
        public int Draw(Rect position, int index, string[] values, FocusType focusType, GUIStyle style)
        {
            BuildRoot(values);
            DrawButton(position, index >= 0 && index < values.Length ? values[index] : string.Empty, focusType, style);

            int result = selectedItem?.index ?? index;
            selectedItem = null;

            return result;
        }

        public T DrawLayout<T>(T enumValue, params GUILayoutOption[] options) where T : Enum => (T)DrawLayout((Enum)enumValue, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public T DrawLayout<T>(T enumValue, FocusType focusType, params GUILayoutOption[] options) where T : Enum => (T)DrawLayout((Enum)enumValue, focusType, EditorStyles.miniPullDown, options);
        public T DrawLayout<T>(T enumValue, FocusType focusType, GUIStyle style, params GUILayoutOption[] options) where T : Enum => (T)DrawLayout((Enum)enumValue, focusType, style, options);

        public Enum DrawLayout(Enum enumValue, params GUILayoutOption[] options) => DrawLayout(enumValue, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public Enum DrawLayout(Enum enumValue, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(enumValue, focusType, EditorStyles.miniPullDown, options);
        public Enum DrawLayout(Enum enumValue, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            BuildRoot(enumValue);

            DrawLayoutButton(new GUIContent(enumValue.ToString()), focusType, style, options);

            Enum result;
            if (selectedItem != null)
                result = (Enum)Enum.ToObject(enumValue.GetType(), selectedItem.index);
            else
                result = enumValue;

            return result;
        }

        public T Draw<T>(Rect position, T enumValue) where T : Enum => (T)Draw(position, (Enum)enumValue, FocusType.Keyboard, EditorStyles.miniPullDown);
        public T Draw<T>(Rect position, T enumValue, FocusType focusType) where T : Enum => Draw(position, enumValue, focusType, EditorStyles.miniPullDown);
        public T Draw<T>(Rect position, T enumValue, FocusType focusType, GUIStyle style) where T : Enum => Draw<T>(position, enumValue, focusType, style);

        public object Draw(Rect position, Enum enumValue) => Draw(position, enumValue, FocusType.Keyboard, EditorStyles.miniPullDown);
        public object Draw(Rect position, Enum enumValue, FocusType focusType) => Draw(position, enumValue, focusType, EditorStyles.miniPullDown);
        public object Draw(Rect position, Enum enumValue, FocusType focusType, GUIStyle style)
        {
            BuildRoot(enumValue);

            DrawButton(position, new GUIContent(enumValue.ToString()), focusType, style);
            return Enum.ToObject(enumValue.GetType(), selectedItem?.index ?? 0);
        }

        public void DrawLayout(SerializedProperty property, params GUILayoutOption[] options) => DrawLayout(property, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public void DrawLayout(SerializedProperty property, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(property, focusType, EditorStyles.miniPullDown, options);
        public void DrawLayout(SerializedProperty property, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            BuildRoot(property);
            DrawLayoutButton(property.enumDisplayNames[property.enumValueIndex], focusType, style, options);

            property.enumValueIndex = selectedItem?.index ?? property.enumValueIndex;
            selectedItem = null;
        }

        public void Draw(Rect position, SerializedProperty property) => Draw(position, property, FocusType.Keyboard, EditorStyles.miniPullDown);
        public void Draw(Rect position, SerializedProperty property, FocusType focusType) => Draw(position, property, focusType, EditorStyles.miniPullDown);
        public void Draw(Rect position, SerializedProperty property, FocusType focusType, GUIStyle style)
        {
            BuildRoot(property);
            DrawButton(position, property.enumDisplayNames[property.enumValueIndex], focusType, style);

            property.enumValueIndex = selectedItem?.index ?? property.enumValueIndex;
            selectedItem = null;
        }

        public string DrawLayoutPath(string path, string[] paths, params GUILayoutOption[] options) => DrawLayoutPath(path, paths, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public string DrawLayoutPath(string path, string[] paths, FocusType focusType, params GUILayoutOption[] options) => DrawLayoutPath(path, paths, focusType, EditorStyles.miniPullDown, options);
        public string DrawLayoutPath(string path, string[] paths, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            BuildRoot(paths, true);
            DrawLayoutButton(PathUtility.GetFileName(path), focusType, style, options);

            string result = selectedItem?.path ?? path;
            selectedItem = null;

            return result;
        }

        public string DrawPath(Rect position, string path, string[] paths) => DrawPath(position, path, paths, FocusType.Keyboard, EditorStyles.miniPullDown);
        public string DrawPath(Rect position, string path, string[] paths, FocusType focusType) => DrawPath(position, path, paths, focusType, EditorStyles.miniPullDown);
        public string DrawPath(Rect position, string path, string[] paths, FocusType focusType, GUIStyle style)
        {
            BuildRoot(paths, true);
            DrawButton(position, PathUtility.GetFileName(path), focusType, style);

            string result = selectedItem?.path ?? path;
            selectedItem = null;

            return result;
        }

        public void DrawLayoutButton(string content, params GUILayoutOption[] options) => DrawLayoutButton(new GUIContent(content), FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public void DrawLayoutButton(GUIContent content, params GUILayoutOption[] options) => DrawLayoutButton(content, FocusType.Keyboard, EditorStyles.miniPullDown, options);

        public void DrawLayoutButton(string content, FocusType focusType, params GUILayoutOption[] options) => DrawLayoutButton(new GUIContent(content), focusType, EditorStyles.miniPullDown, options);
        public void DrawLayoutButton(GUIContent content, FocusType focusType, params GUILayoutOption[] options) => DrawLayoutButton(content, focusType, EditorStyles.miniPullDown, options);

        public void DrawLayoutButton(string content, FocusType focusType, GUIStyle style, params GUILayoutOption[] options) => DrawLayoutButton(new GUIContent(content), focusType, style, options);
        public void DrawLayoutButton(GUIContent content, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect position = EditorGUILayout.GetControlRect(false, GetYSize(style), options);
            DrawButton(position, content, focusType, style);
        }

        public void DrawButton(Rect position, string content) => DrawButton(position, new GUIContent(content), FocusType.Keyboard, EditorStyles.miniPullDown);
        public void DrawButton(Rect position, GUIContent content) => DrawButton(position, content, FocusType.Keyboard, EditorStyles.miniPullDown);

        public void DrawButton(Rect position, string content, FocusType focusType) => DrawButton(position, new GUIContent(content), focusType, EditorStyles.miniPullDown);
        public void DrawButton(Rect position, GUIContent content, FocusType focusType) => DrawButton(position, content, focusType, EditorStyles.miniPullDown);

        public void DrawButton(Rect position, string content, FocusType focusType, GUIStyle style) => DrawButton(position, new GUIContent(content), focusType, style);
        public void DrawButton(Rect position, GUIContent content, FocusType focusType, GUIStyle style)
        {
            if (EditorGUI.DropdownButton(position, content, focusType, style))
            {
                bridgeAdvancedDropdown.m_State = new AdvancedDropdownState();
                Show(position);
            }
        }

        readonly Dictionary<string, RuniAdvancedDropdownItem> buildRootPaths = new();
        protected override AdvancedDropdownItem BuildRoot()
        {
            RuniAdvancedDropdownItem root = new RuniAdvancedDropdownItem(string.Empty, string.Empty, -1);

            string[] array;
            if (selectedPaths != null)
                array = selectedPaths;
            else if (selectedEnum != null)
                array = selectedEnum.GetType().GetEnumNames();
            else if (selectedProperty != null)
                array = selectedProperty.enumDisplayNames;
            else
                return root;

            if (isComplicacy)
            {
                Array.Sort(array);

                buildRootPaths.Clear();
                for (int i = 0; i < array.Length; i++)
                {
                    string path = array[i];
                    if (string.IsNullOrEmpty(path))
                    {
                        root.AddChild(new RuniAdvancedDropdownItem(string.Empty, TryGetText("gui.none"), i));

                        if (array.Length > 1)
                            root.AddSeparator();

                        continue;
                    }

                    string[] splitPaths = path.Split('/');
                    
                    string splitAllPath = string.Empty;
                    for (int j = 0; j < splitPaths.Length; j++)
                    {
                        string splitPath = splitPaths[j];
                        splitAllPath = PathUtility.Combine(splitAllPath, splitPath);

                        if (!buildRootPaths.ContainsKey(splitAllPath))
                        {
                            string parentPath = PathUtility.GetParentPath(splitAllPath);

                            RuniAdvancedDropdownItem item = new RuniAdvancedDropdownItem(splitAllPath, PathUtility.GetFileName(splitAllPath), i);
                            if (!buildRootPaths.TryGetValue(parentPath, out RuniAdvancedDropdownItem parentItem))
                                parentItem = root;

                            if (i + 1 < array.Length)
                            {
                                int nextPathIndex = i + 1;
                                if (PathUtility.StartsWith(array[nextPathIndex], path))
                                    parentItem.AddChild(new RuniAdvancedDropdownItem(path, PathUtility.GetFileName(path), i));
                            }

                            parentItem.AddChild(item);
                            buildRootPaths.Add(splitAllPath, item);
                        }
                    }
                }

            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    string name = array[i];
                    root.AddChild(new RuniAdvancedDropdownItem(name, name, i));
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item) => selectedItem = (RuniAdvancedDropdownItem)item;
    }
}
