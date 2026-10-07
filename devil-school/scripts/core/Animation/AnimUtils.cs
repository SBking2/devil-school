
using Godot;

namespace EGame
{
    public static class AnimUtils
    {
        /// <summary>
        /// 取震动点，并让相机插值
        /// </summary>
        public static Tween ShakeRotation(Node3D obj, float duration, Vector3 strength, float vibrato, float randomness)
        {
            //最少震两次
            int total_shake_point = (int)(vibrato * duration);
            if (total_shake_point < 2)
                total_shake_point = 2;

            //越后面的震动点时间越长
            float sum = 0f;
            for (int i = 0; i < total_shake_point; i++)
                sum += i + 1;

            //对时间归一化，保证总时间等于duration
            float[] durations = new float[total_shake_point];   //记录下第i到第i+1个点的时间;
            float[] starts = new float[total_shake_point];
            float accumulated = 0f;
            for (int i = 0; i < total_shake_point; i++)
            {
                durations[i] = (i + 1) * duration / sum;
                starts[i] = accumulated;
                accumulated += durations[i];
            }

            Vector3[] rotations = new Vector3[total_shake_point + 1];
            //设置一个初始的随机角度
            float angle = (float)GD.RandRange(-180.0, 180.0);
            float intensity = strength.Length();
            float declay_per = intensity / total_shake_point;   //这个点减弱的强度

            for (int i = 1; i < total_shake_point; i++)
            {
                if (i > 1)
                    angle = angle - 180f + (float)GD.RandRange(-randomness, randomness);

                //在xoy平面上构建一个角度为angle的向量，长度为intensity
                float x = Mathf.Cos(Mathf.DegToRad(angle)) * intensity;
                float y = Mathf.Sin(Mathf.DegToRad(angle)) * intensity;

                Vector3 vec = new Vector3(x, y, 0f);
                //让此向量在y轴做一些随机旋转
                float random = (float)GD.RandRange(-randomness, randomness);
                vec = vec.Rotated(Vector3.Up, Mathf.DegToRad(random));
                vec.X = vec.LimitLength(strength.X).X;
                vec.Y = vec.LimitLength(strength.Y).Y;
                vec.Z = vec.LimitLength(strength.Z).Z;

                rotations[i] = vec;

                intensity -= declay_per;
            }

            Quaternion last_rotation = Quaternion.Identity;

            //构筑tween
            Tween tween = obj.CreateTween();
            tween.TweenMethod(Callable.From<float>((elapsed) =>
            {
                int index = 0;
                while (index < total_shake_point - 1 && elapsed >= starts[index + 1])
                    index++;

                float t = Mathf.Clamp((elapsed - starts[index]) / durations[index], 0f, 1f);
                float eased = 1f - (1f - t) * (1f - t);

                Vector3 degrees = rotations[index].Lerp(rotations[index + 1], eased);
                Quaternion rotation = Quaternion.FromEuler(degrees * Mathf.DegToRad(1f));
                
                obj.Quaternion = obj.Quaternion * last_rotation.Inverse() * rotation;
                last_rotation = rotation;

            }), 0f, duration, duration);

            return tween;
        }
    }
}
