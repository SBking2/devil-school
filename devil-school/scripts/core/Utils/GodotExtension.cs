
using Godot;
using System;

namespace EGame
{
    public static class Node3DExtension
    {
        public static void SetActive(this Node3D node, bool active)
        {
            node.Visible = active;
            node.ProcessMode = active ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        }
    }

    public static class CanvasItemExtension
    {
        public static void SetActive(this CanvasItem canvas_item, bool active)
        {
            canvas_item.Visible = active;
            canvas_item.ProcessMode = active ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        }
    }
}