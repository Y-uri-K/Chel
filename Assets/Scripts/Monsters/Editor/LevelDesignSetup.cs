using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Автоматическая настройка уровня: Grid + Tilemap + коллизии + Tile Palette.
/// Tools → Level Design → ...
/// </summary>
public class LevelDesignSetup : EditorWindow
{
    Texture2D tilesheet;
    string tileAssetFolder = "Assets/map/TileMap/Tiles";
    string sortingLayer = "Default";
    int sortingOrder;
    bool addCollider = true;
    bool addBackgroundLayer = true;

    [MenuItem("Tools/Level Design/Setup Tilemap From Texture")]
    public static void ShowWindow()
    {
        GetWindow<LevelDesignSetup>("Level Design Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Настройка уровня из тайлсета", EditorStyles.boldLabel);
        GUILayout.Space(10);

        tilesheet = (Texture2D)EditorGUILayout.ObjectField("Тайлсет (Texture2D)", tilesheet, typeof(Texture2D), false);
        tileAssetFolder = EditorGUILayout.TextField("Папка для tile-ассетов", tileAssetFolder);
        sortingLayer = EditorGUILayout.TextField("Sorting Layer", sortingLayer);
        sortingOrder = EditorGUILayout.IntField("Sorting Order", sortingOrder);
        addCollider = EditorGUILayout.Toggle("Добавить TilemapCollider2D", addCollider);
        addBackgroundLayer = EditorGUILayout.Toggle("Добавить фоновый слой", addBackgroundLayer);

        GUILayout.Space(20);

        if (GUILayout.Button("Создать Grid + Tilemap на сцене", GUILayout.Height(40)))
        {
            if (tilesheet == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Перетащи Texture2D в поле Tilesheet!", "OK");
                return;
            }
            SliceTilesheet();
            CreateLevelGrid();
        }
    }

    [MenuItem("Tools/Level Design/Quick Setup (выделенный спрайт → объект с коллизией)", true)]
    static bool ValidateQuickSetup()
    {
        return Selection.activeObject is Texture2D;
    }

    [MenuItem("Tools/Level Design/Apply Sprite+Collider to Selected", true)]
    static bool ValidateApplyToSelected()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("Tools/Level Design/Apply Sprite+Collider to Selected")]
    static void ApplyToSelectedGameObject()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выдели GameObject на сцене!", "OK");
            return;
        }

        // Ищем первый SpriteRenderer или PolygonCollider2D чтобы понять что уже есть
        var sr = go.GetComponent<SpriteRenderer>();
        var col = go.GetComponent<PolygonCollider2D>();

        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
        if (col == null) col = Undo.AddComponent<PolygonCollider2D>(go);

        // Если спрайт не назначен — пробуем взять из перетащенной текстуры
        if (sr.sprite == null)
        {
            var selectedTex = Selection.activeObject as Texture2D;
            if (selectedTex == null)
            {
                // Ищем текстуры в выделении
                foreach (var obj in Selection.objects)
                {
                    selectedTex = obj as Texture2D;
                    if (selectedTex != null) break;
                }
            }

            if (selectedTex != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedTex);
                SliceTexture(path);
                AssetDatabase.Refresh();
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in sprites)
                    if (asset is Sprite s) { sr.sprite = s; break; }
            }
        }

        // Обновляем коллайдер по форме спрайта
        if (sr.sprite != null)
        {
            FitCollider(col, sr.sprite);
            Debug.Log($"[LevelDesign] '{go.name}': SpriteRenderer + PolygonCollider2D настроены");
        }
        else
        {
            Debug.LogWarning($"[LevelDesign] '{go.name}': спрайт не назначен — перетащи текстуру в инспектор SpriteRenderer");
        }
    }

    [MenuItem("Tools/Level Design/Create Floor Object from Selected Tile/Sprite")]
    static void CreateFloorFromSelected()
    {
        Sprite sprite = null;

        // Пробуем Tile
        var tile = Selection.activeObject as Tile;
        if (tile != null) sprite = tile.sprite;

        // Пробуем Sprite напрямую
        if (sprite == null)
            sprite = Selection.activeObject as Sprite;

        // Пробуем текстуру (берём первый спрайт)
        if (sprite == null)
        {
            var tex = Selection.activeObject as Texture2D;
            if (tex != null)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite s) { sprite = s; break; }
            }
        }

        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выдели Tile, Sprite или Texture2D в Project-окне!", "OK");
            return;
        }

        var go = new GameObject(sprite.name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // BoxCollider2D по всему спрайту
        var col = go.AddComponent<BoxCollider2D>();
        col.size = sprite.bounds.size;
        col.offset = sprite.bounds.center;

        Undo.RegisterCreatedObjectUndo(go, "Create Floor Object");
        Selection.activeGameObject = go;
        Debug.Log($"[LevelDesign] Создан пол: '{go.name}' с BoxCollider2D (размер: {col.size})");
    }

    [MenuItem("Tools/Level Design/Quick Setup (выделенный спрайт → объект с коллизией)")]
    static void QuickSetupSelectedTexture()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex == null) return;

        string path = AssetDatabase.GetAssetPath(tex);
        SliceTexture(path);

        // Создаём GameObject с первым спрайтом
        AssetDatabase.Refresh();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite firstSprite = null;
        foreach (var asset in sprites)
            if (asset is Sprite s) { firstSprite = s; break; }

        if (firstSprite == null)
        {
            Debug.LogError("Не удалось загрузить спрайт из текстуры");
            return;
        }

        var go = new GameObject(tex.name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = firstSprite;

        var col = go.AddComponent<PolygonCollider2D>();
        FitCollider(col, firstSprite);

        Undo.RegisterCreatedObjectUndo(go, "Create Level Object");
        Selection.activeGameObject = go;
        Debug.Log($"Создан объект '{go.name}' с коллизией по форме спрайта");
    }

    static void SliceTilesheet()
    {
        // Настраивает импорт текстуры как Multiple Sprite
        var window = GetWindow<LevelDesignSetup>();
        var tex = window.tilesheet;
        string path = AssetDatabase.GetAssetPath(tex);
        SliceTexture(path);
        AssetDatabase.Refresh();
    }

    static void SliceTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.Tight;
        importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    void CreateLevelGrid()
    {
        // Ищем или создаём корневой объект карты
        var mapRoot = GameObject.Find("map");
        if (mapRoot == null)
        {
            mapRoot = new GameObject("map");
            Undo.RegisterCreatedObjectUndo(mapRoot, "Create map root");
        }

        // Grid
        var gridGo = new GameObject("Grid");
        Undo.RegisterCreatedObjectUndo(gridGo, "Create Grid");
        gridGo.transform.SetParent(mapRoot.transform);
        var grid = gridGo.AddComponent<Grid>();

        // Основной Tilemap (земля/стены)
        var groundGo = new GameObject("Ground");
        Undo.RegisterCreatedObjectUndo(groundGo, "Create Ground Tilemap");
        groundGo.transform.SetParent(gridGo.transform);
        var groundTm = groundGo.AddComponent<Tilemap>();
        var groundTr = groundGo.AddComponent<TilemapRenderer>();
        groundTr.sortingLayerName = sortingLayer;
        groundTr.sortingOrder = sortingOrder;

        if (addCollider)
        {
            var groundCol = groundGo.AddComponent<TilemapCollider2D>();
            // CompositeCollider2D для оптимизации (объединяет тайлы в один коллайдер)
            var composite = groundGo.AddComponent<CompositeCollider2D>();
            groundCol.usedByComposite = true;
            groundGo.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }

        // Фоновый слой (декор без коллизий)
        if (addBackgroundLayer)
        {
            var bgGo = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(bgGo, "Create Background Tilemap");
            bgGo.transform.SetParent(gridGo.transform);
            var bgTm = bgGo.AddComponent<Tilemap>();
            var bgTr = bgGo.AddComponent<TilemapRenderer>();
            bgTr.sortingLayerName = sortingLayer;
            bgTr.sortingOrder = sortingOrder - 1;
        }

        // Создаём Tile Palette из нарезанных спрайтов
        CreateTileAssets();

        Selection.activeGameObject = gridGo;
        Debug.Log($"Level Grid создан: Grid + Ground (+ Background) с коллизиями. Теперь открой Tile Palette (Window → 2D → Tile Palette) и рисуй!");
    }

    void CreateTileAssets()
    {
        string path = AssetDatabase.GetAssetPath(tilesheet);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path);

        if (!System.IO.Directory.Exists(tileAssetFolder))
            System.IO.Directory.CreateDirectory(tileAssetFolder);

        int count = 0;
        foreach (var asset in sprites)
        {
            if (asset is Sprite sprite)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Sprite;

                string assetPath = $"{tileAssetFolder}/{tilesheet.name}_{count}.asset";
                AssetDatabase.CreateAsset(tile, assetPath);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Создано {count} tile-ассетов в {tileAssetFolder}. Перетащи их в Tile Palette.");
    }

    static void FitCollider(PolygonCollider2D col, Sprite sprite)
    {
        int n = sprite.GetPhysicsShapeCount();
        if (n > 0)
        {
            col.pathCount = n;
            var shape = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                sprite.GetPhysicsShape(i, shape);
                col.SetPath(i, shape.ToArray());
                shape.Clear();
            }
        }
        else
        {
            var b = sprite.bounds;
            col.pathCount = 1;
            col.SetPath(0, new[] {
                new Vector2(b.min.x, b.min.y),
                new Vector2(b.min.x, b.max.y),
                new Vector2(b.max.x, b.max.y),
                new Vector2(b.max.x, b.min.y)
            });
        }
    }
}
