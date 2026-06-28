using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonsterSpawner))]
public class MonsterSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (MonsterSpawner)target;
        var pointsProp = serializedObject.FindProperty("spawnPoints");

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Редактор: расстановка мобов", EditorStyles.boldLabel);

        if (GUILayout.Button("Спавнить полноценных мобов на сцену", GUILayout.Height(35)))
        {
            SpawnRealMonsters(spawner, pointsProp);
        }

        if (GUILayout.Button("Удалить мобов (сбросить точки)", GUILayout.Height(30)))
        {
            ClearSpawnedMonsters(spawner, pointsProp);
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Создаёт ПОЛНОЦЕННЫХ мобов (с AI, статами, HP-баром) как префабы на сцене.\n" +
            "Точки спавна помечаются как выполненные — спавнер не будет дублировать.\n" +
            "«Удалить» — уничтожает заспавненных мобов и сбрасывает точки.",
            MessageType.Info);
    }

    void SpawnRealMonsters(MonsterSpawner spawner, SerializedProperty pointsProp)
    {
        if (pointsProp == null || pointsProp.arraySize == 0)
        {
            Debug.LogWarning("[MonsterSpawnerEditor] Нет точек спавна!");
            return;
        }

        // Контейнер
        var container = GameObject.Find("Monsters");
        if (container == null)
        {
            container = new GameObject("Monsters");
            Undo.RegisterCreatedObjectUndo(container, "Create Monsters container");
        }

        serializedObject.Update();
        int spawned = 0;

        for (int i = 0; i < pointsProp.arraySize; i++)
        {
            var point = pointsProp.GetArrayElementAtIndex(i);
            var doneProp = point.FindPropertyRelative("initialSpawnDone");

            if (doneProp.boolValue)
            {
                Debug.Log($"[MonsterSpawnerEditor] Точка[{i}] уже выполнена — пропускаю");
                continue;
            }

            var typeProp = point.FindPropertyRelative("monsterType");
            var posProp = point.FindPropertyRelative("position");
            var levelProp = point.FindPropertyRelative("level");

            MonsterType type = (MonsterType)typeProp.enumValueIndex;
            Vector2 pos = posProp.vector2Value;
            int level = levelProp.intValue;

            GameObject prefab = GetPrefab(spawner, type);
            if (prefab == null)
            {
                Debug.LogWarning($"[MonsterSpawnerEditor] Префаб для {type} не назначен в спавнере!");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
            instance.name = $"{type}_Lv{level}";
            instance.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(instance, $"Spawn {type}_Lv{level}");

            // Устанавливаем уровень
            var stats = instance.GetComponent<MonsterStats>();
            if (stats != null)
                stats.SetLevel(Mathf.Max(1, level), fillHealth: true);

            doneProp.boolValue = true;
            spawned++;
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[MonsterSpawnerEditor] Заспавнено {spawned} полноценных мобов. Точки помечены как выполненные. Сохрани сцену (Ctrl+S)!");
    }

    void ClearSpawnedMonsters(MonsterSpawner spawner, SerializedProperty pointsProp)
    {
        // Удаляем контейнер с мобами
        var container = GameObject.Find("Monsters");
        if (container != null)
            Undo.DestroyObjectImmediate(container);

        // Сбрасываем точки
        serializedObject.Update();
        if (pointsProp != null)
        {
            for (int i = 0; i < pointsProp.arraySize; i++)
            {
                pointsProp.GetArrayElementAtIndex(i).FindPropertyRelative("initialSpawnDone").boolValue = false;
            }
        }
        serializedObject.ApplyModifiedProperties();

        Debug.Log("[MonsterSpawnerEditor] Все мобы удалены, точки сброшены");
    }

    GameObject GetPrefab(MonsterSpawner spawner, MonsterType type)
    {
        var so = new SerializedObject(spawner);
        string fieldName = type switch
        {
            MonsterType.FlyingEye   => "flyingEyePrefab",
            MonsterType.Goblin      => "goblinPrefab",
            MonsterType.Mushroom    => "mushroomPrefab",
            MonsterType.Skeleton    => "skeletonPrefab",
            MonsterType.StoneGolem  => "stoneGolemPrefab",
            MonsterType.DemonSlime  => "demonSlimePrefab",
            MonsterType.Minotaur    => "minotaurPrefab",
            MonsterType.FlyingDemon => "flyingDemonPrefab",
            _ => null
        };
        if (fieldName == null) return null;
        return so.FindProperty(fieldName).objectReferenceValue as GameObject;
    }
}
