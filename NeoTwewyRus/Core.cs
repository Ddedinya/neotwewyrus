using Newtonsoft.Json;
using MelonLoader;
using UnityEngine;
using Il2CppTMPro;
using System.Drawing;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using HarmonyLib;
using Il2CppUI.Utility;
using Il2CppUI;
using UnityEngine.U2D;
using Il2Cpp;

[assembly: MelonInfo(typeof(NeoTwewyRus.Core), "NeoTwewyRus", "0.0.1", "Ddedinya", null)]
[assembly: MelonGame("SQUARE ENIX", "NEO: The World Ends with You")]

namespace NeoTwewyRus
{
    public class Core : MelonMod
    {
        static Dictionary<string, TMP_FontAsset> _fonts = new Dictionary<string, TMP_FontAsset>() // Создаём отдельный словарь для шрифтов, чтобы не пришлось их постоянно создавать
        {
            { "FOT-NewRodinProN-B SDF", null },
            { "FOT-CARATSTD-UB SDF", null },
            { "FOT-CometStd-B SDF", null },
            { "FOT-ComicReggaeStd-B SDF", null },
            { "FOT-GOSPELSTD-EB SDF", null }
        };

        public static TMP_FontAsset CreateNewFont(string name)
        {
            try
            {
                MelonLogger.Msg($"Mods/NeoTwewyRus/Fonts/{name} Atlas.png");
                var texture = LoadTexture($"Mods/NeoTwewyRus/Fonts/{name} Atlas.png"); // Загрузка атласа шрифта
                if (texture == null)
                {
                    MelonLogger.Error($"Не удалось загрузить атлас шрифта {name}.");
                    return null;
                }

                var shader = Shader.Find("TextMeshPro/Distance Field");
                if (shader == null)
                {
                    MelonLogger.Error("Не удалось загрузить шейдер.");
                    return null;
                }

                Material material = new Material(shader)
                {
                    mainTexture = texture,
                    name = $"{name} (New) Material"
                };

                // Обводка
                material.EnableKeyword("OUTLINE_ON");

                // Создание объекта TMP шрифта и загрузка в него кучи инфы. 
                var fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
                fontAsset.name = $"{name} (New)";
                fontAsset.material = material;
                fontAsset.atlas = texture;
                fontAsset.atlasTextures = new Texture2D[] { texture };
                var jsonData = JsonConvert.DeserializeObject<FontData>(File.ReadAllText($"Mods/NeoTwewyRus/Fonts/{name}.json"));

                if (jsonData == null)
                {
                    MelonLogger.Error($"Не удалось загрузить данные шрифта.");
                    return null;
                }

                fontAsset.faceInfo = new FaceInfo
                {
                    familyName = jsonData.m_FaceInfo.m_FamilyName,
                    styleName = jsonData.m_FaceInfo.m_StyleName,
                    pointSize = (int)jsonData.m_FaceInfo.m_PointSize,
                    scale = jsonData.m_FaceInfo.m_Scale,
                    lineHeight = jsonData.m_FaceInfo.m_LineHeight,
                    ascentLine = jsonData.m_FaceInfo.m_AscentLine,
                    capLine = jsonData.m_FaceInfo.m_CapLine,
                    meanLine = jsonData.m_FaceInfo.m_MeanLine,
                    baseline = jsonData.m_FaceInfo.m_Baseline,
                    descentLine = jsonData.m_FaceInfo.m_DescentLine,
                    superscriptOffset = jsonData.m_FaceInfo.m_SuperscriptOffset,
                    subscriptOffset = jsonData.m_FaceInfo.m_SubscriptOffset,
                    underlineOffset = jsonData.m_FaceInfo.m_UnderlineOffset,
                    underlineThickness = jsonData.m_FaceInfo.m_UnderlineThickness,
                    strikethroughOffset = jsonData.m_FaceInfo.m_StrikethroughOffset,
                    strikethroughThickness = jsonData.m_FaceInfo.m_StrikethroughThickness,
                    tabWidth = jsonData.m_FaceInfo.m_TabWidth
                };

                fontAsset.glyphTable = new Il2CppSystem.Collections.Generic.List<Glyph>();
                foreach (var g in jsonData.m_GlyphTable)
                {
                    var glyph = new Glyph(
                        (uint)g.m_Index,
                        new GlyphMetrics(
                            g.m_Metrics.m_Width,
                            g.m_Metrics.m_Height,
                            g.m_Metrics.m_HorizontalBearingX,
                            g.m_Metrics.m_HorizontalBearingY,
                            g.m_Metrics.m_HorizontalAdvance
                        ),
                        new GlyphRect(
                            g.m_GlyphRect.m_X,
                            g.m_GlyphRect.m_Y,
                            g.m_GlyphRect.m_Width,
                            g.m_GlyphRect.m_Height
                        ),
                        1.0f,
                        0
                    );
                    fontAsset.glyphTable.Add(glyph);
                }

                fontAsset.characterTable = new Il2CppSystem.Collections.Generic.List<TMP_Character>();
                foreach (var c in jsonData.m_CharacterTable)
                {
                    Glyph glyph = null;
                    foreach (var g in fontAsset.glyphTable)
                    {
                        if (g.index == (uint)c.m_GlyphIndex)
                        {
                            glyph = g;
                            break;
                        }
                    }

                    if (glyph != null)
                    {
                        fontAsset.characterTable.Add(new TMP_Character(
                            (uint)c.m_Unicode,
                            glyph
                        ));
                    }
                }

                if (fontAsset.glyphTable == null || fontAsset.characterTable == null || fontAsset.material == null)
                {
                    MelonLogger.Error($"Некоторые важные данные шрифта не были инициализированы.");
                    return null;
                }

                fontAsset.atlasWidth = jsonData.m_AtlasWidth;
                fontAsset.atlasHeight = jsonData.m_AtlasHeight;
                fontAsset.atlasPadding = jsonData.m_AtlasPadding;
                fontAsset.atlasRenderMode = (GlyphRenderMode)jsonData.m_AtlasRenderMode;
                fontAsset.InitializeDictionaryLookupTables();

                return fontAsset; 
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Ошибка загрузки шрифта {name}: {ex.Message}");
                return null;
            }
        }

        public static Texture2D LoadTexture(string path) // Функция загрузки текстуры в игру. Довольно медлительная
        {
            try
            {
                using (var bitmap = new Bitmap(path))
                {
                    var texture = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.RGBA32, false);
                    var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    int byteCount = bitmapData.Stride * bitmap.Height;
                    byte[] pixelData = new byte[byteCount];
                    System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, byteCount);
                    bitmap.UnlockBits(bitmapData);
                    Color32[] pixels = new Color32[bitmap.Width * bitmap.Height];
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            int index = (y * bitmapData.Stride) + (x * 4);
                            byte b = pixelData[index];
                            byte g = pixelData[index + 1];
                            byte r = pixelData[index + 2];
                            byte a = pixelData[index + 3];
                            pixels[(bitmap.Height - y - 1) * bitmap.Width + x] = new Color32(r, g, b, a);
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    return texture;
                }
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Ошибка загрузки текстуры: {e.Message}");
                return null;
            }
        }

        public static JsonStructure LoadNewText(string fileName) // Функция для загрузки нового текстового файла в игру
        {
            try
            {
                string filePath = $"Mods/NeoTwewyRus/Text/{fileName}.txt"; // Путь до файла
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var newText = JsonConvert.DeserializeObject<JsonStructure>(json); // Преобразуем содержимое файла в JSON объект
                    return newText;
                }
                else { return null;}
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Ошибка при загрузке нового текстового файла {fileName}: {ex.Message}");
                return null;
            }
        }

        public static ConfigStructure LoadNewTextConfig(string fileName) // Функция для загрузки нового конфига диалоговых окон в игру
        {
            try
            {
                string filePath = $"Mods/NeoTwewyRus/Text/Config/{fileName}.json"; // Путь до файла
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var newText = JsonConvert.DeserializeObject<ConfigStructure>(json); // Преобразуем содержимое файла в JSON объект
                    return newText;
                }
                else { return null; }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Ошибка при загрузке нового конфига для {fileName}: {ex.Message}");
                return null;
            }
        }

        public static Sprite LoadNewSprite(string name, Vector2 pivot)
        {
            Texture2D texture = LoadTexture($"Mods/NeoTwewyRus/Textures/{name}.png");
            if (texture == null)
            {
                MelonLogger.Error("Не удалось загрузить новый спрайт!");
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot);
        }

        // это вроде не нужно, но на всякий случай оставлю
        /*
        [HarmonyPatch(typeof(UI109Base), "GetSpriteFromAtlas")]
        public class GetSpriteFromAtlasPatch
        {
            [HarmonyPostfix]
            public static void Postfix(UI109Base __instance, ref UnityEngine.Sprite __result, string key, string spriteName)
            {
                 __result = LoadNewSprite(__result.name, __result.pivot);
            }
        }*/

        // Патч грува - он какого-то хрена загружается как текстура
        [HarmonyPatch(typeof(UIImageLoaderPersonal), "GetTexture")]
        public class GetTexturePatch
        {
            [HarmonyPostfix]
            public static void Postfix(UIImageLoaderPersonal __instance, ref UnityEngine.Texture2D __result, Il2CppSystem.String key)
            {
                if (File.Exists($"Mods/NeoTwewyRus/Textures/{__result.name}.png"))
                {
                    Texture2D texture = LoadTexture($"Mods/NeoTwewyRus/Textures/{__result.name}.png");
                    if (texture == null)
                    {
                        MelonLogger.Error("Не удалось загрузить новую текстуру!");
                        return;
                    }

                    __result = texture;
                }
            }
        }

        // Патч некоторых надписей
        [HarmonyPatch(typeof(SpriteAtlas), "GetSprite")]
        public class SpriteAtlasPatch
        {
            [HarmonyPostfix]
            public static void Postfix(UI109Base __instance, ref UnityEngine.Sprite __result, string name)
            {
                if (File.Exists($"Mods/NeoTwewyRus/Textures/{__result.name}.png"))
                {
                    __result = LoadNewSprite(__result.name, __result.pivot);
                }
            }
        }

        // Патч некоторых элементов интерфейса
        [HarmonyPatch(typeof(UIImageLoaderPersonal), "GetSprite")]
        class GetSpritePatch
        {
            [HarmonyPostfix]
            public static void Postfix(UIImageLoaderPersonal __instance, ref Sprite __result, Il2CppSystem.String key, Vector2 pivot)
            {
                if (File.Exists($"Mods/NeoTwewyRus/Textures/{__result.name}.png"))
                {
                    __result = LoadNewSprite(__result.name, pivot);
                }
            }
        }

        // Фикс шрифта на иконках магазинов
        [HarmonyPatch(typeof(ShopNameObject), "GetShopName")]
        public static class GetShopNamePatch
        {
            static void Postfix(ShopNameObject __instance, ref GameObject __result)
            {
                __result.GetComponentInChildren<TextMeshProUGUI>().fontSharedMaterial.mainTexture = _fonts["FOT-NewRodinProN-B SDF"].atlasTexture;
            }
        }

        // Фикс шрифта на иконках магазинов
        [HarmonyPatch(typeof(ShopIconObject), "SetShow")]
        public static class SetShowPatch
        {
            static void Postfix(ShopIconObject __instance)
            {
                __instance.m_TitleText.fontSharedMaterial.mainTexture = _fonts["FOT-NewRodinProN-B SDF"].atlasTexture;
            }
        }

        // Фикс шрифта на иконках магазинов
        [HarmonyPatch(typeof(ShopIconObject), "OnScanMode")]
        public static class OnScanModePatch
        {
            static void Postfix(ShopIconObject __instance)
            {
                __instance.m_TitleText.fontSharedMaterial.mainTexture = _fonts["FOT-NewRodinProN-B SDF"].atlasTexture;
            }
        }

        // Фикс шрифта на иконках переходов между зонами
        [HarmonyPatch(typeof(FieldMapUIAreaSign), "OnLateUpdate")]
        public static class AreaNamePatch
        {
            [HarmonyPostfix]
            public static void Postfix(FieldMapUIAreaSign __instance)
            {
                __instance.m_NextAreaText.fontSharedMaterial.mainTexture = _fonts["FOT-NewRodinProN-B SDF"].atlasTexture;
            }
        }

        // Фикс шрифта во время боя
        [HarmonyPatch(typeof(DamageFont), "ShowFont")]
        public static class DamageFontPatch
        {
            static void Postfix(DamageFont __instance)
            {
                __instance.mTextMeshBlock.fontSharedMaterial.mainTexture = _fonts["FOT-CometStd-B SDF"].atlasTexture;
                __instance.mTextMeshScript.fontSharedMaterial.mainTexture = _fonts["FOT-CometStd-B SDF"].atlasTexture;
                __instance.mTextMeshWeak.fontSharedMaterial.mainTexture = _fonts["FOT-CometStd-B SDF"].atlasTexture;
            }
        }

        // Патч шрифта
        [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
        public static class FontPatch
        {
            [HarmonyPostfix]
            public static void Postfix(TextMeshProUGUI __instance)
            {
                if (_fonts.ContainsKey(__instance.font.name))
                {
                    if (_fonts[__instance.font.name] == null) { _fonts[__instance.font.name] = CreateNewFont(__instance.font.name); }
                    TMP_FontAsset newFont = _fonts[__instance.font.name];
                    Color32 outlineColor = __instance.outlineColor;
                    float outlineWidth = __instance.outlineWidth;
                    Material material;
                    if (__instance.name != "Text_name") { material = new Material(__instance.fontSharedMaterial); }
                    else { material = __instance.fontSharedMaterial; }
                    __instance.font = newFont;
                    __instance.fontSharedMaterial = material;
                    __instance.fontSharedMaterial.mainTexture = newFont.atlasTexture;
                    __instance.outlineColor = outlineColor;
                    __instance.outlineWidth = outlineWidth * 1.5f;
                }
            }
        }

        // Патч текста и конфига диалоговых окон
        [HarmonyPatch(typeof(TextAsset), "get_text")]
        private class TextPatch
        {
            static void Postfix(TextAsset __instance, ref string __result) 
            {
                try
                {
                    // Преобразуем содержимое файла в два JSON объекта. Так надо, ладно?
                    var textData = JsonConvert.DeserializeObject<JsonStructure>(__result); 
                    var confData = JsonConvert.DeserializeObject<ConfigStructure>(__result); 
                    bool changed = false;
                    bool config = false;
                    if (textData != null && textData.columns != null)
                    {
                        var patchedTextData = LoadNewText(__instance.name); // Получаем файл на замену
                        var patchedConfData = LoadNewTextConfig(__instance.name);
                        if (patchedTextData == null) { return; } // Нет файла - нет замены

                        for (int i = 0; i < textData.columns.Count; i++) // Цикл для перебора колон и замены контента в них
                        {
                            if (textData.columns[i].name == patchedTextData.columns[i].name)
                            {

                                // В N:TWEWY есть два файла с одинаковыми названиями для каждого дня.
                                // Один из них является техническим и если его изменить, то скрипты сломаются.
                                // В техническом файле нет атрибута "content", и благодаря этой проверки определяем, что и чем заменять.
                                if (!String.IsNullOrEmpty(textData.columns[i].content))
                                {
                                    textData.columns[i].speaker = patchedTextData.columns[i].speaker;
                                    textData.columns[i].listener = patchedTextData.columns[i].listener;
                                    textData.columns[i].content = patchedTextData.columns[i].content;
                                    changed = true;
                                }
                                else if (patchedConfData != null)
                                {
                                    confData.columns[i].config_speaker = patchedConfData.columns[i].config_speaker;
                                    confData.columns[i].config_frame = patchedConfData.columns[i].config_frame;
                                    confData.columns[i].log_speaker = patchedConfData.columns[i].log_speaker;
                                    confData.columns[i].log_setting = patchedConfData.columns[i].log_setting;
                                    confData.columns[i].voice = patchedConfData.columns[i].voice;
                                    confData.columns[i].logvoice = patchedConfData.columns[i].logvoice;
                                    confData.columns[i].config_offsetX = patchedConfData.columns[i].config_offsetX;
                                    confData.columns[i].config_offsetY = patchedConfData.columns[i].config_offsetY;
                                    confData.columns[i].config = patchedConfData.columns[i].config;
                                    confData.columns[i].config_font = patchedConfData.columns[i].config_font;
                                    confData.columns[i].config_fontsize = patchedConfData.columns[i].config_fontsize;
                                    confData.columns[i].config_width = patchedConfData.columns[i].config_width;
                                    confData.columns[i].config_height = patchedConfData.columns[i].config_height;
                                    changed = true;
                                    config = true;
                                }
                            }
                        }

                        if (changed) // Если были изменения, то преобразовываем обратно и заменяем файл
                        {
                            if (config)
                            {
                                __result = JsonConvert.SerializeObject(confData, Newtonsoft.Json.Formatting.Indented);
                            }
                            else
                            {
                                __result = JsonConvert.SerializeObject(textData, Newtonsoft.Json.Formatting.Indented);
                            }
                        }  
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Ошибка при попытке пропачить {__instance.name}: {ex.Message}");
                }
            }
        }

        // Классы JSON структур файлов с текстом, с конфигом диалоговых окон и инфы о шрифте.
        public class JsonStructure
        {
            public List<Column> columns { get; set; }
        }

        public class Column
        {
            public string name { get; set; }
            public string speaker { get; set; }
            public string listener { get; set; }
            public string content { get; set; }
        }

        public class ConfigStructure
        {
            public List<ConfigColumn> columns { get; set; }
        }

        public class ConfigColumn
        {
            public string name { get; set; }
            public string config_speaker { get; set; }
            public string config_frame { get; set; }
            public string log_speaker { get; set; }
            public string log_setting { get; set; }
            public string voice { get; set; }
            public string logvoice { get; set; }
            public string config_offsetX { get; set; }
            public string config_offsetY { get; set; }
            public string config { get; set; }
            public string config_font { get; set; }
            public string config_fontsize { get; set; }
            public string config_width { get; set; }
            public string config_height { get; set; }
        }

        [Serializable]
        private class FontData
        {
            public FaceInfoData m_FaceInfo;
            public List<GlyphData> m_GlyphTable;
            public List<CharacterData> m_CharacterTable;
            public int m_AtlasWidth;
            public int m_AtlasHeight;
            public int m_AtlasPadding;
            public int m_AtlasRenderMode;
        }

        [Serializable]
        private class FaceInfoData
        {
            public string m_FamilyName;
            public string m_StyleName;
            public float m_PointSize;
            public float m_Scale;
            public float m_LineHeight;
            public float m_AscentLine;
            public float m_CapLine;
            public float m_MeanLine;
            public float m_Baseline;
            public float m_DescentLine;
            public float m_SuperscriptOffset;
            public float m_SubscriptOffset;
            public float m_UnderlineOffset;
            public float m_UnderlineThickness;
            public float m_StrikethroughOffset;
            public float m_StrikethroughThickness;
            public float m_TabWidth;
        }

        [Serializable]
        private class GlyphData
        {
            public int m_Index;
            public GlyphMetricsData m_Metrics;
            public GlyphRectData m_GlyphRect;
        }

        [Serializable]
        private class GlyphMetricsData
        {
            public float m_Width;
            public float m_Height;
            public float m_HorizontalBearingX;
            public float m_HorizontalBearingY;
            public float m_HorizontalAdvance;
        }

        [Serializable]
        private class GlyphRectData
        {
            public int m_X;
            public int m_Y;
            public int m_Width;
            public int m_Height;
        }

        [Serializable]
        private class CharacterData
        {
            public int m_Unicode;
            public int m_GlyphIndex;
        }
    }
}