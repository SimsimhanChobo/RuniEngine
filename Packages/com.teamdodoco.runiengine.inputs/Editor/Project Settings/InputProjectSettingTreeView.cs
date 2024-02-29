#nullable enable
using RuniEngine.Inputs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RuniEngine.Editor
{
    public class InputProjectSettingTreeView : TreeView
    {
        public InputProjectSettingTreeView(TreeViewState state) : base(state) { }

        public new InputProjectSettingTreeViewItem rootItem => (InputProjectSettingTreeViewItem)base.rootItem;

        public Dictionary<string, int> itemIDs { get; } = new Dictionary<string, int>();

        public InputProjectSettingTreeViewItem? selectedItem { get; private set; }
        public InputProjectSettingTreeViewItem?[] selectedItems { get; private set; } = new InputProjectSettingTreeViewItem[0];

        public event Action<IList<int>>? selectionChanged;



        public new void Reload()
        {
            base.Reload();
            SetSelectedItem(GetSelection());
        }

        protected override TreeViewItem BuildRoot()
        {
            InputProjectSettingTreeViewItem root = new InputProjectSettingTreeViewItem(0, -1, "", "root");

            List<KeyValuePair<string, KeyCode[]>> controls = InputManager.ProjectData.controlList.ToList();
            Dictionary<string, InputProjectSettingTreeViewItem> items = new Dictionary<string, InputProjectSettingTreeViewItem>();

            itemIDs.Clear();

            int id = 1;
            for (int i = 0; i < controls.Count; i++)
            {
                KeyValuePair<string, KeyCode[]> pair = controls[i];
                string[] keySplit = pair.Key.Split('.');

                string allKey = "";
                for (int j = 0; j < keySplit.Length; j++)
                {
                    string key = keySplit[j];
                    string parentAllKey = allKey;

                    if (j > 0)
                        allKey += "." + key;
                    else
                        allKey += key;

                    if (!items.ContainsKey(allKey))
                    {
                        if (j > 0)
                        {
                            InputProjectSettingTreeViewItem item = new InputProjectSettingTreeViewItem(id, allKey, key);

                            items[parentAllKey].AddChild(item);
                            items.Add(allKey, item);

                            itemIDs.Add(allKey, id);
                            id++;
                        }
                        else
                        {
                            InputProjectSettingTreeViewItem item = new InputProjectSettingTreeViewItem(id, allKey, key);

                            items.Add(allKey, item);
                            root.AddChild(item);

                            itemIDs.Add(allKey, id);
                            id++;
                        }
                    }
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIDs)
        {
            SetSelectedItem(selectedIDs);
            selectionChanged?.Invoke(selectedIDs);
        }

        void SetSelectedItem(IList<int> selectedIDs)
        {
            if (selectedIDs.Count == 1)
                selectedItem = (InputProjectSettingTreeViewItem)FindItem(selectedIDs[0], rootItem);
            else
                selectedItem = null;

            selectedItems = new InputProjectSettingTreeViewItem[selectedIDs.Count];
            for (int i = 0; i < selectedItems.Length; i++)
                selectedItems[i] = (InputProjectSettingTreeViewItem)FindItem(selectedIDs[i], rootItem);
        }

        readonly int[] setSelectionIDs = new int[] { 0 };
        public void SetSelection(int selectedID)
        {
            setSelectionIDs[0] = selectedID;
            SetSelection(setSelectionIDs);
        }

        public void SetSelection(params int[] selectedIDs) => SetSelection((IList<int>)selectedIDs);

        public InputProjectSettingTreeViewItem FindItem(int id) => (InputProjectSettingTreeViewItem)FindItem(id, rootItem);
    }
}
