
using Godot;
using System;
using System.Collections.Generic;

namespace EGame
{
    public abstract class EGSpineBinding
    {
        private GodotObject _SpineObject;
        protected abstract string SpineClassName { get; }
        protected virtual IEnumerable<string> MethodNames => Array.Empty<string>();
        protected virtual IEnumerable<string> SignalNames => Array.Empty<string>();

        public EGSpineBinding(Variant obj)
        {
            if(obj.VariantType != Variant.Type.Object)
            {
                throw new InvalidOperationException($"Expected GodotObject type but was {obj.VariantType}");
            }

            _SpineObject = obj.AsGodotObject();
            Validate();
        }

        /// <summary>
        /// 验证当前传进来的obj是否有问题
        /// </summary>
        private void Validate()
        {
            if (_SpineObject == null)
                throw new ArgumentNullException("The SpineObject is null");

            if (_SpineObject.GetClass() != SpineClassName)
                throw new InvalidOperationException($"Expected {SpineClassName} but was {_SpineObject.GetClass()}");

            foreach(var method_name in MethodNames)
            {
                if (_SpineObject.HasMethod(method_name) == false)
                    throw new InvalidOperationException($"Class {SpineClassName} doesn't have the method which name is {method_name}");
            }

            foreach (var signal_name in SignalNames)
            {
                if (_SpineObject.HasSignal(signal_name) == false)
                    throw new InvalidOperationException($"Class {SpineClassName} doesn't have the signal which name is {signal_name}");
            }
        }

        public Variant? Call(string name, params Variant[] args)
        {
            var result = _SpineObject.Call(name, args);
            if (result.VariantType == Variant.Type.Nil)
                return null;
            return result;
        }

        public Error Connect(string signal, Callable callback)
        {
            return _SpineObject.Connect(signal, callback);
        }

        public void Disconnect(string signal, Callable callback)
        {
            _SpineObject.Disconnect(signal, callback);
        }
    }
}