using System.IO;
using GameConfig;
using NUnit.Framework;
using PFGAS.Generated;
using PFGAS.Runtime;
using UnityEngine;

namespace PFGAS
{
    [SetUpFixture]
    public sealed class PFGASTagTestBootstrap
    {
        [OneTimeSetUp]
        public void RegisterTags()
        {
            Tables.SetJsonLoader(new AssetRawJsonLoader());
            PFGASTagGenerated.RegisterFromLubanTable();
        }

        [OneTimeTearDown]
        public void ClearTags()
        {
            TagHelper.Clear();
            Tables.SetJsonLoader(null);
        }

        private sealed class AssetRawJsonLoader : IJsonTableLoader
        {
            public string LoadJson(string fileName)
            {
                var path = Path.Combine(
                    Application.dataPath,
                    "AssetRaw",
                    "Configs",
                    "json",
                    fileName + ".json");
                return File.ReadAllText(path);
            }
        }
    }
}

namespace PFGAS.Runtime.Tests
{
    internal static class PFGASTestTagIds
    {
        public static readonly PFTagId State = new PFTagId(0);
        public static readonly PFTagId State_Buff = new PFTagId(1);
        public static readonly PFTagId State_DeBuff = new PFTagId(2);
        public static readonly PFTagId State_DeBuff_Du = new PFTagId(5);
        public static readonly PFTagId State_DeBuff_Fire = new PFTagId(6);
        public static readonly PFTagId State_DeBuff_Ice = new PFTagId(7);
        public static readonly PFTagId Life = new PFTagId(3);
        public static readonly PFTagId Life_MP = new PFTagId(8);
        public static readonly PFTagId Life_HP = new PFTagId(4);
    }
}
