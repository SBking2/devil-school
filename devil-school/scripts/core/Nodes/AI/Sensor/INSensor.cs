
using System;
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public interface INSensor
    {
        public NEnvCreature EnvCreatureOwner { get; }
        public void Bind(Node3D parent);
    }
}
