﻿using System.Windows;
using System.Windows.Controls;
using KoAR.Core;

namespace KoAR.SaveEditor.Views
{
    public sealed class EquipmentTypeIcon : Control
    {
        public static readonly DependencyProperty EquipmentTypeProperty = DependencyProperty.Register(nameof(EquipmentTypeIcon.EquipmentType), typeof(EquipmentType?), typeof(EquipmentTypeIcon));

        static EquipmentTypeIcon() => FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(EquipmentTypeIcon), new FrameworkPropertyMetadata(typeof(EquipmentTypeIcon)));

        public EquipmentType? EquipmentType
        {
            get => (EquipmentType?)this.GetValue(EquipmentTypeIcon.EquipmentTypeProperty);
            set => this.SetValue(EquipmentTypeIcon.EquipmentTypeProperty, value);
        }
    }
}