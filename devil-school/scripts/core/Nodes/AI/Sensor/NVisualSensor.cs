using Godot;
using System;
using System.Collections.Generic;

namespace EGame
{
    public partial class NVisualSensor : Area3D, INSensor
    {
        public static NVisualSensor Create(NEnvCreature ncreate)
        {
            var instance = new NVisualSensor();
            instance._Owner = ncreate;
            return instance;
        }

        private NEnvCreature _Owner;
        NEnvCreature INSensor.Owner => _Owner;

        public void Bind(Node3D parent)
        {
            parent.AddChild(this);
        }
    }
}
