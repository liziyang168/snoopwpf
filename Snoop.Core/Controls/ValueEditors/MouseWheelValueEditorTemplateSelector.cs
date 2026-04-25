namespace Snoop.Controls.ValueEditors;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Snoop.Infrastructure;

public class MouseWheelValueEditorTemplateSelector : DataTemplateSelector
{
    public static readonly MouseWheelValueEditorTemplateSelector Default = new();

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not PropertyInformation propertyInformation
            || container is not FrameworkElement uiElement)
        {
            return null;
        }

        var templateName = propertyInformation.PropertyType.Name switch
        {
            nameof(Int16) or nameof(Int32) or nameof(Decimal) or nameof(Double) or nameof(Single) or nameof(Visibility) or nameof(HorizontalAlignment) or nameof(VerticalAlignment) => "CustomEditSingleField",
            nameof(Thickness) => "CustomEditThickness",
            nameof(Brush) when propertyInformation.Value is not GradientBrush and not TileBrush => "CustomEditBrush",
            nameof(Color) => "CustomEditBrush",
            // Catch all to prevent editing data types (based on runtime type) that we cant support (i.e. GradientBrush)
            // and use the empty template.
            var _ => "DefaultEmptyTemplate"
        };

        return (DataTemplate)uiElement.FindResource(templateName);
    }
}