/**
BSD 2-Clause License

Copyright (c) 2026, Kyle Givler

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 **/

using BetterTradeColors.Settings;
using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace BetterTradeColors
{
    [StaticConstructorOnStartup]
    public class BetterTradeColorsMod : Mod
    {
        public static BetterTradeColorsSettings Settings;

        public BetterTradeColorsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<BetterTradeColorsSettings>();
            var harmony = new Harmony("com.kylegivler.bettertradecolors");

            MethodInfo targetMethod = AccessTools.Method(typeof(GenLabel), "NewThingLabel", new Type[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) });

            if (targetMethod != null)
            {
                var postfixMethod = new HarmonyMethod(typeof(Patch_GenLabel_NewThingLabel), nameof(Patch_GenLabel_NewThingLabel.Postfix));
                harmony.Patch(targetMethod, postfix: postfixMethod);
            }
            else
            {
                Log.Error("[BetterTradeColors] Failed to find target method GenLabel.NewThingLabel. Colorization disabled.");
            }
        }

        public override string SettingsCategory()
        {
            return "Better Trade Colors";
        }
    }
}
