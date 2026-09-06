
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public class UIManager
    {
        public static UIManager Instance { get; } = new UIManager();

        private Log.Logger _Logger = new Log.Logger(Log.LogType.Generic);
        private Node _Root;
        private Dictionary<UIPanelType, NAbstractPanel> _Panels = new Dictionary<UIPanelType, NAbstractPanel>();

        public void Init(Node root)
        {
            _Root = root;
        }

        public NAbstractPanel Show(UIPanelType type)
        {
            var panel = GetOrLoad(type);
            if (panel == null)
                return null;

            panel.Visible = true;
            panel.OnShow();
            return panel;
        }

        public void Hide(UIPanelType type)
        {
            if (_Panels.TryGetValue(type, out var panel))
            {
                panel.Visible = false;
                panel.OnHide();
            }
        }

        public void Close(UIPanelType type)
        {
            if (_Panels.Remove(type, out var panel))
            {
                panel.OnDestry();
                panel.QueueFree();
            }
        }

        public NAbstractPanel Get(UIPanelType type)
        {
            _Panels.TryGetValue(type, out var panel);
            return panel;
        }

        // 没加载过就用SceneHelper加载并挂到根节点上，加载过就直接复用
        private NAbstractPanel GetOrLoad(UIPanelType type)
        {
            if (_Panels.TryGetValue(type, out var panel))
                return panel;

            if (_Root == null)
            {
                _Logger.Error($"UIManager未Init就尝试打开面板 {type}");
                return null;
            }

            panel = SceneHelper.LoadScene<NAbstractPanel>(GetScenePath(type));
            _Root.AddChild(panel);
            panel.OnInit();
            _Panels[type] = panel;
            return panel;
        }

        private string GetScenePath(UIPanelType type)
        {
            return "ui/" + type.ToString().Slugify().ToLowerInvariant();
        }
    }
}
