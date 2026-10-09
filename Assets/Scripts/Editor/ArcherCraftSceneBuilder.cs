using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using ArcherCraft.Combat;
using ArcherCraft.Player;
using ArcherCraft.Enemy;
using ArcherCraft.Core;
using ArcherCraft.Vision;
using ArcherCraft.UI;

namespace ArcherCraft.Editor
{
    public static class ArcherCraftSceneBuilder
    {
        [MenuItem("ArcherCraft/Setup Prototype Sandbox Scene")]
        public static void SetupPrototypeScene()
        {
            // 1. 새 씬 생성
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. 태그 및 레이어 안내/설정 확인
            EnsureTagExists("Player");
            EnsureTagExists("Enemy");
            EnsureTagExists("Obstacle");

            // 3. 라이트 & 게임 매니저 생성
            GameObject dirLight = new GameObject("Directional Light");
            var lightComp = dirLight.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1f, 0.96f, 0.9f);
            lightComp.intensity = 1.1f;
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // 4. 바닥 (Ground)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50m x 50m

            // 바닥 머티리얼 (어두운 회색 설원/포장 느낌)
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMat.color = new Color(0.22f, 0.25f, 0.28f);
            AssetDatabase.CreateAsset(groundMat, "Assets/Materials/Mat_Ground.mat");
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // 5. 장애물 기둥 (Obstacles)
            Material obstacleMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            obstacleMat.color = new Color(0.45f, 0.5f, 0.55f);
            AssetDatabase.CreateAsset(obstacleMat, "Assets/Materials/Mat_Obstacle.mat");

            Vector3[] obstaclePositions = new Vector3[]
            {
                new Vector3(-6f, 1.5f, 4f),
                new Vector3(6f, 1.5f, 3f),
                new Vector3(0f, 1.5f, 9f),
                new Vector3(-8f, 1.5f, -5f),
                new Vector3(7f, 1.5f, -6f)
            };

            GameObject obstacleRoot = new GameObject("Obstacles");
            foreach (var pos in obstaclePositions)
            {
                GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.name = "Obstacle_Pillar";
                obs.tag = "Obstacle";
                obs.transform.position = pos;
                obs.transform.localScale = new Vector3(2f, 3f, 2f);
                obs.transform.parent = obstacleRoot.transform;
                obs.GetComponent<MeshRenderer>().sharedMaterial = obstacleMat;
            }

            // 6. 프리팹 생성: 탄막 상쇄 스파크 이펙트 (Parry Spark)
            GameObject sparkPrefab = CreateSparkVFXPrefab();

            // 7. 프리팹 생성: 적 탄환 (Enemy Bullet)
            GameObject bulletPrefab = CreateEnemyBulletPrefab(sparkPrefab);

            // 8. 프리팹 생성: 플레이어 화살 (Arrow Projectile)
            GameObject arrowPrefab = CreateArrowPrefab(sparkPrefab);

            // 9. 플레이어 캐릭터 생성
            GameObject player = CreatePlayerObject(arrowPrefab);

            // 10. FOV (Field Of View) 시야각 메시 생성
            CreateFOVVisualizer(player);

            // 11. 쿼터뷰 카메라 생성
            CreateTopDownCamera(player);

            // 12. 적 (Enemy) 2체 스폰
            CreateEnemies(bulletPrefab, player);

            // 13. UI Canvas (조준선, 게이지, HUD)
            CreateUIElements(player);

            // 14. 씬 저장
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/PrototypeSandbox.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("ArcherCraft Setup Complete",
                "프로토타입 샌드박스 씬(PrototypeSandbox.unity) 생성이 완료되었습니다!\n\n" +
                "▶ 조작법:\n" +
                "- WASD: 이동\n" +
                "- Space: 대시/회피\n" +
                "- 좌클릭 탭: 즉발 퀵샷 (탄막 즉시 상쇄 요격)\n" +
                "- 좌클릭 홀드: 파워샷 차징 (사거리/대미지/관통 증가)\n" +
                "- 우클릭 or F: 근접 발차기 넉백\n" +
                "- R: 씬 재시작\n\n" +
                "이제 [Play] 버튼을 눌러 테스트해 보세요!", "확인");
        }

        private static GameObject CreateSparkVFXPrefab()
        {
            GameObject spark = new GameObject("FX_ParrySpark");
            var ps = spark.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.3f;
            main.startSpeed = 8f;
            main.startSize = 0.25f;
            main.startColor = new Color(1f, 0.85f, 0.2f); // 밝은 황금빛 스파크
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            spark.AddComponent<ImpactEffect>();
            return PrefabUtility.SaveAsPrefabAsset(spark, "Assets/Prefabs/FX_ParrySpark.prefab");
        }

        private static GameObject CreateEnemyBulletPrefab(GameObject sparkPrefab)
        {
            GameObject bulletObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObj.name = "EnemyBullet";
            bulletObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var col = bulletObj.GetComponent<SphereCollider>();
            col.isTrigger = true;

            Material bulletMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bulletMat.color = new Color(1f, 0.2f, 0.2f); // 붉은색 탄환
            bulletMat.EnableKeyword("_EMISSION");
            bulletMat.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.1f) * 2f);
            AssetDatabase.CreateAsset(bulletMat, "Assets/Materials/Mat_Bullet.mat");
            bulletObj.GetComponent<MeshRenderer>().sharedMaterial = bulletMat;

            var bullet = bulletObj.AddComponent<EnemyBullet>();
            SerializedObject so = new SerializedObject(bullet);
            so.FindProperty("parrySparkEffectPrefab").objectReferenceValue = sparkPrefab;
            so.ApplyModifiedProperties();

            return PrefabUtility.SaveAsPrefabAsset(bulletObj, "Assets/Prefabs/EnemyBullet.prefab");
        }

        private static GameObject CreateArrowPrefab(GameObject sparkPrefab)
        {
            GameObject arrowObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrowObj.name = "ArrowProjectile";
            arrowObj.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
            arrowObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 전방 정렬

            // 실린더 기본 콜라이더 제거 후 구형 트리거 부착
            Object.DestroyImmediate(arrowObj.GetComponent<CapsuleCollider>());
            var col = arrowObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;

            Material arrowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            arrowMat.color = new Color(0.2f, 0.9f, 1f); // 청록빛 발광 화살
            arrowMat.EnableKeyword("_EMISSION");
            arrowMat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1f) * 1.5f);
            AssetDatabase.CreateAsset(arrowMat, "Assets/Materials/Mat_Arrow.mat");
            arrowObj.GetComponent<MeshRenderer>().sharedMaterial = arrowMat;

            var arrow = arrowObj.AddComponent<ArrowProjectile>();
            SerializedObject so = new SerializedObject(arrow);
            so.FindProperty("parrySparkPrefab").objectReferenceValue = sparkPrefab;
            so.ApplyModifiedProperties();

            return PrefabUtility.SaveAsPrefabAsset(arrowObj, "Assets/Prefabs/ArrowProjectile.prefab");
        }

        private static GameObject CreatePlayerObject(GameObject arrowPrefab)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Archer";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);

            Material playerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            playerMat.color = new Color(0.2f, 0.65f, 0.4f); // 녹색 후드 궁수 느낌
            AssetDatabase.CreateAsset(playerMat, "Assets/Materials/Mat_Player.mat");
            player.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

            player.AddComponent<CharacterController>();
            player.AddComponent<Health>();
            player.AddComponent<PlayerController>();

            var bow = player.AddComponent<PlayerBow>();

            // 발사 위치 FirePoint 오브젝트
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.parent = player.transform;
            firePoint.transform.localPosition = new Vector3(0f, 0.3f, 0.8f);

            SerializedObject bowSo = new SerializedObject(bow);
            bowSo.FindProperty("arrowPrefab").objectReferenceValue = arrowPrefab;
            bowSo.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            bowSo.ApplyModifiedProperties();

            return player;
        }

        private static void CreateFOVVisualizer(GameObject player)
        {
            GameObject fovObj = new GameObject("FieldOfView_Visualizer");
            fovObj.transform.parent = player.transform;
            fovObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var fov = fovObj.AddComponent<FieldOfView>();

            Material fovMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            fovMat.color = new Color(1f, 1f, 0.4f, 0.15f); // 은은한 노란색 시야 부채꼴
            AssetDatabase.CreateAsset(fovMat, "Assets/Materials/Mat_FOV.mat");
            fovObj.GetComponent<MeshRenderer>().sharedMaterial = fovMat;
        }

        private static void CreateTopDownCamera(GameObject player)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();

            var follow = camObj.AddComponent<TopDownCameraFollow>();
            camObj.transform.position = player.transform.position + new Vector3(0f, 15f, -9f);
        }

        private static void CreateEnemies(GameObject bulletPrefab, GameObject player)
        {
            Material enemyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            enemyMat.color = new Color(0.85f, 0.3f, 0.25f); // 붉은색 기계 골렘 몬스터
            AssetDatabase.CreateAsset(enemyMat, "Assets/Materials/Mat_Enemy.mat");

            Vector3[] spawnPositions = new Vector3[]
            {
                new Vector3(-4f, 1f, 8f),
                new Vector3(5f, 1f, 10f)
            };

            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                enemy.name = $"Enemy_Golem_{i + 1}";
                enemy.tag = "Enemy";
                enemy.transform.position = spawnPositions[i];
                enemy.GetComponent<MeshRenderer>().sharedMaterial = enemyMat;

                enemy.AddComponent<Health>();
                var emitter = enemy.AddComponent<EnemyBulletEmitter>();
                var ai = enemy.AddComponent<EnemyAI>();

                SerializedObject emitterSo = new SerializedObject(emitter);
                emitterSo.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
                emitterSo.ApplyModifiedProperties();
            }
        }

        private static void CreateUIElements(GameObject player)
        {
            // UI Canvas 생성
            GameObject canvasObj = new GameObject("InGame_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // AimReticleUI 컴포넌트 부착
            var aimUI = canvasObj.AddComponent<AimReticleUI>();

            // 사거리 링 LineRenderer 오브젝트
            GameObject rangeRingObj = new GameObject("OptimalRangeRing");
            var lineRenderer = rangeRingObj.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.08f;
            lineRenderer.endWidth = 0.08f;
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0.3f, 0.8f, 1f, 0.5f);
            lineRenderer.endColor = new Color(0.3f, 0.8f, 1f, 0.5f);

            SerializedObject aimSo = new SerializedObject(aimUI);
            aimSo.FindProperty("rangeRingLine").objectReferenceValue = lineRenderer;
            aimSo.ApplyModifiedProperties();

            // 체력 HUD UI
            canvasObj.AddComponent<InGameHUD>();
        }

        private static void EnsureTagExists(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                {
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
