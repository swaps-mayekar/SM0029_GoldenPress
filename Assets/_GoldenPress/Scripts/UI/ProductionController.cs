using System;
using System.Collections.Generic;
using GoldenPress.Core;
using GoldenPress.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoldenPress.UI
{
    public sealed class ProductionController : MonoBehaviour
    {
        private GameSession _session;
        private OilDefinition _oil;
        private RectTransform _root;
        private Text _headerText;
        private Text _hintText;
        private Text _scoreText;
        private GameObject _stageRoot;
        private Button _continueButton;
        private Button _retryButton;
        private Button _exitButton;

        private SortingMinigame _sorting;
        private ProcessingMinigame _processing;
        private BottlingMinigame _bottling;
        private bool _stageComplete;
        private float _lastScore;

        private void Start()
        {
            EnsureEventSystem();
            _session = GameContext.Instance.Session;
            var production = _session.State.productionSession;
            if (!_session.Production.HasActiveSession && production.currentStage != ProductionStage.Finished)
            {
                SceneManager.LoadScene(SceneNames.MainMill);
                return;
            }

            _oil = _session.Balance.GetOil(production.oilId);
            BuildUi();
            ShowCurrentStage();
        }

        private void BuildUi()
        {
            ArtCatalog.Warm();
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = Color.Lerp(GameTheme.Background, _oil.oilColor, 0.15f);
            }

            var canvas = UiFactory.CreateCanvas("ProductionCanvas", transform);
            _root = UiFactory.CreatePanel(canvas.transform, "SafeRoot", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.ApplySafeArea(_root);

            UiFactory.CreateFullscreenBackground(_root, ArtCatalog.ProductionBackground, GameTheme.Background);
            UiFactory.CreatePanel(_root, "SoftVeil", new Color(1f, 0.94f, 0.84f, 0.22f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var top = UiFactory.CreateFramedPanel(_root, "Top",
                new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            _headerText = UiFactory.CreateText(top, "Header", "", 30, GameTheme.TextDark, TextAnchor.MiddleLeft, FontStyle.Bold);
            _headerText.rectTransform.offsetMin = new Vector2(20, 0);
            _scoreText = UiFactory.CreateText(top, "Score", "", 26, GameTheme.TextMuted, TextAnchor.MiddleRight);
            _scoreText.rectTransform.offsetMax = new Vector2(-20, 0);

            _hintText = UiFactory.CreateText(_root, "Hint", "", 24, GameTheme.TextDark, TextAnchor.MiddleCenter);
            _hintText.rectTransform.anchorMin = new Vector2(0.1f, 0.76f);
            _hintText.rectTransform.anchorMax = new Vector2(0.9f, 0.85f);

            _stageRoot = UiFactory.CreatePanel(_root, "StageRoot", Color.clear,
                new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.75f), Vector2.zero, Vector2.zero).gameObject;

            _continueButton = UiFactory.CreateButton(_root, "Continue", "Continue", GameTheme.Success,
                new Vector2(0.55f, 0.03f), new Vector2(0.8f, 0.12f), Vector2.zero, Vector2.zero);
            _continueButton.onClick.AddListener(OnContinue);
            _continueButton.gameObject.SetActive(false);

            _retryButton = UiFactory.CreateButton(_root, "Retry", "Retry Stage", GameTheme.Accent,
                new Vector2(0.2f, 0.03f), new Vector2(0.45f, 0.12f), Vector2.zero, Vector2.zero);
            _retryButton.onClick.AddListener(OnRetry);
            _retryButton.gameObject.SetActive(false);

            _exitButton = UiFactory.CreateButton(_root, "Exit", "Back", GameTheme.Wood,
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.12f), Vector2.zero, Vector2.zero);
            _exitButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.MainMill));
        }

        private void ShowCurrentStage()
        {
            ClearStage();
            _stageComplete = false;
            _continueButton.gameObject.SetActive(false);
            _retryButton.gameObject.SetActive(false);

            var stage = _session.Production.Session.currentStage;
            var forgiveness = _session.Progression.GetForgivenessBonus();

            switch (stage)
            {
                case ProductionStage.Sorting:
                    _headerText.text = $"Sorting · {_oil.rawMaterialName}";
                    _hintText.text = _session.Tutorial.IsActive ? _session.Tutorial.GetPrompt() : "Click good seeds with the mouse. Avoid debris.";
                    _sorting = _stageRoot.AddComponent<SortingMinigame>();
                    _sorting.Begin(_oil, forgiveness, OnStageScored);
                    break;
                case ProductionStage.Processing:
                    _headerText.text = $"Processing · {_oil.displayName}";
                    _hintText.text = _session.Tutorial.IsActive ? _session.Tutorial.GetPrompt() : "Hold to raise pressure. Stay in the golden zone.";
                    _processing = _stageRoot.AddComponent<ProcessingMinigame>();
                    _processing.Begin(_oil, forgiveness, OnStageScored);
                    break;
                case ProductionStage.Bottling:
                    _headerText.text = $"Bottling · {_oil.displayName}";
                    _hintText.text = _session.Tutorial.IsActive ? _session.Tutorial.GetPrompt() : "Tap to stop the fill in the target band.";
                    _bottling = _stageRoot.AddComponent<BottlingMinigame>();
                    _bottling.Begin(_oil, forgiveness, OnStageScored);
                    break;
                case ProductionStage.Finished:
                    ShowFinishedSummary();
                    break;
                default:
                    SceneManager.LoadScene(SceneNames.MainMill);
                    break;
            }
        }

        private void OnStageScored(float score)
        {
            _lastScore = score;
            _stageComplete = true;
            _scoreText.text = $"Stage quality: {Mathf.RoundToInt(score * 100)}%";
            _continueButton.gameObject.SetActive(true);
            _retryButton.gameObject.SetActive(true);
            _hintText.text = "Nice work. Continue, or retry this stage before committing.";
        }

        private void OnContinue()
        {
            if (!_stageComplete)
            {
                return;
            }

            var stage = _session.Production.Session.currentStage;
            _session.Production.SetStageScore(stage, _lastScore);
            if (_session.Production.Session.currentStage == ProductionStage.Finished ||
                _session.Production.Session.committed ||
                !_session.Production.HasActiveSession && _session.State.productionSession.resultingLiters > 0f)
            {
                ShowFinishedSummary();
            }
            else
            {
                ShowCurrentStage();
            }
        }

        private void OnRetry()
        {
            _session.Production.RetryCurrentStage();
            ShowCurrentStage();
        }

        private void ShowFinishedSummary()
        {
            ClearStage();
            var session = _session.State.productionSession;
            _headerText.text = "Batch Ready";
            _hintText.text = $"Produced {session.resultingLiters:0.#} L of {_oil.displayName} (quality x{session.qualityMultiplier:0.00})";
            _scoreText.text = $"Sort {Pct(session.sortingScore)} · Press {Pct(session.processingScore)} · Bottle {Pct(session.bottlingScore)}";
            _continueButton.gameObject.SetActive(true);
            _continueButton.GetComponentInChildren<Text>().text = "Return to Mill";
            _retryButton.gameObject.SetActive(false);
            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(() =>
            {
                _session.Production.ClearCommittedSession();
                SceneManager.LoadScene(SceneNames.MainMill);
            });

            var panel = UiFactory.CreateFramedPanel(_stageRoot.transform, "Summary",
                new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.75f), Vector2.zero, Vector2.zero);
            UiFactory.CreateArtImage(panel, "Bottle", ArtCatalog.OilBottle,
                new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.92f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(panel, "Body",
                "The oil is in your storage tank.\nDeliver it to complete the order.",
                28, GameTheme.TextDark, TextAnchor.MiddleCenter);
        }

        private static string Pct(float v) => $"{Mathf.RoundToInt(v * 100)}%";

        private void ClearStage()
        {
            if (_sorting != null) Destroy(_sorting);
            if (_processing != null) Destroy(_processing);
            if (_bottling != null) Destroy(_bottling);
            _sorting = null;
            _processing = null;
            _bottling = null;

            var children = new List<Transform>();
            foreach (Transform child in _stageRoot.transform)
            {
                children.Add(child);
            }

            for (int i = 0; i < children.Count; i++)
            {
                Destroy(children[i].gameObject);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
    }

    public sealed class SortingMinigame : MonoBehaviour
    {
        private Action<float> _onComplete;
        private OilDefinition _oil;
        private float _forgiveness;
        private int _caughtGood;
        private int _caughtBad;
        private int _spawned;
        private float _spawnTimer;
        private Text _counter;
        private RectTransform _area;
        private readonly List<SortItem> _items = new List<SortItem>();
        private bool _done;
        private const int TargetGood = 8;
        private const int MaxSpawns = 16;

        public void Begin(OilDefinition oil, float forgiveness, Action<float> onComplete)
        {
            _oil = oil;
            _forgiveness = forgiveness;
            _onComplete = onComplete;
            _area = GetComponent<RectTransform>();
            _counter = UiFactory.CreateText(transform, "Counter", "Click good seeds · Good: 0 / 8", 24, GameTheme.TextDark, TextAnchor.UpperCenter);
            _counter.rectTransform.anchorMin = new Vector2(0.2f, 0.88f);
            _counter.rectTransform.anchorMax = new Vector2(0.8f, 1f);

            UiFactory.CreateArtImage(transform, "BasketArt", ArtCatalog.Basket,
                new Vector2(0.35f, 0.0f), new Vector2(0.65f, 0.22f), Vector2.zero, Vector2.zero);
        }

        private void Update()
        {
            if (_done) return;

            _spawnTimer -= Time.deltaTime;
            var interval = Mathf.Max(0.35f, 0.7f / _oil.sortingSpeed);
            if (_spawnTimer <= 0f && _spawned < MaxSpawns)
            {
                SpawnItem();
                _spawnTimer = interval;
            }

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item == null || item.Consumed)
                {
                    _items.RemoveAt(i);
                    continue;
                }

                item.Tick(Time.deltaTime);
                if (item.FallenOff)
                {
                    if (item.IsGood)
                    {
                        // missed good seed
                    }

                    Destroy(item.gameObject);
                    _items.RemoveAt(i);
                }
            }

            if ((_caughtGood >= TargetGood) || (_spawned >= MaxSpawns && _items.Count == 0))
            {
                Finish();
            }
        }

        private void SpawnItem()
        {
            _spawned++;
            bool isGood = UnityEngine.Random.value > 0.3f;
            var sprite = isGood ? ArtCatalog.SeedForOil(_oil.id) : ArtCatalog.Debris;
            var tint = isGood ? Color.Lerp(Color.white, _oil.seedColor, 0.25f) : Color.white;
            var image = UiFactory.CreateArtImage(transform, isGood ? "Seed" : "Debris", sprite,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, tint, true);
            image.raycastTarget = true;
            var panel = image.rectTransform;
            panel.sizeDelta = new Vector2(96, 96);
            float width = Mathf.Max(200f, _area.rect.width);
            float height = Mathf.Max(200f, _area.rect.height);
            panel.anchoredPosition = new Vector2(UnityEngine.Random.Range(-width * 0.4f, width * 0.4f), height * 0.35f);

            var item = panel.gameObject.AddComponent<SortItem>();
            item.Initialize(isGood, 180f * _oil.sortingSpeed, () => OnItemTapped(item));
            _items.Add(item);
        }

        private void OnItemTapped(SortItem item)
        {
            if (_done || item.Consumed) return;
            item.Consumed = true;
            if (item.IsGood)
            {
                _caughtGood++;
            }
            else
            {
                _caughtBad++;
            }

            _counter.text = $"Click good seeds · Good: {_caughtGood} / {TargetGood}   Debris: {_caughtBad}";
            Destroy(item.gameObject);
        }

        private void Finish()
        {
            if (_done) return;
            _done = true;
            float score = Mathf.Clamp01((_caughtGood / (float)TargetGood) - _caughtBad * (0.08f - _forgiveness * 0.5f));
            score = Mathf.Clamp01(score + _forgiveness * 0.25f);
            if (score < 0.55f) score = 0.55f; // never soft-lock first runs too harshly
            _onComplete?.Invoke(score);
        }
    }

    public sealed class SortItem : MonoBehaviour, IPointerClickHandler
    {
        public bool IsGood;
        public bool Consumed;
        public bool FallenOff;
        private float _speed;
        private Action _onTap;
        private RectTransform _rt;

        public void Initialize(bool isGood, float speed, Action onTap)
        {
            IsGood = isGood;
            _speed = speed;
            _onTap = onTap;
            _rt = GetComponent<RectTransform>();
            var button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => _onTap?.Invoke());
        }

        public void Tick(float dt)
        {
            _rt.anchoredPosition += Vector2.down * _speed * dt;
            if (_rt.anchoredPosition.y < -400f)
            {
                FallenOff = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onTap?.Invoke();
        }
    }

    public sealed class ProcessingMinigame : MonoBehaviour
    {
        private Action<float> _onComplete;
        private OilDefinition _oil;
        private float _forgiveness;
        private float _pressure;
        private float _timeInZone;
        private float _elapsed;
        private bool _holding;
        private bool _done;
        private Image _bar;
        private Image _zone;
        private Text _label;
        private const float Duration = 8f;

        public void Begin(OilDefinition oil, float forgiveness, Action<float> onComplete)
        {
            _oil = oil;
            _forgiveness = forgiveness;
            _onComplete = onComplete;

            UiFactory.CreateArtImage(transform, "PressArt", ArtCatalog.WoodenPress,
                new Vector2(0.32f, 0.38f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero);

            var track = UiFactory.CreatePanel(transform, "Track", GameTheme.WoodDark,
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.35f), Vector2.zero, Vector2.zero);

            var window = Mathf.Clamp01(_oil.processingWindow + _forgiveness);
            _zone = UiFactory.CreatePanel(track, "Zone", GameTheme.AccentSoft,
                new Vector2(0.5f - window * 0.5f, 0.15f), new Vector2(0.5f + window * 0.5f, 0.85f), Vector2.zero, Vector2.zero).GetComponent<Image>();

            _bar = UiFactory.CreatePanel(track, "Bar", GameTheme.Accent,
                new Vector2(0f, 0.05f), new Vector2(0.02f, 0.95f), Vector2.zero, Vector2.zero).GetComponent<Image>();

            _label = UiFactory.CreateText(transform, "Label", "Hold to press Father's mill", 26, GameTheme.TextDark, TextAnchor.MiddleCenter);
            _label.rectTransform.anchorMin = new Vector2(0.2f, 0.36f);
            _label.rectTransform.anchorMax = new Vector2(0.8f, 0.46f);

            var holdButton = UiFactory.CreateButton(transform, "HoldArea", "Hold to Press", GameTheme.Wood,
                new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.16f), Vector2.zero, Vector2.zero);
            var trigger = holdButton.gameObject.AddComponent<HoldButton>();
            trigger.OnHoldChanged = v => _holding = v;
        }

        private void Update()
        {
            if (_done) return;
            _elapsed += Time.deltaTime;
            float rise = _holding ? 0.55f : -0.35f;
            _pressure = Mathf.Clamp01(_pressure + rise * Time.deltaTime);

            var rt = _bar.rectTransform;
            rt.anchorMin = new Vector2(_pressure, 0.05f);
            rt.anchorMax = new Vector2(Mathf.Min(1f, _pressure + 0.02f), 0.95f);

            var window = Mathf.Clamp01(_oil.processingWindow + _forgiveness);
            float zoneMin = 0.5f - window * 0.5f;
            float zoneMax = 0.5f + window * 0.5f;
            if (_pressure >= zoneMin && _pressure <= zoneMax)
            {
                _timeInZone += Time.deltaTime;
                _label.text = "Perfect pressure!";
            }
            else
            {
                _label.text = _holding ? "Ease up..." : "Press harder...";
            }

            if (_elapsed >= Duration)
            {
                _done = true;
                float score = Mathf.Clamp01(_timeInZone / (Duration * 0.55f));
                score = Mathf.Clamp01(score + _forgiveness * 0.2f);
                if (score < 0.5f) score = 0.5f;
                _onComplete?.Invoke(score);
            }
        }
    }

    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> OnHoldChanged;

        public void OnPointerDown(PointerEventData eventData) => OnHoldChanged?.Invoke(true);
        public void OnPointerUp(PointerEventData eventData) => OnHoldChanged?.Invoke(false);
        public void OnPointerExit(PointerEventData eventData) => OnHoldChanged?.Invoke(false);
    }

    public sealed class BottlingMinigame : MonoBehaviour
    {
        private Action<float> _onComplete;
        private OilDefinition _oil;
        private float _forgiveness;
        private int _bottleIndex;
        private float _fill;
        private bool _running = true;
        private bool _done;
        private readonly List<float> _scores = new List<float>();
        private Image _fillImage;
        private Image _target;
        private Text _label;
        private const int BottleCount = 4;

        public void Begin(OilDefinition oil, float forgiveness, Action<float> onComplete)
        {
            _oil = oil;
            _forgiveness = forgiveness;
            _onComplete = onComplete;

            var bottleFrame = UiFactory.CreateFramedPanel(transform, "BottleFrame",
                new Vector2(0.34f, 0.18f), new Vector2(0.66f, 0.78f), Vector2.zero, Vector2.zero);

            UiFactory.CreateArtImage(bottleFrame, "BottleArt", ArtCatalog.OilBottle,
                new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.92f), Vector2.zero, Vector2.zero,
                Color.Lerp(Color.white, oil.oilColor, 0.15f));

            _fillImage = UiFactory.CreatePanel(bottleFrame, "Fill", new Color(oil.oilColor.r, oil.oilColor.g, oil.oilColor.b, 0.55f),
                new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.12f), Vector2.zero, Vector2.zero).GetComponent<Image>();

            var window = Mathf.Clamp01(_oil.bottlingWindow + _forgiveness);
            _target = UiFactory.CreatePanel(bottleFrame, "Target", new Color(1f, 1f, 1f, 0.35f),
                new Vector2(0.18f, 0.68f - window), new Vector2(0.82f, 0.68f + window * 0.15f), Vector2.zero, Vector2.zero).GetComponent<Image>();

            _label = UiFactory.CreateText(transform, "Label", "Bottle 1 / 4 — tap to stop", 26, GameTheme.TextDark, TextAnchor.MiddleCenter);
            _label.rectTransform.anchorMin = new Vector2(0.2f, 0.05f);
            _label.rectTransform.anchorMax = new Vector2(0.8f, 0.15f);

            var tap = UiFactory.CreateButton(transform, "TapZone", "Tap to Stop Fill", GameTheme.Accent,
                new Vector2(0.25f, 0.8f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);
            tap.onClick.AddListener(StopFill);
        }

        private void Update()
        {
            if (_done || !_running) return;
            _fill += Time.deltaTime * (0.35f + _oil.sortingSpeed * 0.15f);
            if (_fill >= 1f)
            {
                _fill = 1f;
                StopFill();
                return;
            }

            var rt = _fillImage.rectTransform;
            rt.anchorMax = new Vector2(0.72f, 0.12f + _fill * 0.7f);
        }

        private void StopFill()
        {
            if (_done || !_running) return;
            _running = false;

            var window = Mathf.Clamp01(_oil.bottlingWindow + _forgiveness);
            float target = 0.7f;
            float dist = Mathf.Abs(_fill - target);
            float score = Mathf.Clamp01(1f - dist / (window + 0.15f));
            _scores.Add(score);

            _bottleIndex++;
            if (_bottleIndex >= BottleCount)
            {
                _done = true;
                float avg = 0f;
                for (int i = 0; i < _scores.Count; i++) avg += _scores[i];
                avg /= _scores.Count;
                avg = Mathf.Clamp01(avg + _forgiveness * 0.2f);
                if (avg < 0.5f) avg = 0.5f;
                _label.text = "Bottling complete";
                _onComplete?.Invoke(avg);
                return;
            }

            _fill = 0f;
            _running = true;
            _label.text = $"Bottle {_bottleIndex + 1} / {BottleCount} — tap to stop";
            var fillRt = _fillImage.rectTransform;
            fillRt.anchorMax = new Vector2(0.72f, 0.12f);
        }
    }
}
