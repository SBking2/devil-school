
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EGame
{
    // 音效管理：缓存 AudioStream + 播放器对象池，异步加载资源。UI 音效走 Play（非定位），场景里的音效走 Play3D（定位）
    public class SoundManager
    {
        public static SoundManager Instance { get; } = new SoundManager();

        private Node _Root;
        private Dictionary<string, AudioStream> _Cache = new Dictionary<string, AudioStream>();
        private List<AudioStreamPlayer> _PlayerPool = new List<AudioStreamPlayer>();
        private List<AudioStreamPlayer3D> _PlayerPool3D = new List<AudioStreamPlayer3D>();

        public void Init(Node root)
        {
            _Root = root;
        }

        public void Play(string path)
        {
            var ui_audio_path = "res://audios/ui_audios/" + path + ".mp3";
            TaskHelper.RunSafely(PlayAsync(ui_audio_path));
        }

        public void Play3D(string path, Vector3 position)
        {
            var world_audio_path = "res://audios/world_audios/" + path + ".mp3";
            TaskHelper.RunSafely(Play3DAsync(world_audio_path, position));
        }

        private async Task PlayAsync(string path)
        {
            var stream = await GetOrLoadAsync(path);
            if (stream == null)
                return;

            var player = GetFreePlayer();
            player.Stream = stream;
            player.VolumeLinear = UserData.Instance.GetFloat("volume", 1.0f);
            player.Play();
        }

        private async Task Play3DAsync(string path, Vector3 position)
        {
            var stream = await GetOrLoadAsync(path);
            if (stream == null)
                return;

            var player = GetFreePlayer3D();
            player.GlobalPosition = position;
            player.Stream = stream;
            player.VolumeLinear = UserData.Instance.GetFloat("volume", 1.0f);
            player.Play();
        }

        private async Task<AudioStream> GetOrLoadAsync(string path)
        {
            if (_Cache.TryGetValue(path, out var cached))
            {
                // 已经加载完的直接返回；还是 null 说明有别的调用正在加载这个 path，往下走轮询等它
                if (cached != null)
                    return cached;
            }
            else
            {
                // 第一次遇到这个 path，先占位，避免同一帧内别的调用重复发起加载请求
                _Cache[path] = null;
                ResourceLoader.LoadThreadedRequest(path);
            }

            ResourceLoader.ThreadLoadStatus status;
            while ((status = ResourceLoader.LoadThreadedGetStatus(path)) == ResourceLoader.ThreadLoadStatus.InProgress)
                await _Root.ToSignal(_Root.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (status != ResourceLoader.ThreadLoadStatus.Loaded)
            {
                Log.Warn($"SoundManager 加载音频失败: {path}", type: Log.LogType.Generic);
                _Cache.Remove(path);
                return null;
            }

            var stream = (AudioStream)ResourceLoader.LoadThreadedGet(path);
            _Cache[path] = stream;
            return stream;
        }

        // 找一个空闲的播放器，没有就新建一个加进池子，池子不设上限（音效数量少，够用）
        private AudioStreamPlayer GetFreePlayer()
        {
            foreach (var player in _PlayerPool)
            {
                if (!player.Playing)
                    return player;
            }

            var new_player = new AudioStreamPlayer();
            _Root.AddChild(new_player);
            _PlayerPool.Add(new_player);
            return new_player;
        }

        private AudioStreamPlayer3D GetFreePlayer3D()
        {
            foreach (var player in _PlayerPool3D)
            {
                if (!player.Playing)
                    return player;
            }

            var new_player = new AudioStreamPlayer3D();
            _Root.AddChild(new_player);
            _PlayerPool3D.Add(new_player);
            return new_player;
        }

        // 清空缓存的资源引用，具体什么时候调（切场景/内存紧张）先留着以后再接
        public void ClearCache()
        {
            _Cache.Clear();
        }
    }
}
