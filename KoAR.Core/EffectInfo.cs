﻿using System;

namespace KoAR.Core
{
    /// <summary>
    /// Attribute Information
    /// </summary>
    public class EffectInfo
    {
        /// <summary>
        /// Attribute ID
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Attribute Description
        /// </summary>
        public string DisplayText { get; set; }

        public EffectInfo Clone() => (EffectInfo)this.MemberwiseClone();
    }
}
