using System;
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NVisualSensor : Area3D, INSensor
    {
        public enum SensorShape
        {
            Sphere = 0,
            Sentor = 1,
            Box = 2
        }

        public static NVisualSensor Create(NEnvCreature ncreature, SensorShape sensor_shape, int layer, int mask, Action<IEnumerable<NEnvCreature>> callback = null)
        {
            NVisualSensor instance = null;
            
            switch(sensor_shape)
            {
                case SensorShape.Sphere:    instance = new NSphereSensor();     break;
                case SensorShape.Sentor:    instance = new NSectorSensor();     break;
                default:
                    throw new InvalidOperationException($"Unknow shape type : {sensor_shape.ToString()}!");
            }

            instance._Owner = ncreature;
            instance.CollisionLayer = 1u << layer;
            instance.CollisionMask = 1u << mask;
            instance._OnCreaturesInShapeChanged = callback;
            return instance;
        }

        private NEnvCreature _Owner;
        public NEnvCreature EnvCreatureOwner => _Owner;
        public IEnumerable<NEnvCreature> CreaturesInShape => _CreatesInShape;

        private event Action<IEnumerable<NEnvCreature>> _OnCreaturesInShapeChanged;
        
        public void Bind(Node3D parent)
        {
            parent.AddChild(this);
        }

        public override void _Ready()
        {
            base._Ready();

            var shape = CreateShpae();
            this.AddChild(shape);

            BodyEntered += OnEnvCreatureEnter;
            BodyExited -= OnEnvCreatureExit;
        }

        private HashSet<NEnvCreature> _CreatesInShape = new HashSet<NEnvCreature>();

        private void OnEnvCreatureEnter(Node3D body)
        {
            var ncreature = body as NEnvCreature;
            if(ncreature != null)
            {
                if (_CreatesInShape.Contains(ncreature) == false)
                {
                    _CreatesInShape.Add(ncreature);
                    _OnCreaturesInShapeChanged?.Invoke(CreaturesInShape);
                }
            }
        }

        private void OnEnvCreatureExit(Node3D body)
        {
            var ncreature = body as NEnvCreature;
            if(ncreature != null)
            {
                if (_CreatesInShape.Contains(ncreature))
                {
                    _CreatesInShape.Remove(ncreature);
                    _OnCreaturesInShapeChanged?.Invoke(CreaturesInShape);
                }
            }
        }

        protected virtual CollisionShape3D CreateShpae()
        {
            var shape = new CollisionShape3D();
            shape.Shape = new SphereShape3D { Radius = 10f };
            return shape;
        }
    }
}
