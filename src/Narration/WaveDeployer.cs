using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The battle's wave deployer, whichever one this battle uses. The game
    /// ships three unrelated deployer components — the plain WaveDeploySystem,
    /// the WaveDeploySystemOverflow the current version actually puts in the
    /// Battle scene, and a no-limit variant — with no shared base class or
    /// interface between them, only identically named methods and fields.
    /// Exactly one is ever in the scene, so anything asking about waves has to
    /// try all three, and anything patching the deploy has to patch all three
    /// or it fires in no battle at all.
    /// </summary>
    internal static class WaveDeployer
    {
        /// <summary>Every deployer component type, most likely first.</summary>
        internal static readonly Type[] SystemTypes =
        {
            typeof(WaveDeploySystemOverflow),
            typeof(WaveDeploySystem),
            typeof(WaveDeploySystemNoLimit),
        };

        /// <summary>The deployer running this battle, or null outside one.</summary>
        internal static Component Find()
        {
            foreach (Type type in SystemTypes)
            {
                if (UnityEngine.Object.FindObjectOfType(type) is Component system)
                    return system;
            }
            return null;
        }

        /// <summary>Turns until the next wave lands; 0 when nothing is counting down.</summary>
        internal static int GetCounter(Component system)
        {
            return system != null ? ReflectionUtil.GetIntField(system, "counter", 0) : 0;
        }

        /// <summary>Index of the next wave to arrive — equivalently, how many have already landed.</summary>
        internal static int GetCurrentWave(Component system)
        {
            return system != null ? ReflectionUtil.GetIntField(system, "currentWave", 0) : 0;
        }

        /// <summary>
        /// The battle's waves. The overflow deployer works off its own copy of
        /// the list and appends to it — units that could not fit come back as
        /// extra waves — so that copy is the truthful one; the others read the
        /// wave manager on the enemy.
        /// </summary>
        internal static List<BattleWaveManager.Wave> GetWaves(Component system)
        {
            if (system is WaveDeploySystemOverflow)
            {
                var own = ReflectionUtil.GetField<List<BattleWaveManager.Wave>>(system, "waves");
                if (own != null)
                    return own;
            }

            var enemy = Battle.instance?.enemy;
            return enemy != null ? enemy.GetComponent<BattleWaveManager>()?.list : null;
        }
    }
}
