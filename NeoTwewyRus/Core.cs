using Newtonsoft.Json;
using MelonLoader;
using UnityEngine;
using Il2CppTMPro;
using System.Drawing;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using HarmonyLib;
using Il2CppUI.Utility;

[assembly: MelonInfo(typeof(NeoTwewyRus.Core), "NeoTwewyRus", "0.0.1", "Ddedinya", null)]
[assembly: MelonGame("SQUARE ENIX", "NEO: The World Ends with You")]

namespace NeoTwewyRus
{
    public class Core : MelonMod
    {
        private static TMP_FontAsset _newFont; // Создаём отдельную переменную для шрифта, чтобы не пришлось его постоянно создавать

        public override void OnInitializeMelon()
        {
            GetNewFont();
        }

        public static void GetNewFont() // Функция для создания шрифта на замену оригинальному
        {
            try
            {
                var texture = LoadTexture("Mods/NeoTwewyRus/Fonts/FOT-NewRodinProN-B SDF Atlas.png"); // Загрузка атласа шрифта
                if (texture == null)
                {
                    MelonLogger.Error("Не удалось загрузить атлас шрифта.");
                    return;
                }

                var shader = Shader.Find("TextMeshPro/Distance Field"); // Создание шейдера шрифта
                if (shader == null)
                {
                    MelonLogger.Error("Не удалось загрузить шейдер шрифта.");
                    return;
                }

                var material = new Material(shader) // Создание материала шрифта из его шейдера
                {
                    mainTexture = texture,
                    name = "FOT-NewRodinProN-B Material"

                };

                // Обводка
                material.EnableKeyword("OUTLINE_ON");
                material.SetColor("_OutlineColor", UnityEngine.Color.black);
                material.SetFloat("_OutlineWidth", 0.2f);

                // Создание объекта TMP шрифта и загрузка в него кучи инфы. 
                var fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
                fontAsset.name = "FOT-NewRodinProN-B SDF";
                fontAsset.material = material;
                fontAsset.atlas = texture;
                fontAsset.atlasTextures = new Texture2D[] { texture };
                var jsonData = JsonConvert.DeserializeObject<FontData>(File.ReadAllText("Mods/NeoTwewyRus/Fonts/FOT-NewRodinProN-B SDF.json"));

                if (jsonData == null)
                {
                    MelonLogger.Error("Не удалось загрузить данные шрифта.");
                    return;
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
                    MelonLogger.Error("Некоторые важные данные шрифта не были инициализированы.");
                    return;
                }

                fontAsset.atlasWidth = jsonData.m_AtlasWidth;
                fontAsset.atlasHeight = jsonData.m_AtlasHeight;
                fontAsset.atlasPadding = jsonData.m_AtlasPadding;
                fontAsset.atlasRenderMode = (GlyphRenderMode)jsonData.m_AtlasRenderMode;
                fontAsset.InitializeDictionaryLookupTables();

                _newFont = fontAsset; // Присваиваем полученный шрифт и молимся, чтобы он не превратил весь текст в ужас
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Ошибка загрузки шрифта: {ex.Message}");
                return;
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

        // Патч некоторых элементов интерфейса
        [HarmonyPatch(typeof(UIImageLoaderPersonal), "GetSprite")]
        class Patch_UIImageLoaderPersonal_GetSprite
        {
            [HarmonyPostfix]
            public static void Postfix(UIImageLoaderPersonal __instance, ref Sprite __result, Il2CppSystem.String key, Vector2 pivot)
            {
                if (File.Exists($"Mods/NeoTwewyRus/Textures/{__result.name}.png")) 
                {
                    Texture2D texture = LoadTexture($"Mods/NeoTwewyRus/Textures/{__result.name}.png");
                    if (texture == null)
                    {
                        MelonLogger.Error("Не удалось загрузить новый спрайт!");
                        return;
                    }
                    
                    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot);
                    __result = newSprite;
                }
            }
        }

        // Патч шрифта
        [HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
        public static class FontPatch
        {
            [HarmonyPostfix]
            public static void Postfix(TextMeshProUGUI __instance)
            {
                if (__instance.font != null && __instance.font.name == "FOT-NewRodinProN-B SDF" && __instance.name != "Text_name"
                    && __instance.name != "AreaName" && !__instance.name.StartsWith("ShopName"))
                {
                    __instance.font = _newFont;
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
                    MelonLogger.Error($"Ошибка при попытка пропачить {__instance.name}: {ex.Message}");
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