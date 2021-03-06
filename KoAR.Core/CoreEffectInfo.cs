﻿using System;

namespace KoAR.Core
{
    public sealed class CoreEffectInfo : IEffectInfo, IEquatable<CoreEffectInfo>
    {
        public CoreEffectInfo(uint code, DamageType damageType, float tier = 0f) => (Code, DamageType, Tier) = (code, damageType, tier);
         
        public uint Code { get; }
        public DamageType DamageType { get; }
        public float Tier { get; }
        public string DisplayText => DamageType == DamageType.Unknown ? "Unknown" : $"{DamageType} (Tier: {Tier})";

        public CoreEffectInfo Clone() => (CoreEffectInfo)MemberwiseClone();
        public bool Equals(CoreEffectInfo? other) => other?.Code == Code;
        public override bool Equals(object obj) => Equals(obj as CoreEffectInfo);
        public override int GetHashCode() => Code.GetHashCode();
    }
}
