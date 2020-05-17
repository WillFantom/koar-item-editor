﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using KoAR.Core;
using KoAR.SaveEditor.Constructs;
using Microsoft.Win32;
using TaskDialogInterop;

namespace KoAR.SaveEditor.Views
{
    public sealed class MainViewModel : NotifierBase
    {
        public static IReadOnlyList<EffectInfo> Effects = MainViewModel.LoadAllEffects();

        private readonly ObservableCollection<ItemModel> _items;
        private string? _currentDurabilityFilter = string.Empty;
        private AmalurSaveEditor? _editor;
        private string? _fileName;
        private IReadOnlyList<ItemModel> _filteredItems;
        private int _inventorySize;
        private string? _itemNameFilter = string.Empty;
        private string? _maxDurabilityFilter = string.Empty;
        private EffectInfo? _selectedEffect = MainViewModel.Effects.FirstOrDefault();
        private ItemModel? _selectedItem;
        private bool _unsavedChanges;

        public MainViewModel()
        {
            this.OpenFileCommand = new DelegateCommand(this.OpenFile);
            this._filteredItems = this.Items = new ReadOnlyObservableCollection<ItemModel>(this._items = new ObservableCollection<ItemModel>());
            this.MakeAllItemsSellableCommand = new DelegateCommand(this.MakeAllItemsSellable, this.CanMakeAllItemsSellable);
            this.ResetFiltersCommand = new DelegateCommand(this.ResetFilters);
            this.EditItemHexCommand = new DelegateCommand<ItemModel>(this.EditItemHex);
            this.UpdateInventorySizeCommand = new DelegateCommand(this.UpdateInventorySize, this.CanUpdateInventorySize);
            this.AddEffectCommand = new DelegateCommand<EffectInfo>(this.AddEffect);
            this.DeleteEffectCommand = new DelegateCommand<EffectInfo>(this.DeleteEffect);
            this.SaveCommand = new DelegateCommand(this.Save);
        }

        public DelegateCommand<EffectInfo> AddEffectCommand
        {
            get;
        }

        public string? CurrentDurabilityFilter
        {
            get => this._currentDurabilityFilter;
            set
            {
                if (this._currentDurabilityFilter == value)
                {
                    return;
                }
                this._currentDurabilityFilter = value;
                this.OnPropertyChanged();
                this.OnFilterChange();
            }
        }

        public DelegateCommand<EffectInfo> DeleteEffectCommand
        {
            get;
        }

        public DelegateCommand<ItemModel> EditItemHexCommand
        {
            get;
        }

        public string? FileName
        {
            get => this._fileName;
            private set => this.SetValue(ref this._fileName, value);
        }

        public IReadOnlyList<ItemModel> FilteredItems
        {
            get => this._filteredItems;
            private set => this.SetValue(ref this._filteredItems, value);
        }

        public int InventorySize
        {
            get => this._inventorySize;
            set => this.SetValue(ref this._inventorySize, value);
        }

        public string? ItemNameFilter
        {
            get => this._itemNameFilter;
            set
            {
                if (this._itemNameFilter == value)
                {
                    return;
                }
                this._itemNameFilter = value;
                this.OnPropertyChanged();
                this.OnFilterChange();
            }
        }

        public ReadOnlyObservableCollection<ItemModel> Items
        {
            get;
        }

        public DelegateCommand MakeAllItemsSellableCommand
        {
            get;
        }

        public string? MaxDurabilityFilter
        {
            get => this._maxDurabilityFilter;
            set
            {
                if (this._maxDurabilityFilter == value)
                {
                    return;
                }
                this._maxDurabilityFilter = value;
                this.OnPropertyChanged();
                this.OnFilterChange();
            }
        }

        public DelegateCommand OpenFileCommand
        {
            get;
        }

        public DelegateCommand ResetFiltersCommand
        {
            get;
        }

        public DelegateCommand SaveCommand
        {
            get;
        }

        public EffectInfo? SelectedEffect
        {
            get => this._selectedEffect;
            set => this.SetValue(ref this._selectedEffect, value);
        }

        public ItemModel? SelectedItem
        {
            get => this._selectedItem;
            set
            {
                if (value == this._selectedItem)
                {
                    return;
                }
                if (this._selectedItem != null)
                {
                    PropertyChangedEventManager.RemoveHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.ItemName));
                    PropertyChangedEventManager.RemoveHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.CurrentDurability));
                    PropertyChangedEventManager.RemoveHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.MaxDurability));
                    PropertyChangedEventManager.RemoveHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.IsUnsellable));
                }
                if ((this._selectedItem = value) != null)
                {
                    PropertyChangedEventManager.AddHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.ItemName));
                    PropertyChangedEventManager.AddHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.CurrentDurability));
                    PropertyChangedEventManager.AddHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.MaxDurability));
                    PropertyChangedEventManager.AddHandler(this._selectedItem, this.SelectedItem_MateriallyChanged, nameof(ItemModel.IsUnsellable));
                }
                this.OnPropertyChanged();
            }
        }

        public bool? UnsavedChanges
        {
            get => this._fileName == null ? default(bool?) : this._unsavedChanges;
            private set => this.SetValue(ref this._unsavedChanges, value.GetValueOrDefault());
        }

        public DelegateCommand UpdateInventorySizeCommand
        {
            get;
        }

        private static IReadOnlyList<EffectInfo> LoadAllEffects()
        {
            if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(Window)).DefaultValue)
            {
                return Array.Empty<EffectInfo>();
            }
            using Stream stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "properties.xml"));
            return XDocument.Load(stream).Root
                .Elements()
                .Select(element => new EffectInfo { Code = element.Attribute("id").Value.ToUpper(), DisplayText = element.Value.ToUpper() })
                .ToList(); // xaml will bind to `Count` property so keeping consistent with `ItemModel.Effects`.
        }

        private void AddEffect(EffectInfo info)
        {
            if (info == null || this.SelectedItem == null)
            {
                return;
            }
            this.SelectedItem.AddEffect(info.Clone());
            this.SelectedEffect = MainViewModel.Effects[0];
            this.Refresh();
        }

        private bool CanMakeAllItemsSellable()
        {
            return this._editor != null && this._fileName != null && this._items.Any(item => item.IsUnsellable);
        }

        private bool CanUpdateInventorySize() => this._editor != null && this._editor.GetMaxBagCount() != this.InventorySize;

        private void DeleteEffect(EffectInfo info)
        {
            this.SelectedItem?.DeleteEffect(info);
            this.Refresh();
        }

        private void EditItemHex(ItemModel item)
        {
            if (this._editor == null)
            {
                return;
            }
            ItemEditorView view = new ItemEditorView
            {
                Owner = Application.Current.MainWindow,
                DataContext = new ItemEditorViewModel(this._editor, item.GetItem())
            };
            if (view.ShowDialog() == true)
            {
                this.Refresh();
            }
        }

        private void MakeAllItemsSellable()
        {
            if (this._editor == null)
            {
                return;
            }
            int count = 0;
            foreach (ItemModel item in this.Items)
            {
                if (!item.IsUnsellable)
                {
                    continue;
                }
                item.IsUnsellable = false;
                this._editor.WriteEquipmentBytes(item.GetItem(), out _);
                count++;
            }
            TaskDialog.Show(new TaskDialogOptions
            {
                Title = "KoAR Save Editor",
                Owner = Application.Current.MainWindow,
                CommonButtons = TaskDialogCommonButtons.Close,
                Content = $"Modified {count} items.",
                MainIcon = VistaTaskDialogIcon.Information
            });
            if (count > 0)
            {
                this.UnsavedChanges = true;
            }
        }

        private void OnFilterChange()
        {
            IEnumerable<ItemModel> items = this.Items;
            if (!string.IsNullOrEmpty(this._currentDurabilityFilter) && float.TryParse(this._currentDurabilityFilter, out float single))
            {
                int temp = (int)Math.Floor(single);
                items = items.Where(model => (int)Math.Floor(model.CurrentDurability) == temp);
            }
            if (!string.IsNullOrEmpty(this._maxDurabilityFilter) && float.TryParse(this._maxDurabilityFilter, out single))
            {
                int temp = (int)Math.Floor(single);
                items = items.Where(model => (int)Math.Floor(model.MaxDurability) == temp);
            }
            if (!string.IsNullOrEmpty(this._itemNameFilter))
            {
                items = items.Where(model => model.ItemName.IndexOf(this._itemNameFilter, StringComparison.OrdinalIgnoreCase) != -1);
            }
            this.FilteredItems = object.ReferenceEquals(items, this.Items)
                ? (IReadOnlyList<ItemModel>)this.Items
                : items.ToList();
            this.SelectedItem = null;
        }

        private void OpenFile()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Open Save File...",
                DefaultExt = ".sav",
                Filter = "Save Files (*.sav)|*.sav",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            this._editor = new AmalurSaveEditor();
            this._editor.ReadFile(this.FileName = dialog.FileName);
            this.InventorySize = this._editor.GetMaxBagCount();
            this.RepopulateItems();
            this.ResetFilters();
            this._unsavedChanges = false;
            this.OnPropertyChanged(nameof(this.UnsavedChanges));
        }

        private void Refresh()
        {
            int? selectedItemIndex = this._selectedItem?.ItemIndex;
            this.RepopulateItems();
            if (selectedItemIndex.HasValue)
            {
                this.SelectedItem = this._items.FirstOrDefault(item => item.ItemIndex == selectedItemIndex.Value);
            }
            this.UnsavedChanges = true;
            this.SelectedEffect = MainViewModel.Effects[0];
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Formerly called ShowAll or btnShowAll_Click
        /// </summary>
        private void RepopulateItems()
        {
            if (this._editor == null)
            {
                MessageBox.Show("No save file opened!", "KoAR Save Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.OpenFile();
                return;
            }
            this._items.Clear();
            foreach (ItemMemoryInfo info in this._editor.GetAllEquipment())
            {
                this._items.Add(new ItemModel(this._editor, info));
            }
        }

        private void ResetFilters()
        {
            this._itemNameFilter = this._currentDurabilityFilter = this._maxDurabilityFilter = string.Empty;
            this.OnFilterChange();
        }

        private void Save()
        {
            if (this._editor == null)
            {
                return;
            }
            File.Copy(this._fileName, $"{this._fileName}.bak", true);
            this._editor.SaveFile(this._fileName);
            this.UnsavedChanges = false;
            this.RepopulateItems();
            MessageBox.Show($"Save successful! Original save backed up as {this._fileName}.bak.", "KoAR Save Editor", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SelectedItem_MateriallyChanged(object sender, EventArgs e)
        {
            if (this._editor == null)
            {
                return;
            }
            ItemModel model = (ItemModel)sender;
            this._editor.WriteEquipmentBytes(model.GetItem(), out bool lengthChanged);
            if (lengthChanged)
            {
                this.Refresh();
            }
            else
            {
                this.UnsavedChanges = true;
            }
        }

        private void UpdateInventorySize()
        {
            if (this._editor == null)
            {
                return;
            }
            this._editor.EditMaxBagCount(this.InventorySize);
            this.UnsavedChanges = true;
        }
    }
}
