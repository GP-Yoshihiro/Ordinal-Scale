using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>敵の行動の段階（Q3）。</summary>
    public enum EnemyPhase
    {
        /// <summary>配置直後の待ち（いきなり襲わない）。</summary>
        Waiting,
        /// <summary>プレイヤーへ近づく（安全範囲の内側だけ）。</summary>
        Approaching,
        /// <summary>攻撃の予備動作。止まって予告する（この間に下がれば避けられる）。</summary>
        WindUp,
        /// <summary>攻撃の後の硬直。動かない。</summary>
        Recovering,
    }

    /// <summary>
    /// 敵の行動の調整値。**すべて仮の値で、Quest 実機では未確認**。安全範囲と距離は実機の場所に合わせて調整する（Q-6）。
    /// </summary>
    public sealed class EnemyBrainSettings
    {
        public EnemyBrainSettings(
            float moveSpeed = 0.5f,
            float attackRange = 1.2f,
            float strikeReach = 1.5f,
            float leashRadius = 1.0f,
            double windUpSeconds = 0.8,
            double recoverSeconds = 2.0,
            double startDelaySeconds = 1.5)
        {
            if (!(moveSpeed >= 0f)) throw new ArgumentOutOfRangeException(nameof(moveSpeed));
            if (!(attackRange > 0f)) throw new ArgumentOutOfRangeException(nameof(attackRange));
            if (!(strikeReach >= attackRange)) throw new ArgumentOutOfRangeException(nameof(strikeReach), strikeReach, "Must be at least attackRange.");
            if (!(leashRadius >= 0f)) throw new ArgumentOutOfRangeException(nameof(leashRadius));
            if (!(windUpSeconds >= 0)) throw new ArgumentOutOfRangeException(nameof(windUpSeconds));
            if (!(recoverSeconds >= 0)) throw new ArgumentOutOfRangeException(nameof(recoverSeconds));
            if (!(startDelaySeconds >= 0)) throw new ArgumentOutOfRangeException(nameof(startDelaySeconds));
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            StrikeReach = strikeReach;
            LeashRadius = leashRadius;
            WindUpSeconds = windUpSeconds;
            RecoverSeconds = recoverSeconds;
            StartDelaySeconds = startDelaySeconds;
        }

        /// <summary>近づく速さ（m/s）。</summary>
        public float MoveSpeed { get; }

        /// <summary>この水平距離（m）まで近づいたら止まって予備動作に入る。これより近くへは自分から寄らない。</summary>
        public float AttackRange { get; }

        /// <summary>攻撃の瞬間にプレイヤーの頭がこの水平距離（m）以内なら命中。予備動作中に下がれば避けられる。</summary>
        public float StrikeReach { get; }

        /// <summary>安全範囲：配置した位置からこの半径（m）の外へは出ない。プレイヤーが離れても部屋の中を追いかけない。</summary>
        public float LeashRadius { get; }

        public double WindUpSeconds { get; }
        public double RecoverSeconds { get; }
        public double StartDelaySeconds { get; }

        public static EnemyBrainSettings Default { get; } = new EnemyBrainSettings();

        public override string ToString()
        {
            return $"speed={MoveSpeed:0.##}m/s range={AttackRange:0.##}m reach={StrikeReach:0.##}m leash={LeashRadius:0.##}m " +
                   $"windup={WindUpSeconds:0.##}s recover={RecoverSeconds:0.##}s start={StartDelaySeconds:0.##}s";
        }
    }

    /// <summary>1フレーム分の敵の行動の結果。</summary>
    public readonly struct EnemyStep
    {
        public float X { get; }
        public float Z { get; }
        public EnemyPhase Phase { get; }

        /// <summary>このフレームで予備動作に入った（予告の表示・攻撃アニメの開始）。</summary>
        public bool WindUpStarted { get; }

        /// <summary>このフレームで攻撃し、プレイヤーに当たった。</summary>
        public bool StrikeLanded { get; }

        /// <summary>このフレームで攻撃したが、プレイヤーが届かない位置にいた（避けた）。</summary>
        public bool StrikeMissed { get; }

        public EnemyStep(float x, float z, EnemyPhase phase, bool windUpStarted, bool strikeLanded, bool strikeMissed)
        {
            X = x;
            Z = z;
            Phase = phase;
            WindUpStarted = windUpStarted;
            StrikeLanded = strikeLanded;
            StrikeMissed = strikeMissed;
        }
    }

    /// <summary>
    /// 敵1体の移動と反撃（Q3、仕様 Q-3）。水平面（X・Z）だけを扱う純粋な状態機械。
    /// 待ち → 接近（攻撃距離まで・安全範囲の内側だけ）→ 予備動作（止まる）→ 攻撃（その瞬間に届けば命中）→ 硬直 → 接近 … を繰り返す。
    /// 戦闘の勝敗（HP）は BattleSession が持ち、ここは「攻撃が当たったか」だけを返す。決着後は呼び出し側が Step を止める。
    /// </summary>
    public sealed class EnemyBrain
    {
        /// <summary>1回の Step で進める時間の上限（秒）。処理落ちや一時停止の後に大きく動かないようにする。</summary>
        public const double MaxStepSeconds = 0.1;

        private float _spawnX;
        private float _spawnZ;
        private float _x;
        private float _z;
        private double _timer;

        public EnemyBrain(EnemyBrainSettings settings = null)
        {
            Settings = settings ?? EnemyBrainSettings.Default;
        }

        public EnemyBrainSettings Settings { get; }
        public EnemyPhase Phase { get; private set; } = EnemyPhase.Waiting;
        public float X => _x;
        public float Z => _z;

        /// <summary>今まで行った攻撃の回数（命中・回避の両方）。</summary>
        public int Strikes { get; private set; }

        /// <summary>配置した位置から始め直す（配置・再挑戦のとき）。</summary>
        public void Reset(float spawnX, float spawnZ)
        {
            _spawnX = spawnX;
            _spawnZ = spawnZ;
            _x = spawnX;
            _z = spawnZ;
            Phase = EnemyPhase.Waiting;
            _timer = Settings.StartDelaySeconds;
            Strikes = 0;
        }

        /// <summary>時間を dt 秒進める。playerX/Z はプレイヤーの頭の水平位置。</summary>
        public EnemyStep Step(double dt, float playerX, float playerZ)
        {
            if (!(dt > 0)) return new EnemyStep(_x, _z, Phase, false, false, false);
            if (dt > MaxStepSeconds) dt = MaxStepSeconds;

            var windUpStarted = false;
            var landed = false;
            var missed = false;

            switch (Phase)
            {
                case EnemyPhase.Waiting:
                    _timer -= dt;
                    if (_timer <= 0) Phase = EnemyPhase.Approaching;
                    break;

                case EnemyPhase.Approaching:
                    if (Distance(_x, _z, playerX, playerZ) <= Settings.AttackRange + 1e-4f)
                    {
                        Phase = EnemyPhase.WindUp;
                        _timer = Settings.WindUpSeconds;
                        windUpStarted = true;
                    }
                    else
                    {
                        MoveToward(playerX, playerZ, dt);
                    }

                    break;

                case EnemyPhase.WindUp:
                    _timer -= dt;
                    if (_timer <= 0)
                    {
                        Strikes++;
                        if (Distance(_x, _z, playerX, playerZ) <= Settings.StrikeReach) landed = true;
                        else missed = true;
                        Phase = EnemyPhase.Recovering;
                        _timer = Settings.RecoverSeconds;
                    }

                    break;

                case EnemyPhase.Recovering:
                    _timer -= dt;
                    if (_timer <= 0) Phase = EnemyPhase.Approaching;
                    break;
            }

            return new EnemyStep(_x, _z, Phase, windUpStarted, landed, missed);
        }

        private void MoveToward(float playerX, float playerZ, double dt)
        {
            var dx = playerX - _x;
            var dz = playerZ - _z;
            var d = (float)Math.Sqrt(dx * dx + dz * dz);
            if (d < 1e-5f) return;

            // 攻撃距離より内側へは寄らない
            var step = (float)Math.Min(Settings.MoveSpeed * dt, d - Settings.AttackRange);
            if (step <= 0f) return;
            var nx = _x + dx / d * step;
            var nz = _z + dz / d * step;

            // 安全範囲（配置位置からの半径）の外へは出ない
            var ox = nx - _spawnX;
            var oz = nz - _spawnZ;
            var o = (float)Math.Sqrt(ox * ox + oz * oz);
            if (o > Settings.LeashRadius)
            {
                var k = Settings.LeashRadius / o;
                nx = _spawnX + ox * k;
                nz = _spawnZ + oz * k;
            }

            _x = nx;
            _z = nz;
        }

        private static float Distance(float ax, float az, float bx, float bz)
        {
            var dx = bx - ax;
            var dz = bz - az;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
