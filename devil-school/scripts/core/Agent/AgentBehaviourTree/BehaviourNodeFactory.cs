
using System;
using System.Collections.Generic;

namespace EGame
{
    // 行为树节点的创建入口：构建树时用 Create<T>() 代替 new，子类 Model 用 Replace<旧, 新>() 声明替换，不用重写整棵树的构建
    public class BehaviourNodeFactory
    {
        private readonly Dictionary<Type, Type> _Replacements = new Dictionary<Type, Type>();

        // 之后所有 Create<TOld>() 都会创建 TNew。同一个 TOld 重复声明，后声明的生效
        public void Replace<TOld, TNew>() where TOld : AbstractAgentBehaviourNode where TNew : TOld
        {
            if (typeof(TOld) == typeof(TNew))
                throw new ArgumentException($"Cannot replace {typeof(TOld).Name} with itself!");

            _Replacements[typeof(TOld)] = typeof(TNew);
        }

        // args 是构造函数参数，替换后的类必须有能接收这些参数的构造函数
        public T Create<T>(params object[] args) where T : AbstractAgentBehaviourNode
        {
            return (T)Activator.CreateInstance(Resolve(typeof(T)), args);
        }

        // 链式替换：A 被 B 替换、B 又被 C 替换，要 A 就拿到 C
        private Type Resolve(Type type)
        {
            while (_Replacements.TryGetValue(type, out Type replacement))
                type = replacement;
            return type;
        }
    }
}
