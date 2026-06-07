
using Godot;

namespace EGame
{
    public static class UserDataPathProvider
    {
        public static string GetProfileScopedBasePath(int profile_id)
        {
            var platform_dir = GetPlatformDirectoryName();
            var profile_dir = GetProfilerDir(profile_id);
            var player_id = "test_player";

            return $"user://{platform_dir}/{player_id}/{profile_dir}";
        }

        public static string GetAccountScopedBasePath(string data_type)
        {
            var platform_dir = GetPlatformDirectoryName();
            var player_id = "test_player";

            if (data_type != null)
                return $"user://{platform_dir}/{player_id}/{data_type}";
            return $"user://{platform_dir}/{player_id}";
        }

        private static string GetPlatformDirectoryName()
        {
            return OS.HasFeature("editor") ? "editor" : "default";
        }

        private static string GetProfilerDir(int profile_id)
        {
            return $"profile{profile_id}";
        }
    }
}