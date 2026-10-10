using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>
    /// Editor の代替入力（条件 C7）：マウスのドラッグから刃の位置を作る。座標はカメラから見た局所座標（m、+Z が前、+Y が上）。
    ///
    /// - ドラッグを始めた点の刃先を startTip とし、以後は「マウスの移動量（画面の高さで割った値）× MetersPerScreenHeight」だけ
    ///   刃先をカメラの正面の平面（Z = startTip.Z）上で動かす。したがって **刃先の速さ(m/s) = マウスの速さ(画面高/秒) × MetersPerScreenHeight**。
    /// - 刃は肩の位置（Shoulder）から刃先へ向かう長さ BladeLength の線分。手元は刃先から肩の方向へ BladeLength 戻った点。
    /// - ドラッグしていない間は刃が無い（ISwordPoseSource は追跡外を返す）。
    ///
    /// 換算係数・肩の位置・刃の長さはすべて Editor 検証用の仮の値で、Quest の操作感とは別物。
    /// </summary>
    public sealed class DragBladeModel
    {
        private float _startTipX;
        private float _startTipY;
        private float _tipZ;
        private float _startMouseX;
        private float _startMouseY;
        private float _tipX;
        private float _tipY;

        public DragBladeModel(float metersPerScreenHeight = 1f, float bladeLength = 0.9f,
            float shoulderX = 0.2f, float shoulderY = -0.35f, float shoulderZ = 0.1f)
        {
            if (!(metersPerScreenHeight > 0f)) throw new ArgumentOutOfRangeException(nameof(metersPerScreenHeight));
            if (!(bladeLength > 0f)) throw new ArgumentOutOfRangeException(nameof(bladeLength));
            MetersPerScreenHeight = metersPerScreenHeight;
            BladeLength = bladeLength;
            ShoulderX = shoulderX;
            ShoulderY = shoulderY;
            ShoulderZ = shoulderZ;
        }

        public float MetersPerScreenHeight { get; }
        public float BladeLength { get; }
        public float ShoulderX { get; }
        public float ShoulderY { get; }
        public float ShoulderZ { get; }

        public bool IsDragging { get; private set; }

        /// <summary>マウスの速さ（ピクセル/秒）を刃先の速さ（m/s）に換算する。画面の高さが分からなければ0。</summary>
        public static float TipSpeedFromMouse(float pixelsPerSecond, float screenHeightPixels, float metersPerScreenHeight)
        {
            if (!(screenHeightPixels > 0f)) return 0f;
            return pixelsPerSecond / screenHeightPixels * metersPerScreenHeight;
        }

        /// <summary>ドラッグ開始。startTip はマウスの下にある、カメラ正面の平面上の点（局所座標）。</summary>
        public void Begin(float startTipX, float startTipY, float startTipZ, float mouseX, float mouseY)
        {
            _startTipX = startTipX;
            _startTipY = startTipY;
            _tipZ = startTipZ;
            _startMouseX = mouseX;
            _startMouseY = mouseY;
            _tipX = startTipX;
            _tipY = startTipY;
            IsDragging = true;
        }

        /// <summary>ドラッグ中のマウス位置（ピクセル）を渡す。開始点からの差で刃先を決めるので、誤差は積み重ならない。</summary>
        public void Move(float mouseX, float mouseY, float screenHeightPixels)
        {
            if (!IsDragging || !(screenHeightPixels > 0f)) return;
            var scale = MetersPerScreenHeight / screenHeightPixels;
            _tipX = _startTipX + (mouseX - _startMouseX) * scale;
            _tipY = _startTipY + (mouseY - _startMouseY) * scale;
        }

        public void End()
        {
            IsDragging = false;
        }

        /// <summary>現在の刃（局所座標の手元と刃先）。ドラッグしていなければ false。</summary>
        public bool TryGetLocalBlade(out float hiltX, out float hiltY, out float hiltZ, out float tipX, out float tipY, out float tipZ)
        {
            tipX = _tipX;
            tipY = _tipY;
            tipZ = _tipZ;
            if (!IsDragging)
            {
                hiltX = hiltY = hiltZ = 0f;
                return false;
            }

            var dx = tipX - ShoulderX;
            var dy = tipY - ShoulderY;
            var dz = tipZ - ShoulderZ;
            var length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (length < 1e-4f)
            {
                // 刃先が肩に重なる（通常は起きない）。前向きの刃にする
                dx = 0f;
                dy = 0f;
                dz = 1f;
                length = 1f;
            }

            hiltX = tipX - dx / length * BladeLength;
            hiltY = tipY - dy / length * BladeLength;
            hiltZ = tipZ - dz / length * BladeLength;
            return true;
        }
    }
}
