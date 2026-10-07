
using Godot;
using System;

namespace EGame
{
    public static class NodeExtension
    {
        public static void SetActive(this Node node, bool active)
        {
            node.ProcessMode = active ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        }

        public static AnimationPlayer GetAnimationPlayer(this Node node)
        {
            var animator = node.GetNodeOrNull<AnimationPlayer>("%AnimationPlayer");
            return animator;
        }
    }

    public static class Node3DExtension
    {
        public static void SetActive(this Node3D node, bool active)
        {
            node.Visible = active;
            node.ProcessMode = active ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        }
    }

    public static class CharacterBody3DExtension
    {
        // MoveAndSlide 用引擎自己的 delta 推进位置，没法缩放，所以先把速度按倍率压缩，走完再还原
        public static bool MoveAndSlideScaled(this CharacterBody3D body, float time_scale)
        {
            Vector3 real_velocity = body.Velocity;
            body.Velocity = real_velocity * time_scale;
            bool collided = body.MoveAndSlide();
            body.Velocity = time_scale > 0.0001f ? body.Velocity / time_scale : real_velocity;
            return collided;
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