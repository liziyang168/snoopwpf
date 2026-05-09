// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure.Helpers;

using System;
using System.Windows;
using System.Windows.Media;

public static class VisualTreeHelper2
{
    public static T? GetAncestor<T>(DependencyObject input, Predicate<T>? predicate = null)
        where T : DependencyObject
    {
        var current = input;

        {
            if (current is T result
                && predicate?.Invoke(result) != false)
            {
                return result;
            }
        }

        while (current is not null)
        {
            current = VisualTreeHelper.GetParent(current);

            if (current is T result
                && predicate?.Invoke(result) != false)
            {
                return result;
            }
        }

        return null;
    }
}