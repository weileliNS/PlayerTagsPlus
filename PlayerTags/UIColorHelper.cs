using Dalamud.Bindings.ImGui;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PlayerTags;

public static class UIColorHelper
{
    private class UIColorComparer : IEqualityComparer<UIColor>
    {
        public bool Equals(UIColor left, UIColor right)
        {
            return left.Dark == right.Dark;
        }

        public int GetHashCode(UIColor obj)
        {
            return obj.Dark.GetHashCode();
        }
    }

    private static UIColor[] s_UIColors = null!;

    public static IEnumerable<UIColor> UIColors => s_UIColors ??= CreateUIColors();

    public static Vector4 ToColor(UIColor uiColor)
    {
        var uiColorBytes = BitConverter.GetBytes(uiColor.Dark);
        return
            new Vector4((float)uiColorBytes[3] / 255,
            (float)uiColorBytes[2] / 255,
            (float)uiColorBytes[1] / 255,
            (float)uiColorBytes[0] / 255);
    }

    public static Vector4 ToColor(ushort colorId)
    {
        foreach (var uiColor in UIColors)
        {
            if ((ushort)uiColor.RowId == colorId)
                return ToColor(uiColor);
        }

        return new Vector4();
    }

    private static UIColor[] CreateUIColors()
    {
        if (PluginServices.DataManager.GetExcelSheet<UIColor>() is not ExcelSheet<UIColor> uiColors)
            return [];

        var filteredUIColors = new List<UIColor>(uiColors.Distinct(new UIColorComparer()).Where(uiColor => uiColor.Dark != 0 && uiColor.Dark != 255));
        filteredUIColors.Sort((left, right) =>
        {
            var leftColor = ToColor(left);
            var rightColor = ToColor(right);
            var leftHue = 0f;
            var leftSaturation = 0f;
            var leftValue = 0f;
            var rightHue = 0f;
            var rightSaturation = 0f;
            var rightValue = 0f;
            ImGui.ColorConvertRGBtoHSV(leftColor.X, leftColor.Y, leftColor.Z, ref leftHue, ref leftSaturation, ref leftValue);
            ImGui.ColorConvertRGBtoHSV(rightColor.X, rightColor.Y, rightColor.Z, ref rightHue, ref rightSaturation, ref rightValue);

            var hueDifference = leftHue.CompareTo(rightHue);
            if (hueDifference != 0)
                return hueDifference;

            var valueDifference = leftValue.CompareTo(rightValue);
            if (valueDifference != 0)
                return valueDifference;

            var saturationDifference = leftSaturation.CompareTo(rightSaturation);
            if (saturationDifference != 0)
                return saturationDifference;

            return 0;
        });

        return [.. filteredUIColors];
    }
}
