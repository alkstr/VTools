﻿using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace VTools.Views
{
    public static class ViewUtilities
    {
        public static void ShowAttachedFlyoutWithText(Control control, string text)
        {
            FlyoutBase.ShowAttachedFlyout(control);
            var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(control)!;
            flyout.Content = text;
        }
    }
}
