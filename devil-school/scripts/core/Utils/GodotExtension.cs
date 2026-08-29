
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
}