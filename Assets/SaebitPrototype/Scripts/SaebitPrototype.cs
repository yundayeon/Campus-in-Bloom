using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SaebitUniversity
{
    public class SaebitPrototype : MonoBehaviour
    {
        private enum Panel { None, Dialogue, Approval, Event, Summary, Inventory, Relationships, CampusMap, Shop }

        [Serializable]
        private class SaveData
        {
            public int day;
            public int money;
            public int reputation;
            public int happiness;
            public int presidentXp;
            public int speechXp;
            public int financeXp;
            public int negotiationXp;
            public int nextEventDay;
            public int wallet;
            public List<string> itemNames = new List<string>();
            public List<int> itemCounts = new List<int>();
            public List<string> npcNames = new List<string>();
            public List<int> npcAffection = new List<int>();
        }

        private class Npc
        {
            public string Name;
            public string Role;
            public string Greeting;
            public int Affection;
            public Transform Transform;
            public bool TalkedToday;
            public string FavoriteGift;
            public string HatedGift;
            public List<ScheduleStop> Schedule = new List<ScheduleStop>();
            public string CurrentActivity;
            public float MoveSpeed;
            public Vector2 AvoidDirection;
            public Texture2D Portrait;
        }

        private class ScheduleStop
        {
            public int Time;
            public Vector2 Position;
            public string Activity;

            public ScheduleStop(int time, Vector2 position, string activity)
            {
                Time = time;
                Position = position;
                Activity = activity;
            }
        }

        private readonly List<Npc> npcs = new List<Npc>();
        private readonly List<Rect> buildingColliders = new List<Rect>();
        private readonly List<SpriteRenderer> proceduralEnvironmentRenderers = new List<SpriteRenderer>();
        private readonly Dictionary<string, int> inventory = new Dictionary<string, int>
        {
            { "따뜻한 커피", 20 },
            { "허브차", 20 },
            { "수제 쿠키", 20 },
            { "매운 생선빵", 20 }
        };
        private Transform player;
        private Camera cam;
        private bool insideOffice;
        private bool officeBuilt;
        private readonly Vector2 officeDoorOutside = new Vector2(0f, 6.35f);
        private readonly Vector2 officeDoorInside = new Vector2(50f, -5.15f);
        private readonly Vector2 campusShopOutside = new Vector2(18f, 4.25f);
        private Panel panel;
        private Npc activeNpc;
        private Vector2 moveInput;
        private float gameMinutes;
        private float minuteTimer;
        private int day = 1;
        private int money = 8000;
        private int wallet = 50000;
        private int reputation = 38;
        private int happiness = 52;
        private int presidentXp;
        private int speechXp;
        private int financeXp;
        private int negotiationXp;
        private int dayStartSpeechLevel;
        private int dayStartFinanceLevel;
        private int dayStartNegotiationLevel;
        private int dayStartSpeechXp;
        private int dayStartFinanceXp;
        private int dayStartNegotiationXp;
        private int approvalsDone;
        private int conversations;
        private bool eventTriggered;
        private bool approvalResolved;
        private bool choosingGift;
        private bool showingGiftReaction;
        private string dialogueOverride = "";
        private int nextEventDay = 6;
        private const string SaveKey = "SaebitUniversityPrototypeSave";
        private string toast = "WASD 또는 방향키로 캠퍼스를 둘러보세요";
        private float toastUntil = 8f;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private Texture2D panelTexture;
        private Texture2D darkTexture;
        private Texture2D campusMapTexture;
        private Texture2D itemIconAtlas;
        private Texture2D shopkeeperPortrait;
        private bool hasCustomShopkeeperPortrait;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SaebitPrototype>() == null)
                new GameObject("Saebit Prototype").AddComponent<SaebitPrototype>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            itemIconAtlas = Resources.Load<Texture2D>("content/items/gift-items-v1");
            if (itemIconAtlas != null) itemIconAtlas.filterMode = FilterMode.Point;
            shopkeeperPortrait = Resources.Load<Texture2D>("content/npc/portraits/shopkeeper-v1");
            hasCustomShopkeeperPortrait = shopkeeperPortrait != null;
            if (shopkeeperPortrait == null) shopkeeperPortrait = PersonSprite(new Color32(192, 112, 71, 255), false, 2).texture;
            shopkeeperPortrait.filterMode = FilterMode.Point;
            BuildWorld();
            LoadGame();
            RememberAbilityLevels();
        }

        private void BuildWorld()
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.7f;
            cam.backgroundColor = new Color32(239, 225, 174, 255);
            cam.transform.position = new Vector3(0, 0, -10);

            CreateBlock("Grass", Vector2.zero, new Vector2(60, 40), new Color32(137, 181, 112, 255), -10);
            CreateBlock("South City Road", new Vector2(0, -18.2f), new Vector2(60, 3.2f), new Color32(181, 176, 157, 255), -8);
            CreateBlock("Main Gate Avenue", new Vector2(0, -10.5f), new Vector2(3.2f, 14), new Color32(223, 196, 143, 255), -8);
            CreateBlock("Loop North", new Vector2(0, 9.5f), new Vector2(43, 2.2f), new Color32(223, 196, 143, 255), -8);
            CreateBlock("Loop South", new Vector2(0, -5.0f), new Vector2(43, 2.2f), new Color32(223, 196, 143, 255), -8);
            CreateBlock("Loop West", new Vector2(-20.5f, 2.2f), new Vector2(2.2f, 16.5f), new Color32(223, 196, 143, 255), -8);
            CreateBlock("Loop East", new Vector2(20.5f, 2.2f), new Vector2(2.2f, 16.5f), new Color32(223, 196, 143, 255), -8);
            CreateBlock("Central Walk", new Vector2(0, 2.0f), new Vector2(37, 2.0f), new Color32(232, 207, 158, 255), -7);
            CreateBlock("Central Field", new Vector2(-3.5f, -0.6f), new Vector2(15, 7), new Color32(104, 166, 91, 255), -6);

            CreateBuilding("본관 총장실", new Vector2(0, 8.0f), new Vector2(10, 5.5f), new Color32(199, 118, 82, 255));
            CreateBuilding("중앙도서관", new Vector2(-17.0f, 7.5f), new Vector2(9, 6.0f), new Color32(228, 187, 112, 255));
            CreateBuilding("인문사회관", new Vector2(-20.0f, -0.5f), new Vector2(9, 5.5f), new Color32(220, 156, 112, 255));
            CreateBuilding("공학 자연과학관", new Vector2(-20.0f, -9.5f), new Vector2(9, 6.0f), new Color32(119, 150, 169, 255));
            CreateBuilding("학생회관", new Vector2(17.0f, 6.5f), new Vector2(10, 6.0f), new Color32(107, 150, 145, 255));
            CreateBuilding("기숙사 구역", new Vector2(-12.0f, 15.0f), new Vector2(12, 5.0f), new Color32(150, 119, 153, 255));
            CreateBuilding("폐쇄된 구관", new Vector2(7.5f, 15.0f), new Vector2(8, 5.0f), new Color32(113, 105, 91, 255));
            CreateBuilding("체육관", new Vector2(14.0f, -9.5f), new Vector2(10, 7.0f), new Color32(112, 143, 171, 255));

            for (int i = 0; i < 34; i++)
            {
                float x = -27 + (i * 5.3f) % 55;
                float y = i % 2 == 0 ? 18.0f : -15.8f;
                CreateTree(new Vector2(x, y));
            }
            for (int i = 0; i < 10; i++) CreateTree(new Vector2(25.0f, -11f + i * 2.8f));

            player = CreatePerson("총장", new Vector2(0, -15.2f), new Color32(42, 79, 68, 255), true);
            AddNpc("김하늘", "사회학과 학생", "총장님도 직접 캠퍼스를 돌아다니시네요? 조금 의외예요.", "수제 쿠키", "매운 생선빵", new Vector2(14.0f, 3.0f), new Vector2(15.0f, 3.0f), new Vector2(-18.0f, -3.8f), new Color32(214, 99, 86, 255));
            AddNpc("박지훈", "도서관 직원", "도서관 열람석이 부족해요. 결재함에 개선안을 올려뒀습니다.", "따뜻한 커피", "매운 생선빵", new Vector2(-19.0f, 6.15f), new Vector2(-13.0f, 3.0f), new Vector2(-20.5f, 6.15f), new Color32(75, 112, 160, 255));
            AddNpc("최미숙", "환경미화 노동자", "좋은 아침이에요, 총장님. 오늘도 캠퍼스가 꽤 넓네요.", "허브차", "따뜻한 커피", new Vector2(0.0f, -8.0f), new Vector2(13.0f, 2.5f), new Vector2(4.0f, -1.0f), new Color32(152, 87, 126, 255));
            AddNpc("이도윤", "경영학과 교수", "올해 연구비 배분에 관해 드릴 말씀이 있습니다.", "매운 생선빵", "수제 쿠키", new Vector2(-19.0f, -9.2f), new Vector2(13.0f, 2.5f), new Vector2(-18.0f, -3.8f), new Color32(99, 87, 66, 255));
            ApplyCampusArtBackground();
        }

        private void ApplyCampusArtBackground()
        {
            Texture2D campusTexture = Resources.Load<Texture2D>("content/world/campus/campus-ground-v1");
            if (campusTexture == null) campusTexture = Resources.Load<Texture2D>("content/world/campus/campus-composite-v2");
            if (campusTexture == null) return;
            campusMapTexture = Resources.Load<Texture2D>("content/world/map/campus-map-v2");
            if (campusMapTexture == null) campusMapTexture = campusTexture;

            // 생성형 임시 건물 대신, 현재 배경 원화에서 보이는 건물 몸체에 맞춘다.
            buildingColliders.Clear();
            AddCampusCollider(-13.7f, 0.3f, 14.1f, 18.5f);     // 기숙사
            AddCampusCollider(2.8f, 13.8f, 12.5f, 17.9f);      // 폐쇄된 구관
            AddCampusCollider(-23.8f, -14.8f, 7.0f, 12.8f);   // 중앙도서관
            AddCampusCollider(-5.9f, 8.6f, 7.2f, 12.5f);      // 본관
            AddCampusCollider(12.4f, 25.1f, 4.9f, 11.9f);     // 학생회관
            AddCampusCollider(-26.2f, -14.8f, -0.9f, 5.1f);   // 인문사회관
            AddCampusCollider(-25.5f, -14.7f, -8.5f, -1.8f);  // 공학·자연과학관
            AddCampusCollider(7.6f, 16.8f, -11.3f, -2.4f);    // 체육관
            foreach (SpriteRenderer environmentRenderer in proceduralEnvironmentRenderers)
                if (environmentRenderer != null) environmentRenderer.enabled = false;

            campusTexture.filterMode = FilterMode.Point;
            Sprite campusSprite = Sprite.Create(
                campusTexture,
                new Rect(0, 0, campusTexture.width, campusTexture.height),
                new Vector2(0.5f, 0.5f),
                campusTexture.width / 60f,
                0,
                SpriteMeshType.FullRect);
            GameObject campus = new GameObject("Saebit University Pixel Campus");
            SpriteRenderer renderer = campus.AddComponent<SpriteRenderer>();
            renderer.sprite = campusSprite;
            renderer.sortingOrder = -9;

            // 개별 PNG가 준비되면 해당 레이어만 독립적으로 교체된다.
            CreateOptionalWorldLayer("content/world/environment/roads-v1", "Campus Roads", Vector2.zero, new Vector2(60f, 40f), -8);
            CreateOptionalWorldLayer("content/world/environment/trees-v1", "Campus Trees", Vector2.zero, new Vector2(60f, 40f), -5);
            CreateOptionalWorldLayer("content/world/buildings/administration-v1", "Administration Art", new Vector2(1.35f, 9.85f), new Vector2(14.5f, 5.3f), -4);
            CreateOptionalWorldLayer("content/world/buildings/library-v1", "Library Art", new Vector2(-19.3f, 9.9f), new Vector2(9.2f, 5.8f), -4);
            CreateOptionalWorldLayer("content/world/buildings/humanities-v1", "Humanities Art", new Vector2(-20.5f, 2.1f), new Vector2(11.4f, 6.2f), -4);
            CreateOptionalWorldLayer("content/world/buildings/science-engineering-v1", "Science Engineering Art", new Vector2(-20.1f, -5.1f), new Vector2(11.0f, 7.0f), -4);
            CreateOptionalWorldLayer("content/world/buildings/student-center-v1", "Student Center Art", new Vector2(18.7f, 8.3f), new Vector2(12.7f, 7.2f), -4);
            CreateOptionalWorldLayer("content/world/buildings/dormitory-v1", "Dormitory Art", new Vector2(-6.7f, 16.0f), new Vector2(14.0f, 5.0f), -4);
            CreateOptionalWorldLayer("content/world/buildings/old-hall-v1", "Old Hall Art", new Vector2(8.3f, 15.2f), new Vector2(11.0f, 5.6f), -4);
            CreateOptionalWorldLayer("content/world/buildings/gym-v1", "Gym Art", new Vector2(12.2f, -6.9f), new Vector2(10.0f, 9.0f), -4);
        }

        private void CreateOptionalWorldLayer(string resourcePath, string objectName, Vector2 position, Vector2 worldSize, int sortingOrder)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return;
            texture.filterMode = FilterMode.Point;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width / worldSize.x, 0, SpriteMeshType.FullRect);
            GameObject layer = new GameObject(objectName);
            layer.transform.position = position;
            float naturalHeight = texture.height * worldSize.x / texture.width;
            layer.transform.localScale = new Vector3(1f, worldSize.y / naturalHeight, 1f);
            SpriteRenderer layerRenderer = layer.AddComponent<SpriteRenderer>();
            layerRenderer.sprite = sprite;
            layerRenderer.sortingOrder = sortingOrder;
        }

        private void AddCampusCollider(float minX, float maxX, float minY, float maxY)
        {
            buildingColliders.Add(Rect.MinMaxRect(minX, minY, maxX, maxY));
        }

        private void Update()
        {
            ReadInput();
            if (panel == Panel.None)
            {
                Vector3 candidate = player.position + (Vector3)(moveInput * (4.2f * Time.deltaTime));
                if (CanMoveTo(candidate)) player.position = candidate;
                if (insideOffice)
                {
                    player.position = new Vector3(Mathf.Clamp(player.position.x, 42.8f, 57.2f), Mathf.Clamp(player.position.y, -5.45f, 5.25f), 0);
                    cam.transform.position = new Vector3(50f, 0f, -10);
                }
                else
                {
                    player.position = new Vector3(Mathf.Clamp(player.position.x, -28.5f, 28.5f), Mathf.Clamp(player.position.y, -17.2f, 19.2f), 0);
                    cam.transform.position = new Vector3(Mathf.Clamp(player.position.x, -22.0f, 22.0f), Mathf.Clamp(player.position.y, -11.5f, 12.5f), -10);
                }
                TickClock();
                UpdateNpcSchedules();
                CheckInteraction();
                CheckEvent();
            }
        }

        private void ReadInput()
        {
            moveInput = Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (panel == Panel.None)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1;
                moveInput = moveInput.normalized;
                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Interact();
                if (kb.tabKey.wasPressedThisFrame) panel = Panel.Approval;
                if (kb.iKey.wasPressedThisFrame) { choosingGift = false; panel = Panel.Inventory; }
                if (kb.rKey.wasPressedThisFrame) panel = Panel.Relationships;
                if (kb.mKey.wasPressedThisFrame) panel = Panel.CampusMap;
            }
            else if (kb.escapeKey.wasPressedThisFrame) panel = Panel.None;
        }

        private void TickClock()
        {
            minuteTimer += Time.deltaTime;
            if (minuteTimer < 0.7f) return;
            minuteTimer = 0;
            gameMinutes += 10;
            if (gameMinutes >= 480 && !approvalResolved && panel == Panel.None)
            {
                // 결재는 플레이를 강제로 멈추지 않고 HUD 아이콘으로만 알린다.
            }
            if (gameMinutes >= 720 && panel == Panel.None) panel = Panel.Summary;
        }

        private void UpdateNpcSchedules()
        {
            int clock = 480 + Mathf.RoundToInt(gameMinutes);
            foreach (Npc npc in npcs)
            {
                ScheduleStop currentStop = npc.Schedule[0];
                for (int i = 1; i < npc.Schedule.Count; i++)
                {
                    if (clock < npc.Schedule[i].Time) break;
                    currentStop = npc.Schedule[i];
                }
                npc.CurrentActivity = currentStop.Activity;

                MoveNpcAroundBuildings(npc, currentStop.Position);
            }
        }

        private void MoveNpcAroundBuildings(Npc npc, Vector2 target)
        {
            Vector2 current = npc.Transform.position;
            if (!CanNpcMoveTo(current))
            {
                current = ClosestWalkablePoint(current);
                npc.Transform.position = current;
                npc.AvoidDirection = Vector2.zero;
            }
            float step = npc.MoveSpeed * Time.deltaTime;
            Vector2 direct = Vector2.MoveTowards(current, target, step);
            if (CanNpcMoveTo(direct))
            {
                npc.AvoidDirection = Vector2.zero;
                npc.Transform.position = direct;
                return;
            }

            // 벽을 만났을 때 네 방향 중 목표에 가장 가까워지는 통로를 골라 외곽을 따라간다.
            Vector2[] directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
            if (npc.AvoidDirection != Vector2.zero)
            {
                Vector2 followWall = current + npc.AvoidDirection * step;
                if (CanNpcMoveTo(followWall))
                {
                    npc.Transform.position = followWall;
                    return;
                }
                npc.AvoidDirection = Vector2.zero;
            }
            Vector2 best = current;
            float bestScore = float.MaxValue;
            foreach (Vector2 direction in directions)
            {
                Vector2 candidate = current + direction * step;
                if (!CanNpcMoveTo(candidate)) continue;
                float score = Vector2.Distance(candidate, target);
                // 정면에서 막혀도 한쪽으로 우회할 수 있도록 아주 작은 방향 우선값을 준다.
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    npc.AvoidDirection = direction;
                }
            }
            npc.Transform.position = best;
        }

        private Vector2 ClosestWalkablePoint(Vector2 point)
        {
            Vector2 best = new Vector2(0f, -15.2f);
            float bestDistance = float.MaxValue;
            foreach (Rect collider in buildingColliders)
            {
                Rect padded = new Rect(collider.xMin - 0.38f, collider.yMin - 0.38f, collider.width + 0.76f, collider.height + 0.76f);
                if (!padded.Contains(point)) continue;
                Vector2[] exits =
                {
                    new Vector2(padded.xMin - 0.08f, point.y),
                    new Vector2(padded.xMax + 0.08f, point.y),
                    new Vector2(point.x, padded.yMin - 0.08f),
                    new Vector2(point.x, padded.yMax + 0.08f)
                };
                foreach (Vector2 exitPoint in exits)
                {
                    if (!CanNpcMoveTo(exitPoint)) continue;
                    float distance = Vector2.Distance(point, exitPoint);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = exitPoint;
                    }
                }
            }
            return best;
        }

        private bool CanNpcMoveTo(Vector2 candidate)
        {
            if (candidate.x < -28.5f || candidate.x > 28.5f || candidate.y < -17.2f || candidate.y > 19.2f) return false;
            foreach (Rect collider in buildingColliders)
            {
                Rect padded = new Rect(collider.xMin - 0.32f, collider.yMin - 0.32f, collider.width + 0.64f, collider.height + 0.64f);
                if (padded.Contains(candidate)) return false;
            }
            return true;
        }

        private void CheckInteraction()
        {
            activeNpc = null;
            if (insideOffice) return;
            float best = 1.55f;
            foreach (Npc npc in npcs)
            {
                float distance = Vector2.Distance(player.position, npc.Transform.position);
                if (distance < best) { best = distance; activeNpc = npc; }
            }
        }

        private void Interact()
        {
            if (insideOffice && Vector2.Distance(player.position, officeDoorInside) < 1.35f)
            {
                ExitOffice();
                return;
            }
            if (!insideOffice && Vector2.Distance(player.position, officeDoorOutside) < 1.45f)
            {
                EnterOffice();
                return;
            }
            if (!insideOffice && Vector2.Distance(player.position, campusShopOutside) < 1.55f)
            {
                panel = Panel.Shop;
                return;
            }
            if (activeNpc == null)
            {
                ShowToast("가까운 사람에게 다가가 E를 눌러보세요.");
                return;
            }
            if (activeNpc.TalkedToday)
            {
                ShowToast($"{activeNpc.Name}와는 오늘 이미 이야기했습니다. 내일 다시 만나요.");
                return;
            }
            activeNpc.TalkedToday = true;
            int talkGain = 5;
            activeNpc.Affection += talkGain;
            conversations++;
            presidentXp += 8;
            speechXp += 8;
            panel = Panel.Dialogue;
        }

        private void CheckEvent()
        {
            // 돌발 사건은 40일짜리 1년 동안 평균 2~3회만 발생한다.
            // 첫해 첫 사건도 최소 6일차 이후에만 나타난다.
            if (insideOffice || eventTriggered || day < nextEventDay || gameMinutes < 240) return;
            if (Vector2.Distance(player.position, new Vector2(-3.5f, -0.6f)) < 4.0f)
            {
                eventTriggered = true;
                panel = Panel.Event;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHud();
            DrawMiniHelp();
            if (activeNpc != null && panel == Panel.None) DrawInteractionPrompt();
            if (panel == Panel.None) DrawDoorPrompt();
            if (panel == Panel.None) DrawShopPrompt();
            if (Time.time < toastUntil) DrawToast();
            switch (panel)
            {
                case Panel.Dialogue: DrawDialogue(); break;
                case Panel.Approval: DrawApproval(); break;
                case Panel.Event: DrawEvent(); break;
                case Panel.Summary: DrawSummary(); break;
                case Panel.Inventory: DrawInventory(); break;
                case Panel.Relationships: DrawRelationships(); break;
                case Panel.CampusMap: DrawCampusMap(); break;
                case Panel.Shop: DrawShop(); break;
            }
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(18, 18, 260, 148), GUIContent.none, bodyStyle);
            GUI.Label(new Rect(34, 29, 225, 28), $"1년차 · 봄 {day}일", titleStyle);
            GUI.Label(new Rect(34, 61, 225, 24), ClockText(), bodyStyle);
            GUI.Label(new Rect(34, 88, 225, 22), $"예산 {money:N0}억   평판 {reputation}", smallStyle);
            GUI.Label(new Rect(34, 111, 225, 22), $"행복 {happiness}%   총장 Lv.{1 + presidentXp / 50}", smallStyle);
            GUI.Label(new Rect(34, 133, 225, 22), $"개인 소지금 {wallet:N0}원", smallStyle);
            DrawApprovalIcon();
            DrawRelationshipIcon();
            DrawMapIcon();
        }

        private void DrawApprovalIcon()
        {
            Rect iconRect = new Rect(Screen.width - 132, 105, 108, 72);
            string label = approvalResolved ? "▤\n결재 완료" : "▤   !\n결재 1건";
            GUI.enabled = !approvalResolved && panel == Panel.None;
            if (GUI.Button(iconRect, label, buttonStyle)) panel = Panel.Approval;
            GUI.enabled = true;
            if (!approvalResolved)
            {
                GUIStyle alertStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter };
                alertStyle.normal.textColor = new Color32(210, 68, 52, 255);
                GUI.Label(new Rect(iconRect.x + 74, iconRect.y - 10, 34, 34), "!", alertStyle);
            }
        }

        private void DrawRelationshipIcon()
        {
            Rect iconRect = new Rect(Screen.width - 132, 185, 108, 60);
            GUI.enabled = panel == Panel.None;
            if (GUI.Button(iconRect, "♥  인물\n관계 수첩", buttonStyle)) panel = Panel.Relationships;
            GUI.enabled = true;
        }

        private void DrawMapIcon()
        {
            Rect iconRect = new Rect(Screen.width - 132, 253, 108, 54);
            GUI.enabled = panel == Panel.None;
            if (GUI.Button(iconRect, "▦  캠퍼스\n지도  M", buttonStyle)) panel = Panel.CampusMap;
            GUI.enabled = true;
        }

        private void DrawCampusMap()
        {
            Rect r = CenterRect(760, 570);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 28, r.y + 20, 420, 34), "새빛대학교 캠퍼스 지도", titleStyle);
            GUI.Label(new Rect(r.x + 510, r.y + 25, 200, 26), "● 총장 현재 위치", smallStyle);

            Rect map = new Rect(r.x + 58, r.y + 70, 645, 430);
            GUI.Box(map, GUIContent.none, bodyStyle);
            if (campusMapTexture != null) GUI.DrawTexture(map, campusMapTexture, ScaleMode.StretchToFill, false);
            string hoveredBuilding = null;
            hoveredBuilding = DrawMapBuilding(map, -13.7f, 0.3f, 14.1f, 18.5f, "기숙사") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, 2.8f, 13.8f, 12.5f, 17.9f, "구관") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, -23.8f, -14.8f, 7.0f, 12.8f, "도서관") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, -5.9f, 8.6f, 7.2f, 12.5f, "본관·총장실") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, 12.4f, 25.1f, 4.9f, 11.9f, "학생회관") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, -26.2f, -14.8f, -0.9f, 5.1f, "인문사회관") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, -25.5f, -14.7f, -8.5f, -1.8f, "공학·자연과학관") ?? hoveredBuilding;
            hoveredBuilding = DrawMapBuilding(map, 7.6f, 16.8f, -11.3f, -2.4f, "체육관") ?? hoveredBuilding;

            if (!insideOffice)
            {
                Vector2 playerPoint = WorldToMap(map, player.position);
                GUI.Label(new Rect(playerPoint.x - 18, playerPoint.y - 18, 80, 30), "● 총장", titleStyle);
            }
            else
            {
                GUI.Label(new Rect(map.x + 235, map.y + 198, 240, 30), "현재 위치: 본관 총장실", titleStyle);
            }

            if (!string.IsNullOrEmpty(hoveredBuilding))
            {
                Vector2 mouse = Event.current.mousePosition;
                float tipX = Mathf.Min(mouse.x + 14, Screen.width - 245);
                float tipY = Mathf.Min(mouse.y + 14, Screen.height - 105);
                GUI.Box(new Rect(tipX, tipY, 230, 86), GUIContent.none, bodyStyle);
                GUI.Label(new Rect(tipX + 12, tipY + 9, 206, 25), hoveredBuilding, titleStyle);
                GUI.Label(new Rect(tipX + 12, tipY + 37, 206, 40), BuildingDescription(hoveredBuilding), smallStyle);
            }

            GUI.Label(new Rect(r.x + 35, r.y + 510, 450, 28), "지도는 시간이 멈춘 상태에서 확인합니다.", smallStyle);
            if (GUI.Button(new Rect(r.x + r.width - 145, r.y + r.height - 56, 115, 34), "닫기", buttonStyle)) panel = Panel.None;
        }

        private string DrawMapBuilding(Rect map, float minX, float maxX, float minY, float maxY, string label)
        {
            Vector2 topLeft = WorldToMap(map, new Vector2(minX, maxY));
            Vector2 bottomRight = WorldToMap(map, new Vector2(maxX, minY));
            Rect building = new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
            return building.Contains(Event.current.mousePosition) ? label : null;
        }

        private string BuildingDescription(string building)
        {
            if (building == "본관·총장실") return "학교 운영과 결재 업무를 처리하는 곳";
            if (building == "도서관") return "자료 열람과 조용한 만남이 가능한 곳";
            if (building == "학생회관") return "식당과 동아리 공간이 모여 있는 곳";
            if (building == "기숙사") return "학생들이 생활하는 거주 구역";
            if (building == "구관") return "현재 폐쇄되어 출입할 수 없는 건물";
            if (building == "체육관") return "수업과 교내 체육 행사가 열리는 곳";
            return "수업과 연구가 이루어지는 강의동";
        }

        private Vector2 WorldToMap(Rect map, Vector2 world)
        {
            float x = map.x + Mathf.InverseLerp(-30f, 30f, world.x) * map.width;
            float y = map.y + (1f - Mathf.InverseLerp(-20f, 20f, world.y)) * map.height;
            return new Vector2(x, y);
        }

        private void DrawMiniHelp()
        {
            GUI.Label(new Rect(Screen.width - 325, 20, 305, 78), "이동  WASD / 방향키\n대화  E 또는 Space   결재함  Tab\n인벤토리 I   관계 R   지도 M", smallStyle);
        }

        private void DrawInteractionPrompt()
        {
            GUI.Label(new Rect(Screen.width / 2 - 135, Screen.height - 82, 270, 42), $"E  {activeNpc.Name}에게 말 걸기", buttonStyle);
        }

        private void DrawDoorPrompt()
        {
            bool nearOutside = !insideOffice && Vector2.Distance(player.position, officeDoorOutside) < 1.45f;
            bool nearInside = insideOffice && Vector2.Distance(player.position, officeDoorInside) < 1.35f;
            if (!nearOutside && !nearInside) return;
            string prompt = nearInside ? "E  캠퍼스로 나가기" : "E  총장실 들어가기";
            GUI.Label(new Rect(Screen.width / 2 - 135, Screen.height - 82, 270, 42), prompt, buttonStyle);
        }

        private void DrawShopPrompt()
        {
            if (insideOffice || Vector2.Distance(player.position, campusShopOutside) >= 1.55f) return;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 132, 300, 42), "E  학생회관 편의점 이용", buttonStyle);
        }

        private bool CanMoveTo(Vector3 candidate)
        {
            if (insideOffice)
            {
                // 중앙 책상과 오른쪽 응접 소파를 통과하지 못하게 한다.
                if (InsideRect(candidate, 47.4f, 52.6f, -0.35f, 1.8f)) return false;
                if (InsideRect(candidate, 53.0f, 56.6f, -1.4f, 1.5f)) return false;
                if (InsideRect(candidate, 42.8f, 45.5f, -2.8f, 4.0f)) return false;
                return true;
            }
            foreach (Rect collider in buildingColliders)
                if (collider.Contains(candidate)) return false;
            return true;
        }

        private bool InsideRect(Vector3 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x > minX && point.x < maxX && point.y > minY && point.y < maxY;
        }

        private void EnterOffice()
        {
            if (!officeBuilt) BuildOfficeInterior();
            insideOffice = true;
            activeNpc = null;
            player.position = new Vector3(50f, -4.45f, 0);
            cam.transform.position = new Vector3(50f, 0f, -10);
            cam.orthographicSize = 6.2f;
        }

        private void ExitOffice()
        {
            insideOffice = false;
            player.position = new Vector3(0f, 6.0f, 0);
            cam.transform.position = new Vector3(0f, 6f, -10);
            cam.orthographicSize = 6.7f;
        }

        private void BuildOfficeInterior()
        {
            officeBuilt = true;
            Texture2D officeTexture = Resources.Load<Texture2D>("content/world/interiors/president-office-v1");
            if (officeTexture == null)
            {
                BuildOfficeFallback();
                return;
            }

            officeTexture.filterMode = FilterMode.Point;
            Sprite officeSprite = Sprite.Create(
                officeTexture,
                new Rect(0, 0, officeTexture.width, officeTexture.height),
                new Vector2(0.5f, 0.5f),
                85.333f,
                0,
                SpriteMeshType.FullRect);
            GameObject room = new GameObject("President Office Pixel Art");
            room.transform.position = new Vector3(50f, 0f, 0f);
            SpriteRenderer renderer = room.AddComponent<SpriteRenderer>();
            renderer.sprite = officeSprite;
            renderer.sortingOrder = -9;
        }

        private void BuildOfficeFallback()
        {
            CreateBlock("Office Floor", new Vector2(50, 0), new Vector2(12, 8), new Color32(211, 177, 124, 255), -9);
            CreateBlock("President Desk", new Vector2(50, 0.7f), new Vector2(3.8f, 1.4f), new Color32(105, 65, 42, 255), -2);
            CreateBlock("Bookshelf", new Vector2(45.5f, 2.5f), new Vector2(1.1f, 3f), new Color32(91, 59, 42, 255), -3);
            CreateBlock("Sofa", new Vector2(53.5f, 0.3f), new Vector2(2.2f, 1.8f), new Color32(73, 124, 110, 255), -3);
        }

        private void DrawDialogue()
        {
            Rect r = CenterRect(820, 252);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 28, r.y + 24, 555, 34), $"{activeNpc.Name} · {activeNpc.Role}", titleStyle);
            GUI.Label(new Rect(r.x + 28, r.y + 69, 555, 82), showingGiftReaction ? dialogueOverride : activeNpc.Greeting, bodyStyle);
            Rect portraitFrame = new Rect(r.x + 610, r.y + 22, 180, 190);
            GUI.Box(portraitFrame, GUIContent.none, bodyStyle);
            DrawNpcPortrait(new Rect(portraitFrame.x + 10, portraitFrame.y + 10, 160, 145), activeNpc);
            GUI.Label(new Rect(portraitFrame.x + 12, portraitFrame.y + 160, 156, 24), activeNpc.Name, titleStyle);
            if (!showingGiftReaction)
            {
                if (GUI.Button(new Rect(r.x + 28, r.y + 164, 190, 48), "선물 주기", buttonStyle)) { choosingGift = true; panel = Panel.Inventory; }
                if (GUI.Button(new Rect(r.x + 235, r.y + 164, 120, 48), "대화 끝내기", buttonStyle)) panel = Panel.None;
            }
            else if (GUI.Button(new Rect(r.x + 28, r.y + 174, 150, 42), "대화 끝내기", buttonStyle))
            {
                showingGiftReaction = false;
                dialogueOverride = "";
                panel = Panel.None;
            }
        }

        private void DrawNpcPortrait(Rect rect, Npc npc)
        {
            if (npc.Portrait != null)
            {
                GUI.DrawTexture(rect, npc.Portrait, ScaleMode.ScaleToFit, true);
                return;
            }

            SpriteRenderer npcRenderer = npc.Transform.GetComponent<SpriteRenderer>();
            if (npcRenderer == null || npcRenderer.sprite == null) return;
            // 전용 초상화가 없을 때는 16×24 캐릭터의 얼굴 부분을 임시 확대한다.
            GUI.DrawTextureWithTexCoords(rect, npcRenderer.sprite.texture, new Rect(0f, 0.62f, 1f, 0.38f), true);
        }

        private void DrawInventory()
        {
            Rect r = CenterRect(760, 560);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 28, r.y + 24, r.width - 56, 34), choosingGift ? $"{activeNpc.Name}에게 줄 선물" : "총장 가방", titleStyle);
            GUI.Label(new Rect(r.x + 28, r.y + 57, r.width - 56, 24), choosingGift ? "줄 아이템을 가방에서 선택하세요." : "가방", smallStyle);

            const int columns = 10;
            const int rows = 3;
            const float slot = 62f;
            int index = 0;
            foreach (KeyValuePair<string, int> item in new List<KeyValuePair<string, int>>(inventory))
            {
                int col = index % columns;
                int row = index / columns;
                Rect itemRect = new Rect(r.x + 68 + col * slot, r.y + 88 + row * slot, 56, 56);
                GUI.enabled = !choosingGift || item.Value > 0;
                if (DrawInventorySlot(itemRect, item.Key, item.Value) && choosingGift) GiveGift(item.Key);
                GUI.enabled = true;
                index++;
            }
            for (; index < columns * rows; index++)
            {
                int col = index % columns;
                int row = index / columns;
                GUI.Box(new Rect(r.x + 68 + col * slot, r.y + 88 + row * slot, 56, 56), GUIContent.none, bodyStyle);
            }

            GUI.Box(new Rect(r.x + 68, r.y + 294, 145, 185), GUIContent.none, bodyStyle);
            Texture2D playerPortrait = player.GetComponent<SpriteRenderer>().sprite.texture;
            GUI.DrawTexture(new Rect(r.x + 101, r.y + 314, 80, 120), playerPortrait, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(r.x + 105, r.y + 444, 100, 24), "새빛대 총장", smallStyle);
            GUI.Box(new Rect(r.x + 232, r.y + 294, 460, 185), GUIContent.none, bodyStyle);
            GUI.Label(new Rect(r.x + 254, r.y + 316, 410, 30), "총장 정보", titleStyle);
            GUI.Label(new Rect(r.x + 254, r.y + 358, 410, 92), $"개인 소지금  {wallet:N0}원\n총장 레벨  {1 + presidentXp / 50}\n평판  {reputation}   행복  {happiness}%", bodyStyle);
            if (GUI.Button(new Rect(r.x + r.width - 145, r.y + r.height - 56, 115, 34), "닫기", buttonStyle)) { choosingGift = false; panel = Panel.None; }
        }

        private void DrawShop()
        {
            Rect r = CenterRect(800, 590);
            DrawModal(r);
            GUI.Box(new Rect(r.x + 24, r.y + 24, 180, 205), GUIContent.none, bodyStyle);
            DrawShopkeeperPortrait(new Rect(r.x + 44, r.y + 40, 140, 140));
            GUI.Label(new Rect(r.x + 54, r.y + 185, 130, 25), "편의점 직원", titleStyle);
            GUI.Box(new Rect(r.x + 24, r.y + 242, 180, 105), GUIContent.none, bodyStyle);
            GUI.Label(new Rect(r.x + 38, r.y + 256, 152, 78), "어서 오세요!\n필요한 물건을\n골라주세요.", bodyStyle);

            GUI.Label(new Rect(r.x + 226, r.y + 24, 350, 34), "학생회관 새빛 편의점", titleStyle);
            GUI.Label(new Rect(r.x + 605, r.y + 29, 155, 26), $"{wallet:N0}원", smallStyle);

            string[] items = { "따뜻한 커피", "허브차", "수제 쿠키", "매운 생선빵" };
            int[] prices = { 3500, 2800, 4200, 3000 };
            for (int i = 0; i < items.Length; i++)
            {
                Rect itemRect = new Rect(r.x + 226, r.y + 70 + i * 66, 540, 58);
                GUI.enabled = wallet >= prices[i];
                if (GUI.Button(itemRect, GUIContent.none, buttonStyle))
                    BuyItem(items[i], prices[i]);
                DrawItemIcon(new Rect(itemRect.x + 7, itemRect.y + 5, 48, 48), items[i]);
                GUI.Label(new Rect(itemRect.x + 66, itemRect.y + 16, 285, 28), items[i], titleStyle);
                GUI.Label(new Rect(itemRect.x + 405, itemRect.y + 17, 115, 24), $"{prices[i]:N0}원", titleStyle);
                GUI.enabled = true;
            }

            GUI.Label(new Rect(r.x + 226, r.y + 348, 250, 27), "내 가방", titleStyle);
            GUI.Box(new Rect(r.x + 226, r.y + 378, 540, 130), GUIContent.none, bodyStyle);
            int bagIndex = 0;
            foreach (KeyValuePair<string, int> item in inventory)
            {
                Rect slotRect = new Rect(r.x + 239 + (bagIndex % 8) * 64, r.y + 387 + (bagIndex / 8) * 62, 56, 56);
                DrawInventorySlot(slotRect, item.Key, item.Value);
                bagIndex++;
            }
            for (; bagIndex < 16; bagIndex++)
                GUI.Box(new Rect(r.x + 239 + (bagIndex % 8) * 64, r.y + 387 + (bagIndex / 8) * 62, 56, 56), GUIContent.none, bodyStyle);
            if (GUI.Button(new Rect(r.x + r.width - 145, r.y + r.height - 56, 115, 34), "나가기", buttonStyle)) panel = Panel.None;
        }

        private void DrawShopkeeperPortrait(Rect rect)
        {
            if (shopkeeperPortrait == null) return;
            if (hasCustomShopkeeperPortrait)
                GUI.DrawTexture(rect, shopkeeperPortrait, ScaleMode.ScaleToFit, true);
            else
                GUI.DrawTextureWithTexCoords(rect, shopkeeperPortrait, new Rect(0f, 0.62f, 1f, 0.38f), true);
        }

        private bool DrawInventorySlot(Rect slotRect, string item, int count)
        {
            bool clicked = GUI.Button(slotRect, GUIContent.none, buttonStyle);
            DrawItemIcon(new Rect(slotRect.x + 5, slotRect.y + 4, slotRect.width - 10, slotRect.height - 10), item);
            GUIStyle countStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            countStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(slotRect.x + 4, slotRect.y + 29, slotRect.width - 8, 22), count.ToString(), countStyle);
            return clicked;
        }

        private void BuyItem(string item, int price)
        {
            if (wallet < price) return;
            wallet -= price;
            inventory[item]++;
            SaveGame();
            ShowToast($"{item}을(를) 샀습니다.");
        }

        private void DrawRelationships()
        {
            Rect r = CenterRect(680, 500);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 30, r.y + 25, r.width - 60, 36), "새빛대학교 인물 관계 수첩", titleStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 66, r.width - 60, 28), "관계의 변화는 평소에 감추고, 이곳에서만 확인합니다.", smallStyle);
            for (int i = 0; i < npcs.Count; i++)
            {
                Npc npc = npcs[i];
                float y = r.y + 112 + i * 72;
                GUI.Box(new Rect(r.x + 30, y, r.width - 60, 58), GUIContent.none, bodyStyle);
                GUI.Label(new Rect(r.x + 48, y + 7, 230, 24), npc.Name, titleStyle);
                GUI.Label(new Rect(r.x + 48, y + 32, 320, 20), $"{npc.Role} · {npc.CurrentActivity}", smallStyle);
                GUI.Label(new Rect(r.x + 390, y + 12, 220, 34), HeartDisplay(npc.Affection), titleStyle);
            }
            if (GUI.Button(new Rect(r.x + r.width - 145, r.y + r.height - 55, 115, 34), "닫기", buttonStyle)) panel = Panel.None;
        }

        private string GiftIcon(string item)
        {
            if (item.Contains("커피")) return "☕";
            if (item.Contains("차")) return "♨";
            if (item.Contains("쿠키")) return "◆";
            return "▲";
        }

        private void DrawItemIcon(Rect rect, string item)
        {
            if (itemIconAtlas == null)
            {
                GUI.Label(rect, GiftIcon(item), titleStyle);
                return;
            }

            Rect uv;
            if (item == "따뜻한 커피") uv = new Rect(0f, 0.5f, 0.5f, 0.5f);
            else if (item == "허브차") uv = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            else if (item == "수제 쿠키") uv = new Rect(0f, 0f, 0.5f, 0.5f);
            else uv = new Rect(0.5f, 0f, 0.5f, 0.5f);
            GUI.DrawTextureWithTexCoords(rect, itemIconAtlas, uv, true);
        }

        private void GiveGift(string item)
        {
            if (inventory[item] <= 0) return;
            inventory[item]--;
            int change = item == activeNpc.FavoriteGift ? 20 : item == activeNpc.HatedGift ? -10 : 5;
            activeNpc.Affection += change;
            presidentXp += change > 0 ? 5 : 0;
            speechXp += change > 0 ? 4 : 1;
            choosingGift = false;
            showingGiftReaction = true;
            dialogueOverride = GiftReaction(activeNpc.Name, change);
            panel = Panel.Dialogue;
        }

        private string GiftReaction(string npcName, int change)
        {
            if (npcName == "김하늘")
            {
                if (change == 20) return "어, 이거 제가 정말 좋아하는 건데! 총장님이 이런 것도 기억해 주실 줄 몰랐어요. 잘 먹을게요!";
                if (change < 0) return "아… 고맙긴 한데 저는 이건 조금 어려워요. 다른 사람에게 주는 게 더 좋았을지도요.";
                return "제 생각을 하고 골라주신 거예요? 고마워요, 총장님!";
            }
            if (npcName == "박지훈")
            {
                if (change == 20) return "마침 따뜻한 커피가 간절했는데요. 오늘 야간 정리도 힘내서 할 수 있겠습니다. 감사합니다.";
                if (change < 0) return "마음은 감사하지만… 도서관 안에서는 냄새가 강한 음식은 조금 곤란하군요.";
                return "업무 중에 잘 먹겠습니다. 이렇게 챙겨주셔서 감사합니다.";
            }
            if (npcName == "최미숙")
            {
                if (change == 20) return "어머, 따뜻한 차네. 쉬는 시간에 동료들이랑 나눠 마실게요. 이런 마음이 참 고맙지.";
                if (change < 0) return "총장님 마음은 고마운데 커피를 마시면 밤에 잠을 못 자서 말이야. 그래도 신경 써준 건 고마워요.";
                return "아이고, 나까지 챙겨주고. 고맙게 잘 받을게요, 총장님.";
            }
            if (change == 20) return "이걸 좋아한다는 걸 어떻게 아셨습니까? 연구실에서 아주 유용하게 먹겠습니다.";
            if (change < 0) return "호의는 감사합니다만, 단 음식은 즐기지 않습니다. 다음에는 마음만 받겠습니다.";
            return "감사합니다. 다음 회의 때 함께 나누도록 하지요.";
        }

        private void DrawApproval()
        {
            Rect r = CenterRect(720, 390);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 30, r.y + 24, r.width - 60, 35), "오늘의 긴급 결재", titleStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 66, r.width - 60, 28), "환경미화 노동자 임금 조정안", titleStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 106, r.width - 60, 100), "3년 동안 동결된 임금을 8% 인상해 달라는 요청입니다.\n승인하면 올해 예산 부담이 늘지만 노동 환경과 구성원 신뢰가 개선됩니다.\n보류하면 당장 지출은 없지만 불만이 누적됩니다.", bodyStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 220, r.width - 60, 28), approvalResolved ? "처리 완료 — 내일 결과가 전달됩니다." : "마감: 오늘 오후 8시", smallStyle);
            GUI.enabled = !approvalResolved;
            int fullRaiseCost = NegotiatedCost(450);
            int phasedRaiseCost = NegotiatedCost(250);
            if (GUI.Button(new Rect(r.x + 30, r.y + 270, 200, 52), $"승인  −{fullRaiseCost}억", buttonStyle))
            {
                money -= fullRaiseCost; happiness += 6; reputation += 2; approvalsDone++; presidentXp += 20; financeXp += 12; approvalResolved = true;
                ShowToast("임금 인상안을 승인했습니다. 구성원 신뢰가 올랐습니다.");
            }
            if (GUI.Button(new Rect(r.x + 250, r.y + 270, 200, 52), $"수정안  −{phasedRaiseCost}억", buttonStyle))
            {
                money -= phasedRaiseCost; happiness += 3; approvalsDone++; presidentXp += 16; financeXp += 6; negotiationXp += 14; approvalResolved = true;
                ShowToast("단계적 인상안을 제시했습니다.");
            }
            if (GUI.Button(new Rect(r.x + 470, r.y + 270, 200, 52), "내일로 보류", buttonStyle))
            {
                happiness -= 4; reputation -= 1; approvalsDone++; presidentXp += 5; financeXp += 3; approvalResolved = true;
                ShowToast("안건을 보류했습니다. 불만이 쌓이기 시작합니다.");
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(r.x + r.width - 140, r.y + 335, 110, 34), "닫기", buttonStyle)) panel = Panel.None;
        }

        private void DrawEvent()
        {
            Rect r = CenterRect(690, 330);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 28, r.y + 25, r.width - 56, 34), "돌발 사건 · 수상한 전도 활동", titleStyle);
            GUI.Label(new Rect(r.x + 28, r.y + 70, r.width - 56, 85), "중앙광장에서 외부 단체가 허가 없이 학생들에게 접근하고 있습니다.\n몇몇 학생이 불편함을 호소하지만 아직 큰 충돌은 없습니다.", bodyStyle);
            if (GUI.Button(new Rect(r.x + 28, r.y + 180, 195, 58), "학생지원팀과\n현장으로 간다", buttonStyle)) ResolveEvent(6, 3, 18, "학생들의 이야기를 듣고 안전하게 상황을 정리했습니다.");
            if (GUI.Button(new Rect(r.x + 246, r.y + 180, 195, 58), "경비팀에\n규정대로 맡긴다", buttonStyle)) ResolveEvent(1, 1, 10, "경비팀이 캠퍼스 규정에 따라 퇴거를 요청했습니다.");
            if (GUI.Button(new Rect(r.x + 464, r.y + 180, 195, 58), "일단 지켜본다", buttonStyle)) ResolveEvent(-5, -3, 4, "대응이 늦어져 학생들의 불안이 커졌습니다.");
        }

        private void ResolveEvent(int happy, int rep, int xp, string message)
        {
            happiness += happy; reputation += rep; presidentXp += xp; panel = Panel.None;
            negotiationXp += xp >= 10 ? 8 : 3;
            nextEventDay = day + UnityEngine.Random.Range(12, 18);
            ShowToast(message);
        }

        private void DrawSummary()
        {
            Rect r = CenterRect(720, 510);
            DrawModal(r);
            GUI.Label(new Rect(r.x + 32, r.y + 26, r.width - 64, 38), $"봄 {day}일 · 오늘의 총장 일지", titleStyle);
            GUI.Label(new Rect(r.x + 32, r.y + 82, r.width - 64, 230),
                $"캠퍼스 대화                         {conversations}회\n처리한 결재                           {approvalsDone}건\n돌발 사건 해결                       {(eventTriggered ? "완료" : "발견하지 못함")}\n\n총장 경험치                           +{presidentXp}\n현재 예산                              {money:N0}억\n대학 평판                              {reputation}점\n학생·교직원 행복도                 {happiness}%", bodyStyle);
            GUI.Label(new Rect(r.x + 32, r.y + 315, r.width - 64, 60), AbilityNightMessage(), smallStyle);
            GUI.Label(new Rect(r.x + 32, r.y + 370, r.width - 64, 28), "내일: 중앙도서관 좌석 부족 문제를 확인해 보세요.", smallStyle);
            if (GUI.Button(new Rect(r.x + 32, r.y + 400, r.width - 64, 62), "저장하고 다음 날로  →", buttonStyle)) NextDay();
        }

        private void NextDay()
        {
            CompleteNextDay();
        }

        private void CompleteNextDay()
        {
            day++; gameMinutes = 0; minuteTimer = 0; conversations = 0; approvalsDone = 0; eventTriggered = false; approvalResolved = false;
            foreach (Npc npc in npcs) npc.TalkedToday = false;
            foreach (Npc npc in npcs)
            {
                npc.Transform.position = npc.Schedule[0].Position;
                npc.CurrentActivity = npc.Schedule[0].Activity;
            }
            player.position = new Vector3(-1, -1, 0); panel = Panel.None;
            SaveGame();
            RememberAbilityLevels();
            ShowToast($"봄 {day}일 아침입니다. 진행 상황이 자동 저장되었습니다.");
        }

        private int NegotiatedCost(int original)
        {
            return original;
        }

        private int AbilityLevel(int xp) { return xp / 40; }

        private void RememberAbilityLevels()
        {
            dayStartSpeechLevel = AbilityLevel(speechXp);
            dayStartFinanceLevel = AbilityLevel(financeXp);
            dayStartNegotiationLevel = AbilityLevel(negotiationXp);
            dayStartSpeechXp = speechXp;
            dayStartFinanceXp = financeXp;
            dayStartNegotiationXp = negotiationXp;
        }

        private string AbilityNightMessage()
        {
            List<string> messages = new List<string>();
            if (AbilityLevel(speechXp) > dayStartSpeechLevel) messages.Add("언변 스킬 포인트가 올랐습니다!");
            else if (speechXp > dayStartSpeechXp) messages.Add("오늘의 대화로 언변 경험이 쌓였습니다.");
            if (AbilityLevel(financeXp) > dayStartFinanceLevel) messages.Add("재정 스킬 포인트가 올랐습니다!");
            else if (financeXp > dayStartFinanceXp) messages.Add("오늘의 결재로 재정 경험이 쌓였습니다.");
            if (AbilityLevel(negotiationXp) > dayStartNegotiationLevel) messages.Add("협상 스킬 포인트가 올랐습니다!");
            else if (negotiationXp > dayStartNegotiationXp) messages.Add("오늘의 선택으로 협상 경험이 쌓였습니다.");
            return messages.Count == 0 ? "오늘 새롭게 성장한 능력은 없습니다." : string.Join("  ·  ", messages);
        }

        private string HeartDisplay(int affection)
        {
            int filled = Mathf.Clamp(affection / 20, 0, 5);
            return new string('♥', filled) + new string('♡', 5 - filled);
        }

        private void SaveGame()
        {
            SaveData data = new SaveData
            {
                day = day,
                money = money,
                reputation = reputation,
                happiness = happiness,
                presidentXp = presidentXp,
                speechXp = speechXp,
                financeXp = financeXp,
                negotiationXp = negotiationXp,
                nextEventDay = nextEventDay,
                wallet = wallet,
            };
            foreach (KeyValuePair<string, int> item in inventory)
            {
                data.itemNames.Add(item.Key);
                data.itemCounts.Add(item.Value);
            }
            foreach (Npc npc in npcs)
            {
                data.npcNames.Add(npc.Name);
                data.npcAffection.Add(npc.Affection);
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void LoadGame()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (data == null || data.day < 1) return;
            day = data.day;
            money = data.money;
            reputation = data.reputation;
            happiness = data.happiness;
            presidentXp = data.presidentXp;
            speechXp = data.speechXp;
            financeXp = data.financeXp;
            negotiationXp = data.negotiationXp;
            wallet = data.wallet > 0 ? data.wallet : 50000;
            nextEventDay = Mathf.Max(6, data.nextEventDay);
            for (int i = 0; i < data.itemNames.Count && i < data.itemCounts.Count; i++)
                if (inventory.ContainsKey(data.itemNames[i])) inventory[data.itemNames[i]] = Mathf.Max(0, data.itemCounts[i]);
            for (int i = 0; i < data.npcNames.Count && i < data.npcAffection.Count; i++)
            {
                Npc npc = npcs.Find(candidate => candidate.Name == data.npcNames[i]);
                if (npc != null) npc.Affection = data.npcAffection[i];
            }
            ShowToast($"봄 {day}일 저장 데이터를 불러왔습니다.");
        }

        private void DrawToast()
        {
            GUI.Label(new Rect(Screen.width / 2 - 280, 22, 560, 44), toast, buttonStyle);
        }

        private void DrawModal(Rect r)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            GUI.DrawTexture(r, panelTexture);
            GUI.Box(r, GUIContent.none, bodyStyle);
        }

        private void ShowToast(string message) { toast = message; toastUntil = Time.time + 4.5f; }
        private string ClockText() { int total = 480 + Mathf.RoundToInt(gameMinutes); return $"{total / 60:00}:{total % 60:00}"; }
        private Rect CenterRect(float width, float height) { return new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height); }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            panelTexture = SolidTexture(new Color32(255, 248, 225, 255));
            darkTexture = SolidTexture(new Color(0.04f, 0.08f, 0.07f, 0.72f));
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color32(31, 67, 57, 255) } };
            bodyStyle = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(10, 10, 8, 8), normal = { textColor = new Color32(39, 69, 61, 255), background = panelTexture } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = new Color32(67, 92, 84, 255) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color32(255, 248, 225, 255), background = SolidTexture(new Color32(38, 91, 77, 255)) }, hover = { textColor = Color.white, background = SolidTexture(new Color32(54, 117, 96, 255)) } };
        }

        private void AddNpc(string name, string role, string greeting, string favoriteGift, string hatedGift, Vector2 morningPosition, Vector2 lunchPosition, Vector2 afternoonPosition, Color color)
        {
            Npc npc = new Npc
            {
                Name = name,
                Role = role,
                Greeting = greeting,
                FavoriteGift = favoriteGift,
                HatedGift = hatedGift,
                Affection = 10,
                MoveSpeed = 1.5f + (Mathf.Abs(name.GetHashCode()) % 5) * 0.08f,
                Transform = CreatePerson(name, morningPosition, color, false)
            };
            npc.Schedule = BuildSchedule(name, morningPosition, lunchPosition, afternoonPosition);
            npc.CurrentActivity = npc.Schedule[0].Activity;
            npc.Portrait = Resources.Load<Texture2D>($"content/npc/portraits/{PortraitFileName(name)}-v1");
            if (npc.Portrait != null) npc.Portrait.filterMode = FilterMode.Point;
            npcs.Add(npc);
        }

        private string PortraitFileName(string npcName)
        {
            if (npcName == "김하늘") return "kim-haneul";
            if (npcName == "박지훈") return "park-jihun";
            if (npcName == "최미숙") return "choi-misuk";
            if (npcName == "이도윤") return "lee-doyun";
            return "unknown";
        }

        private List<ScheduleStop> BuildSchedule(string name, Vector2 morning, Vector2 lunch, Vector2 afternoon)
        {
            if (name == "김하늘")
                return new List<ScheduleStop>
                {
                    new ScheduleStop(480, morning, "등교 중"),
                    new ScheduleStop(540, new Vector2(-18.0f, -3.8f), "오전 수업"),
                    new ScheduleStop(720, lunch, "학생식당에서 점심"),
                    new ScheduleStop(780, new Vector2(0f, 0.5f), "중앙광장에서 휴식"),
                    new ScheduleStop(900, afternoon, "오후 수업"),
                    new ScheduleStop(1020, new Vector2(15.0f, 3.0f), "동아리 활동"),
                    new ScheduleStop(1080, new Vector2(-12.0f, 11.8f), "기숙사로 귀가")
                };
            if (name == "이도윤")
                return new List<ScheduleStop>
                {
                    new ScheduleStop(480, morning, "연구 준비"),
                    new ScheduleStop(600, new Vector2(-17.0f, 4.0f), "도서관 자료 조사"),
                    new ScheduleStop(720, lunch, "교직원 식당에서 점심"),
                    new ScheduleStop(810, new Vector2(-19.0f, -9.2f), "강의 준비"),
                    new ScheduleStop(900, afternoon, "오후 강의"),
                    new ScheduleStop(1020, new Vector2(-15.0f, -1.0f), "학생 상담"),
                    new ScheduleStop(1080, new Vector2(0f, -19.5f), "퇴근")
                };
            if (name == "박지훈")
                return new List<ScheduleStop>
                {
                    new ScheduleStop(480, morning, "도서관 개관 준비"),
                    new ScheduleStop(600, new Vector2(-19.0f, 6.15f), "자료 정리"),
                    new ScheduleStop(720, lunch, "직원 휴게실에서 점심"),
                    new ScheduleStop(780, morning, "대출대 근무"),
                    new ScheduleStop(960, new Vector2(-20.5f, 6.15f), "열람실 점검"),
                    new ScheduleStop(1080, new Vector2(0f, -19.5f), "퇴근")
                };
            return new List<ScheduleStop>
            {
                new ScheduleStop(480, morning, "아침 청소"),
                new ScheduleStop(600, new Vector2(-16.0f, -5.0f), "강의동 담당 구역"),
                new ScheduleStop(720, lunch, "휴게실에서 점심"),
                new ScheduleStop(780, new Vector2(13.0f, 2.0f), "학생회관 담당 구역"),
                new ScheduleStop(960, afternoon, "중앙광장 정리"),
                new ScheduleStop(1050, new Vector2(0f, -8.0f), "도구 정리"),
                new ScheduleStop(1080, new Vector2(0f, -19.5f), "퇴근")
            };
        }

        private Transform CreatePerson(string name, Vector2 position, Color clothes, bool isPlayer)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            SpriteRenderer shadow = CreateBlock(name + " shadow", position + Vector2.down * 0.56f, new Vector2(0.9f, 0.28f), new Color(0.08f, 0.12f, 0.09f, 0.28f), 2).GetComponent<SpriteRenderer>();
            shadow.transform.SetParent(root.transform);
            shadow.transform.localPosition = new Vector3(0, -0.56f, 0);

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = PersonSprite(clothes, isPlayer, Mathf.Abs(name.GetHashCode()) % 4);
            renderer.sortingOrder = 5;
            return root.transform;
        }

        private Sprite PersonSprite(Color clothes, bool isPlayer, int variant)
        {
            const int width = 16;
            const int height = 24;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color32(42, 48, 42, 255);
            Color skin = new Color32(239, 190, 148, 255);
            Color skinShade = new Color32(205, 145, 111, 255);
            Color hair = isPlayer ? new Color32(43, 46, 39, 255) :
                variant == 0 ? new Color32(84, 49, 35, 255) :
                variant == 1 ? new Color32(44, 43, 40, 255) :
                variant == 2 ? new Color32(126, 73, 44, 255) : new Color32(67, 53, 78, 255);
            Color shirtShade = Color.Lerp(clothes, Color.black, 0.23f);
            Color pants = isPlayer ? new Color32(54, 62, 70, 255) : new Color32(67, 74, 78, 255);
            Color shoes = new Color32(53, 43, 39, 255);
            Color accent = isPlayer ? new Color32(235, 192, 75, 255) :
                variant == 0 ? new Color32(247, 225, 184, 255) :
                variant == 1 ? new Color32(115, 194, 188, 255) :
                variant == 2 ? new Color32(232, 149, 95, 255) : new Color32(225, 196, 102, 255);

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            void Fill(int x0, int y0, int x1, int y1, Color color)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        if (x >= 0 && x < width && y >= 0 && y < height) pixels[y * width + x] = color;
            }

            // 신발과 다리
            Fill(4, 1, 7, 2, shoes); Fill(9, 1, 12, 2, shoes);
            Fill(5, 3, 7, 7, pants); Fill(9, 3, 11, 7, pants);
            Fill(4, 7, 12, 8, outline);

            // 몸통 외곽선, 재킷과 팔
            Fill(3, 8, 13, 16, outline);
            Fill(4, 9, 12, 15, clothes);
            Fill(4, 13, 5, 15, shirtShade); Fill(11, 13, 12, 15, shirtShade);
            Fill(2, 10, 3, 14, outline); Fill(13, 10, 14, 14, outline);
            Fill(2, 11, 2, 13, skinShade); Fill(14, 11, 14, 13, skinShade);
            Fill(7, 9, 8, 14, accent);

            // 목과 얼굴 외곽선
            Fill(7, 16, 9, 17, skinShade);
            Fill(4, 16, 12, 22, outline);
            Fill(5, 16, 11, 21, skin);
            Fill(5, 16, 5, 18, skinShade); Fill(11, 16, 11, 18, skinShade);

            // 머리 모양은 직업/인물별로 변화
            Fill(4, 20, 12, 23, hair);
            Fill(4, 18, 5, 21, hair);
            if (variant == 1) Fill(11, 18, 12, 22, hair);
            if (variant == 2) { Fill(3, 19, 4, 22, hair); Fill(12, 19, 13, 22, hair); }
            if (variant == 3) Fill(5, 22, 11, 23, hair);

            // 눈, 코, 직업을 구분하는 작은 장식
            pixels[19 * width + 6] = outline;
            pixels[19 * width + 10] = outline;
            pixels[17 * width + 8] = skinShade;
            if (isPlayer)
            {
                Fill(6, 19, 7, 19, new Color32(238, 204, 95, 255));
                Fill(9, 19, 10, 19, new Color32(238, 204, 95, 255));
                Fill(6, 17, 10, 17, new Color32(224, 225, 211, 255));
            }
            else if (variant == 1)
            {
                Fill(5, 19, 7, 19, new Color32(80, 113, 122, 255));
                Fill(9, 19, 11, 19, new Color32(80, 113, 122, 255));
            }
            else if (variant == 2)
            {
                Fill(11, 13, 13, 14, accent);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.18f), 16, 0, SpriteMeshType.FullRect);
        }

        private void CreateBuilding(string name, Vector2 position, Vector2 size, Color wall)
        {
            buildingColliders.Add(new Rect(position.x - size.x * 0.5f - 0.18f, position.y - size.y * 0.5f - 0.18f, size.x + 0.36f, size.y + 0.36f));
            CreateBlock(name, position, size, wall, -3);
            CreateBlock(name + " roof", position + Vector2.up * (size.y * 0.42f), new Vector2(size.x + 0.4f, 0.8f), new Color32(46, 83, 68, 255), -2);
            CreateBlock(name + " door", position + Vector2.down * (size.y * 0.34f), new Vector2(0.8f, 1.2f), new Color32(107, 67, 49, 255), 0);
            for (int i = -1; i <= 1; i++) CreateBlock(name + " window", position + new Vector2(i * size.x * 0.25f, 0.25f), new Vector2(0.7f, 0.65f), new Color32(114, 169, 174, 255), -1);
        }

        private void CreateTree(Vector2 position)
        {
            CreateBlock("Tree trunk", position + Vector2.down * 0.42f, new Vector2(0.3f, 0.8f), new Color32(111, 72, 42, 255), -5);
            CreateBlock("Tree crown", position + Vector2.up * 0.25f, new Vector2(1.25f, 1.35f), new Color32(54, 119, 70, 255), -4);
        }

        private GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnvironmentSprite(name, color);
            sr.size = size;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.sortingOrder = order;
            if (!name.ToLowerInvariant().Contains("shadow")) proceduralEnvironmentRenderers.Add(sr);
            return go;
        }

        private void CreateChildBlock(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color, int order)
        {
            GameObject go = CreateBlock(name, Vector2.zero, size, color, order);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPosition;
        }

        private Sprite PixelSprite(Color color)
        {
            Texture2D texture = SolidTexture(color);
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8, 0, SpriteMeshType.FullRect, Vector4.one);
        }

        private Sprite EnvironmentSprite(string objectName, Color baseColor)
        {
            const int size = 16;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[size * size];
            Color light = Color.Lerp(baseColor, Color.white, 0.2f);
            Color shade = Color.Lerp(baseColor, Color.black, 0.22f);
            Color deep = Color.Lerp(baseColor, Color.black, 0.38f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = baseColor;

            void Dot(int x, int y, Color color)
            {
                if (x >= 0 && x < size && y >= 0 && y < size) pixels[y * size + x] = color;
            }
            void Fill(int x0, int y0, int x1, int y1, Color color)
            {
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Dot(x, y, color);
            }
            void Outline(Color color)
            {
                Fill(0, 0, 15, 0, color); Fill(0, 15, 15, 15, color);
                Fill(0, 0, 0, 15, color); Fill(15, 0, 15, 15, color);
            }

            string lower = objectName.ToLowerInvariant();
            if (lower.Contains("grass"))
            {
                Dot(2, 3, light); Dot(3, 4, light); Dot(10, 11, shade); Dot(11, 12, shade);
                Dot(7, 6, light); Dot(13, 2, shade); Dot(5, 14, shade);
            }
            else if (lower.Contains("path"))
            {
                Fill(1, 2, 4, 4, light); Fill(9, 1, 13, 3, shade);
                Fill(5, 9, 10, 12, light); Fill(12, 13, 15, 15, shade);
                Dot(3, 10, deep); Dot(14, 7, deep); Dot(8, 5, shade);
            }
            else if (lower.Contains("roof") || lower.Contains("top wall"))
            {
                for (int y = 1; y < 16; y += 4) Fill(0, y, 15, y, deep);
                for (int x = 3; x < 16; x += 6) Fill(x, 0, x, 15, shade);
                Outline(deep);
            }
            else if (lower.Contains("window"))
            {
                Outline(deep); Fill(2, 2, 13, 13, baseColor);
                Fill(3, 10, 7, 12, light); Fill(8, 3, 12, 5, light);
                Fill(7, 2, 8, 13, deep); Fill(2, 7, 13, 8, deep);
            }
            else if (lower.Contains("door") || lower.Contains("exit"))
            {
                Outline(deep); Fill(2, 2, 13, 13, baseColor);
                Fill(4, 9, 11, 10, shade); Dot(11, 7, new Color32(228, 185, 71, 255));
            }
            else if (lower.Contains("tree crown"))
            {
                Outline(deep); Fill(1, 1, 7, 7, light); Fill(8, 8, 14, 14, shade);
                Fill(9, 2, 13, 6, baseColor); Dot(4, 12, deep); Dot(12, 5, light);
            }
            else if (lower.Contains("tree trunk"))
            {
                for (int x = 2; x < 16; x += 5) Fill(x, 0, x + 1, 15, shade);
                Outline(deep);
            }
            else if (lower.Contains("floor"))
            {
                for (int y = 0; y < 16; y += 4) Fill(0, y, 15, y, shade);
                Dot(3, 2, deep); Dot(12, 6, deep); Dot(7, 10, deep); Dot(14, 14, deep);
            }
            else if (lower.Contains("rug"))
            {
                Outline(light); Fill(2, 2, 13, 13, baseColor);
                Fill(3, 3, 12, 3, shade); Fill(3, 12, 12, 12, shade);
                Dot(4, 7, light); Dot(11, 7, light);
            }
            else if (lower.Contains("desk") || lower.Contains("bookshelf") || lower.Contains("sofa"))
            {
                Outline(deep); Fill(2, 2, 13, 13, baseColor);
                Fill(2, 4, 13, 5, shade); Fill(2, 10, 13, 11, light);
            }
            else if (lower.Contains("wall") || lower.Contains("총장실") || lower.Contains("도서관") || lower.Contains("학생회관") || lower.Contains("강의동"))
            {
                for (int y = 3; y < 16; y += 5) Fill(0, y, 15, y, shade);
                for (int x = 4; x < 16; x += 8) Fill(x, 0, x, 3, shade);
                for (int x = 8; x < 16; x += 8) Fill(x, 4, x, 8, shade);
                for (int x = 4; x < 16; x += 8) Fill(x, 9, x, 13, shade);
                Outline(deep);
            }
            else if (lower.Contains("shadow"))
            {
                // 캐릭터 그림자는 단색을 유지한다.
            }
            else
            {
                Outline(deep);
                Fill(2, 2, 13, 13, baseColor);
                Fill(3, 11, 12, 12, light);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16, 0, SpriteMeshType.FullRect);
        }

        private Texture2D SolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
