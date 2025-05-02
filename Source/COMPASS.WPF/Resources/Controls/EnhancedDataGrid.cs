﻿//https://bengribaudo.com/blog/2012/03/14/1942/saving-restoring-wpf-datagrid-columns-size-sorting-and-order

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using COMPASS.Services;
using COMPASS.Tools;
using Newtonsoft.Json;

namespace COMPASS.Resources.Controls
{
    class EnhancedDataGrid : DataGrid
    {
        private bool inWidthChange = false;
        private bool updatingColumnInfo = false;
        public static readonly DependencyProperty ColumnInfoProperty = DependencyProperty.Register("ColumnInfo",
                typeof(ObservableCollection<ColumnInfo>), typeof(EnhancedDataGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColumnInfoChangedCallback)
            );
        private static void ColumnInfoChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var grid = (EnhancedDataGrid)dependencyObject;
            if (!grid.updatingColumnInfo) { grid.ApplySorting(); }
        }
        protected override void OnInitialized(EventArgs e)
        {
            EventHandler widthPropertyChangedHandler = (sender, x) => inWidthChange = true;
            var widthPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
            Loaded += (sender, x) =>
            {
                foreach (var column in Columns)
                {
                    widthPropertyDescriptor.AddValueChanged(column, widthPropertyChangedHandler);
                }
            };
            Unloaded += (sender, x) =>
            {
                foreach (var column in Columns)
                {
                    widthPropertyDescriptor.RemoveValueChanged(column, widthPropertyChangedHandler);
                }
            };
            base.OnInitialized(e);
        }
        public ObservableCollection<ColumnInfo>? ColumnInfo
        {
            get => (ObservableCollection<ColumnInfo>)GetValue(ColumnInfoProperty);
            set => SetValue(ColumnInfoProperty, value);
        }
        private void UpdateColumnInfo()
        {
            PreferencesService prefsService = PreferencesService.GetInstance();

            updatingColumnInfo = true;
            ColumnInfo = new ObservableCollection<ColumnInfo>(Columns.Select((x) => new ColumnInfo(x)));
            //persist data
            string json = JsonConvert.SerializeObject(ColumnInfo); //serializing

            //Save Sorting
            SortDescription sd = Items.SortDescriptions.FirstOrDefault();
            if (!string.IsNullOrEmpty(sd.PropertyName))
            {
                prefsService.Preferences.UIState.SortProperty = sd.PropertyName;
                prefsService.Preferences.UIState.SortDirection = sd.Direction;
            }

            try
            {
                Properties.Settings.Default["DataGridCollumnInfo"] = json;
                Properties.Settings.Default.Save();
                updatingColumnInfo = false;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save Preferences", ex);
            }
        }
        protected override void OnColumnReordered(DataGridColumnEventArgs e)
        {
            UpdateColumnInfo();
            base.OnColumnReordered(e);
        }
        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            Dispatcher.BeginInvoke(UpdateColumnInfo);
            base.OnSorting(eventArgs);
        }
        protected override void OnPreviewMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (inWidthChange)
            {
                inWidthChange = false;
                UpdateColumnInfo();
            }
            base.OnPreviewMouseLeftButtonUp(e);
        }

        public void ApplySorting()
        {
            PreferencesService prefsService = PreferencesService.GetInstance();

            ListSortDirection direction = prefsService.Preferences.UIState.SortDirection;
            string prop = prefsService.Preferences.UIState.SortProperty;

            if (ColumnInfo is null) return;
            foreach (var column in Columns)
            {
                //var realColumn = Columns.FirstOrDefault(x => column.Header == x.Header);
                if (column.SortMemberPath == prop)
                {
                    column.SortDirection = direction;
                    return;
                }
            }

            foreach (var column in ColumnInfo)
            {
                var realColumn = Columns.Where((x) => column.Header.Equals(x.Header)).FirstOrDefault();
                if (realColumn == null) { continue; }
                column.Apply(realColumn, Columns.Count);
            }
        }

        //Fix Sorting not applying
        //https://stackoverflow.com/questions/11177351/wpf-datagrid-ignores-sortdescription
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            ApplySorting();
            base.OnItemsSourceChanged(oldValue, newValue);
        }
    }
    public struct ColumnInfo
    {
        public ColumnInfo(DataGridColumn column)
        {
            Header = column.Header;
            WidthValue = column.Width.DisplayValue;
            WidthType = column.Width.UnitType;
            DisplayIndex = column.DisplayIndex;
        }
        public void Apply(DataGridColumn column, int gridColumnCount)
        {
            if (WidthValue is double.NaN)
            {
                WidthValue = 0;
            }
            column.Width = new DataGridLength(WidthValue, WidthType);
            if (column.DisplayIndex != DisplayIndex)
            {
                var maxIndex = (gridColumnCount == 0) ? 0 : gridColumnCount - 1;
                column.DisplayIndex = (DisplayIndex <= maxIndex) ? DisplayIndex : maxIndex;
            }
        }
        public object Header;
        public int DisplayIndex;
        public double WidthValue;
        public DataGridLengthUnitType WidthType;
    }
}
