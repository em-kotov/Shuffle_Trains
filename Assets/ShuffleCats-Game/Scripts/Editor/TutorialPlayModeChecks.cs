using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class TutorialPlayModeChecks
{
    private const string ScenePath = "Assets/ShuffleCats-Game/Scenes/Level-1.unity";
    private const string ReportPath = "Library/TutorialPlayChecks.txt";
    private static readonly List<string> Report = new();
    private static readonly Dictionary<RectTransform, Vector3[]> HudCorners = new();
    private static Vector3 _cameraPosition;
    private static float _cameraSize;

    [MenuItem("Tools/Shuffle Cats/Tutorial/Check First Three Levels (Play Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying == false || Localization.IsReady == false)
            throw new InvalidOperationException("Start from Boot before running tutorial checks.");

        Report.Clear();
        File.WriteAllText(ReportPath, "RUNNING");
        EditorCoroutineUtility.StartCoroutineOwnerless(Guarded(Check()));
    }

    private static IEnumerator Guarded(IEnumerator check)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(check);
        try
        {
            while (stack.Count > 0)
            {
                bool next;
                try
                {
                    if (EditorApplication.isPlaying == false)
                        throw new Exception("Play mode stopped during checks.");

                    UnityEngine.Object.FindObjectOfType<YandexBridge>()?.SetBrowserFocus("1");
                    next = stack.Peek().MoveNext();
                }
                catch (Exception error)
                {
                    Report.Add("FAIL: " + error.Message);
                    File.WriteAllLines(ReportPath, Report);
                    Debug.LogException(error);
                    yield break;
                }

                if (next == false)
                {
                    (stack.Pop() as IDisposable)?.Dispose();
                    continue;
                }

                if (stack.Peek().Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }

                yield return stack.Peek().Current;
            }
        }
        finally
        {
            while (stack.Count > 0)
                (stack.Pop() as IDisposable)?.Dispose();
        }
    }

    private static IEnumerator Check()
    {
        const int availabilityTimeoutSeconds = 15;
        const int targetTimeoutSeconds = 30;
        const int deadlockTimeoutSeconds = 60;
        const int restartTimeoutSeconds = 10;
        const int sceneReloadTimeoutSeconds = 20;
        const int solveTimeoutSeconds = 45;
        const float cardObservationSeconds = 8f;
        const float inputSettleSeconds = 0.1f;
        const float layoutSettleSeconds = 0.2f;
        const float animationSettleSeconds = 0.3f;

        int preview = LevelProgress.EditorPreviewIndex;
        int money = PlayerPrefs.GetInt(SaveKeys.Money), savedLevel = PlayerPrefs.GetInt(SaveKeys.LevelIndex);
        bool hadMoney = PlayerPrefs.HasKey(SaveKeys.Money), hadLanguage = PlayerPrefs.HasKey(SaveKeys.Language), hadHints = PlayerPrefs.HasKey(SaveKeys.Hints);
        string language = Localization.Preference;
        bool hints = HintSettings.Enabled;
        try
        {
            HintSettings.Enabled = false;
            yield return LoadLevel(0);
            yield return Wait(() => Tutorial()?.Target != null, availabilityTimeoutSeconds);
            var tutorial = Tutorial();
            var overlay = Overlay();
            CheckThat(tutorial.IsRunning, "Level 1 starts with idle hints disabled");
            yield return CheckTarget(tutorial, overlay);
            yield return CheckLesson(tutorial, overlay, TutorialController.Stage.Baskets, 2);
            CheckThat(tutorial.CompletedSteps == 1, "Baskets shown once, after the first launch");
            Continue(overlay);
            yield return Wait(() => tutorial.Target != null, targetTimeoutSeconds);
            CheckThat(tutorial.CurrentStage == TutorialController.Stage.LastCat, "Basket acknowledgement leads to the other cat");
            yield return CheckTarget(tutorial, overlay);
            yield return CheckWin(tutorial, 1);

            yield return LoadLevel(1);
            yield return Wait(() => Tutorial() != null && Overlay() != null, availabilityTimeoutSeconds);
            tutorial = Tutorial();
            overlay = Overlay();
            yield return CheckLesson(tutorial, overlay, TutorialController.Stage.Capacity, 1);
            var canvas = overlay.GetComponentInParent<Canvas>(true);
            CheckThat(canvas != null, "Tutorial has a parent canvas");
            var responsive = canvas.rootCanvas.GetComponent<ResponsiveGameUI>();
            CheckThat(responsive != null, "Tutorial uses the responsive root canvas: " + canvas.name);
            var capacity = responsive.SafeRoot.Find("CatsCounterPanel/Counter/BgIconPrimary") as RectTransform;
            CheckThat(capacity != null, "Track capacity artwork exists");
            CheckThat(overlay.SpotlightRects[0].Contains(overlay.transform.InverseTransformPoint(capacity.TransformPoint(capacity.rect.center))), "Capacity badge is spotlighted");
            TapFocus(overlay);
            yield return Wait(() => tutorial.Target != null, targetTimeoutSeconds);
            CheckThat(tutorial.Target.Color == CatColor.Green, "Demonstration starts with a green cat");
            yield return CheckTarget(tutorial, overlay);
            yield return Wait(() => tutorial.Target != null, targetTimeoutSeconds);
            CheckThat(
                tutorial.CurrentStage == TutorialController.Stage.DemoSecond && tutorial.Target.Color == CatColor.Green,
                "Demonstration sends a second green cat, without repeating baskets");
            CheckThat(
                overlay.transform.Find("Instruction/Progress").GetComponent<TMP_Text>().text.Contains("2/9"),
                "Both demonstration launches belong to tip 2");
            yield return CheckTarget(tutorial, overlay);
            var demoContinue = overlay.transform.Find("Instruction/Continue").GetComponent<UnityEngine.UI.Button>();
            yield return Wait(() => tutorial.CurrentStage == TutorialController.Stage.FullTrack, deadlockTimeoutSeconds);
            CheckThat(tutorial.CompletedSteps == 2, "Tip 3 follows both launches and the real deadlock");
            yield return CheckLesson(tutorial, overlay, TutorialController.Stage.FullTrack, 1);
            CheckThat(
                overlay.transform.Find("Instruction/Progress").GetComponent<TMP_Text>().text.Contains("3/9"),
                "The full-track explanation is tip 3");
            CheckThat(
                overlay.transform.Find("Instruction/Title").GetComponent<TMP_Text>().text == Localization.Get("The track is full"),
                "Tip 3 explains the result instead of repeating the launch instruction");
            yield return StableCard(overlay, cardObservationSeconds);
            CheckThat(
                tutorial.CurrentStage == TutorialController.Stage.FullTrack,
                "Tip 3 cannot be replaced by a timer or completed cat movement");
            var background = new PointerEventData(EventSystem.current) { position = Vector2.one * 2, pointerId = -1 };
            overlay.OnPointerDown(background);
            overlay.OnPointerUp(background);
            overlay.OnPointerClick(background);
            CheckThat(tutorial.CurrentStage == TutorialController.Stage.FullTrack, "Background taps cannot dismiss tip 3");
            UnityEngine.Object.FindObjectOfType<YandexBridge>().OnGameApiPause(string.Empty);
            yield return Seconds(inputSettleSeconds);
            demoContinue.onClick.Invoke();
            CheckThat(tutorial.CurrentStage == TutorialController.Stage.FullTrack, "Paused Continue cannot replace tip 3");
            UnityEngine.Object.FindObjectOfType<YandexBridge>().OnGameApiResume(string.Empty);
            yield return Seconds(inputSettleSeconds);
            Continue(overlay);
            CheckThat(tutorial.CurrentStage == TutorialController.Stage.Blocked, "A deliberate Continue opens tip 4");
            var track = UnityEngine.Object.FindObjectOfType<TrackRegistrator>();
            CheckThat(
                track.CurrentCount == 2 && track.MaxCount == 2 && UnityEngine.Object.FindObjectOfType<SorterRegistrator>().IsTrackDeadlocked(2),
                "Two green cats cause a real 2/2 deadlock");
            CheckThat(
                UnityEngine.Object.FindObjectsOfType<Subscriber>().Any(c => c.Color == CatColor.Blue && c.Parking.isActiveAndEnabled && c.Parking.IsAtBorderCell == false),
                "The blue cat remains parked with its yarn");
            var filledStations = UnityEngine.Object.FindObjectsOfType<Station>().Where(s => s.HavePassengers(out _, out _)).ToArray();
            yield return CheckLesson(tutorial, overlay, TutorialController.Stage.Blocked, 3 + filledStations.Length);

            foreach (var cat in UnityEngine.Object.FindObjectsOfType<Subscriber>())
                CheckSpriteAccent(overlay, cat.CatSprite);

            foreach (var station in filledStations)
            {
                CheckThat(station.GetComponentsInChildren<Passenger>().Length > 0, "Accented station contains yarn balls");

                foreach (var sprite in station.GetComponentsInChildren<SpriteRenderer>())
                    CheckSpriteAccent(overlay, sprite);
            }

            CheckThat(UnityEngine.Object.FindObjectOfType<LooseUI>().IsVisible == false, "Deadlock lesson keeps the loss window closed");
            Continue(overlay);
            yield return Seconds(animationSettleSeconds);
            var options = UnityEngine.Object.FindObjectOfType<OptionsUI>();
            CheckThat(
                tutorial.CurrentStage == TutorialController.Stage.OpenSettings && options.IsVisible == false,
                "Restart lesson first waits for a click on Settings");
            CheckButtonAccent(overlay, options.OpenButton);
            yield return CheckTranslations(overlay, "OpenSettings");
            yield return StableCard(overlay, 1f);
            TapFocus(overlay);
            yield return Wait(() => tutorial.CurrentStage == TutorialController.Stage.Restart, restartTimeoutSeconds);
            yield return Seconds(layoutSettleSeconds);
            CheckThat(
                options.IsVisible && YandexSdk.IsOnlyOptionsPaused && Time.timeScale == 0 && overlay.gameObject.activeSelf,
                "Settings opens normally and the guide remains visible during its pause");
            CheckThat(UnityEngine.Object.FindObjectOfType<LooseUI>().IsVisible == false, "Guided restart uses Settings, not the loss window");
            CheckButtonAccent(overlay, options.RestartButton);
            yield return CheckTranslations(overlay, "Restart");
            yield return StableCard(overlay, 1f);
            int oldScene = SceneManager.GetActiveScene().handle;
            var pausedPointer = FocusPointer(overlay);
            var bridge = UnityEngine.Object.FindObjectOfType<YandexBridge>();
            bridge.OnGameApiPause(string.Empty);
            yield return Seconds(layoutSettleSeconds);
            CheckThat(overlay.gameObject.activeSelf == false && YandexSdk.IsOnlyOptionsPaused == false, "Platform pause also suspends the Settings guide");
            overlay.OnPointerDown(pausedPointer);
            overlay.OnPointerUp(pausedPointer);
            overlay.OnPointerClick(pausedPointer);
            CheckThat(SceneManager.GetActiveScene().handle == oldScene, "Platform-paused tutorial cannot restart the level");
            bridge.OnGameApiResume(string.Empty);
            yield return Seconds(layoutSettleSeconds);
            CheckThat(overlay.gameObject.activeSelf && Time.timeScale == 0, "Platform resume restores the guide while Settings keeps gameplay paused");
            TapFocus(overlay);
            yield return Wait(() => SceneManager.GetActiveScene().handle != oldScene && Tutorial()?.Target != null, sceneReloadTimeoutSeconds);
            tutorial = Tutorial();
            overlay = Overlay();
            CaptureGameFraming();
            CheckThat(
                tutorial.CurrentStage == TutorialController.Stage.PracticeFirst && tutorial.CompletedSteps == 0,
                "Real restart resumes the solution, without looping the failure lesson");
            CheckThat(UnityEngine.Object.FindObjectOfType<TrackRegistrator>().CurrentCount == 0, "Restart resets track occupancy");
            CheckThat(YandexSdk.IsPaused == false && Time.timeScale > 0, "Restart releases the Settings pause");

            foreach (CatColor color in new[] { CatColor.Green, CatColor.Blue, CatColor.Green })
            {
                yield return Wait(() => tutorial.Target != null, solveTimeoutSeconds);
                CheckThat(tutorial.Target.Color == color && tutorial.IsReadingBaskets == false, "Retry guides the winning order: " + color);
                yield return CheckTarget(tutorial, overlay);
            }

            yield return CheckWin(tutorial, 2);

            yield return LoadLevel(0);
            yield return Wait(() => Tutorial()?.Target != null, availabilityTimeoutSeconds);
            tutorial = Tutorial();
            Overlay().transform.Find("Instruction/Skip").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Seconds(inputSettleSeconds);
            CheckThat(tutorial.IsRunning == false && Overlay() == null, "Skip dismisses tutorial");
            CheckGameFraming();
            CheckThat(UnityEngine.Object.FindObjectsOfType<Subscriber>().All(c => c.Clicks.InteractionAllowed), "Skip restores cat input");
            CheckThat(UnityEngine.Object.FindObjectOfType<AutoLoose>().TutorialOwnsDeadlock == false, "Skip restores normal loss handling");
            yield return LoadLevel(1);
            yield return Wait(() => Tutorial()?.CurrentStage == TutorialController.Stage.Capacity && Overlay() != null, availabilityTimeoutSeconds);
            yield return Seconds(layoutSettleSeconds);
            Continue(Overlay());

            for (int i = 0; i < 2; i++)
            {
                yield return Wait(() => Tutorial()?.Target != null, targetTimeoutSeconds);
                yield return Seconds(layoutSettleSeconds);
                TapFocus(Overlay());
            }

            yield return Wait(() => Tutorial()?.CurrentStage == TutorialController.Stage.FullTrack, deadlockTimeoutSeconds);
            CheckThat(YandexSdk.IsOnlyTutorialPaused && Time.timeScale == 0, "Observation freezes only the demonstrated board state");
            Overlay().transform.Find("Instruction/Skip").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Seconds(inputSettleSeconds);
            CheckThat(YandexSdk.IsPaused == false && Time.timeScale > 0, "Skipping an observation releases the tutorial pause");
            yield return CheckHintLesson();
            yield return LoadLevel(3);
            CheckThat(Tutorial() == null, "Level 4 has no tutorial");
            CheckThat(PlayerPrefs.GetInt(SaveKeys.LevelIndex) == savedLevel, "Saved campaign progress unchanged");
            Report.Add("PASS: levels 1–3 teaching, real restart, successful retry and hint-button help.");
            File.WriteAllLines(ReportPath, Report);
        }
        finally
        {
            UnityEngine.Object.FindObjectOfType<YandexBridge>()?.OnGameApiResume(string.Empty);
            YandexSdk.SetTutorialOpen(false);
            Time.timeScale = 1;
            HintSettings.Enabled = hints;

            if (hadHints == false)
                PlayerPrefs.DeleteKey(SaveKeys.Hints);

            Localization.Select(language);

            if (hadLanguage == false)
                PlayerPrefs.DeleteKey(SaveKeys.Language);

            if (hadMoney)
                PlayerPrefs.SetInt(SaveKeys.Money, money);
            else
                PlayerPrefs.DeleteKey(SaveKeys.Money);

            PlayerPrefs.Save();
            LevelProgress.EditorPreviewIndex = preview;

            if (EditorApplication.isPlaying)
                SceneManager.LoadScene(ScenePath);
        }
    }

    private static IEnumerator CheckHintLesson()
    {
        const int availabilityTimeoutSeconds = 15;
        const int highlightTimeoutSeconds = 5;
        const float cardObservationSeconds = 9f;
        const float layoutSettleSeconds = 0.2f;

        HintSettings.Enabled = true;
        yield return LoadLevel(2);
        yield return Wait(() => Tutorial()?.CurrentStage == TutorialController.Stage.Hint && Overlay() != null, availabilityTimeoutSeconds);
        var tutorial = Tutorial();
        var overlay = Overlay();
        var hints = UnityEngine.Object.FindObjectOfType<CatHintController>();
        var highlight = hints.HintButton.transform.parent.Find("CatHintHighlight");
        CheckButtonAccent(overlay, hints.HintButton);
        yield return CheckTranslations(overlay, "Hint");
        yield return StableCard(overlay, cardObservationSeconds);
        CheckThat(
            tutorial.IsRunning && highlight.gameObject.activeSelf == false,
            "Hint lesson waits for the player without an automatic hint or timeout");
        CheckThat(
            UnityEngine.Object.FindObjectsOfType<Subscriber>().All(cat => cat.Clicks.InteractionAllowed == false),
            "Hint lesson blocks cat launches until the player chooses help or skips");
        var background = new PointerEventData(EventSystem.current) { position = Vector2.one * 2, pointerId = -1 };
        overlay.OnPointerDown(background);
        overlay.OnPointerUp(background);
        overlay.OnPointerClick(background);
        CheckThat(tutorial.IsRunning, "Background tap does not dismiss the hint lesson");
        var pointer = FocusPointer(overlay);
        var bridge = UnityEngine.Object.FindObjectOfType<YandexBridge>();
        bridge.OnGameApiPause(string.Empty);
        yield return Seconds(layoutSettleSeconds);
        CheckThat(overlay.gameObject.activeSelf == false, "Platform pause hides the hint lesson");
        overlay.OnPointerDown(pointer);
        overlay.OnPointerUp(pointer);
        overlay.OnPointerClick(pointer);
        CheckThat(tutorial.IsRunning && highlight.gameObject.activeSelf == false, "Paused hint tap has no effect");
        bridge.OnGameApiResume(string.Empty);
        yield return Seconds(layoutSettleSeconds);
        TapFocus(overlay);
        yield return Wait(() => highlight.gameObject.activeInHierarchy, highlightTimeoutSeconds);
        CheckThat(
            tutorial.IsRunning == false && Overlay() == null && hints.TutorialActive == false,
            "Clicking the real hint button finishes the tip and displays a cat suggestion");
        CheckThat(
            UnityEngine.Object.FindObjectsOfType<Subscriber>().All(cat => cat.Clicks.InteractionAllowed),
            "Normal cat input resumes after the hint lesson");
        CheckThat(
            UnityEngine.Object.FindObjectOfType<TrackRegistrator>().CurrentCount == 0,
            "A hint highlights a choice without launching a cat");
        CheckGameFraming();

        HintSettings.Enabled = false;
        yield return LoadLevel(2);
        tutorial = Tutorial();
        overlay = Overlay();
        yield return CheckLesson(tutorial, overlay, TutorialController.Stage.Hint, 1);
        CheckThat(
            UnityEngine.Object.FindObjectOfType<CatHintController>().HintButton.interactable == false,
            "Disabled hints stay disabled during their explanation");
        Continue(overlay);
        yield return Seconds(layoutSettleSeconds);
        CheckThat(
            tutorial.IsRunning == false && Overlay() == null && HintSettings.Enabled == false,
            "Disabled-hints explanation can be acknowledged without changing the preference");

        HintSettings.Enabled = true;
        yield return LoadLevel(2);
        tutorial = Tutorial();
        overlay = Overlay();
        overlay.transform.Find("Instruction/Skip").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return Seconds(layoutSettleSeconds);
        CheckThat(
            tutorial.IsRunning == false && Overlay() == null && HintSettings.Enabled,
            "The hint lesson can be skipped while keeping hints enabled");
        CheckThat(
            UnityEngine.Object.FindObjectOfType<CatHintController>().HintButton.transform.parent.Find("CatHintHighlight").gameObject.activeSelf == false,
            "Skipping does not request a hint");
    }

    private static IEnumerator CheckTarget(TutorialController tutorial, TutorialOverlay overlay)
    {
        const float layoutSettleSeconds = 0.2f;
        const float pauseTransitionSeconds = 0.1f;
        const int secondPointerId = 42;
        const int deadlockTimeoutSeconds = 60;
        const float cardLayoutTolerance = 0.01f;

        yield return Seconds(layoutSettleSeconds);
        var target = tutorial.Target;
        int completed = tutorial.CompletedSteps;
        var stage = tutorial.CurrentStage;
        yield return CheckTranslations(overlay, stage.ToString());
        yield return StableCard(overlay, 1f);
        CheckThat(tutorial.Target == target && tutorial.CompletedSteps == completed, "Target step waits for a click");

        foreach (Subscriber other in UnityEngine.Object.FindObjectsOfType<Subscriber>())
        {
            if (other == target || other.Parking.isActiveAndEnabled == false || other.Parking.IsAtBorderCell)
                continue;

            Vector3 before = other.transform.position;
            var wrong = new PointerEventData(EventSystem.current);
            other.Clicks.OnPointerDown(wrong);
            other.Clicks.OnPointerClick(wrong);
            CheckThat(other.Parking.IsMoving == false && other.transform.position == before, "Wrong cat cannot move");
        }

        Time.timeScale = 0;
        yield return Seconds(pauseTransitionSeconds);
        CheckThat(overlay.gameObject.activeSelf == false && target.Clicks.InteractionAllowed == false, "Pause hides guide and blocks cats");
        Time.timeScale = 1;
        yield return Seconds(pauseTransitionSeconds);
        var pointer = FocusPointer(overlay);
        AssertOverlayRaycast(overlay, pointer);
        overlay.OnPointerClick(pointer);
        CheckThat(tutorial.CompletedSteps == completed, "Orphan click does not advance");
        overlay.OnPointerDown(pointer);
        CheckThat(tutorial.CompletedSteps == completed, "Press alone does not advance");
        var second = new PointerEventData(EventSystem.current) { position = pointer.position, pointerId = secondPointerId };
        overlay.OnPointerDown(second);
        overlay.OnPointerClick(second);
        CheckThat(tutorial.CompletedSteps == completed, "Second touch cannot consume the pending step");
        Vector2 cardPosition = overlay.Card.anchoredPosition, cardSize = overlay.Card.sizeDelta;
        overlay.OnPointerUp(pointer);
        overlay.OnPointerClick(pointer);
        CheckThat(tutorial.CompletedSteps == completed + 1, "Exactly one accepted launch advances the step");
        CheckThat(target.Parking.IsAtBorderCell || target.Parking.enabled == false, "Cat actually departs");

        if (stage == TutorialController.Stage.DemoSecond)
        {
            float deadline = Time.realtimeSinceStartup + deadlockTimeoutSeconds;

            while (tutorial.CurrentStage == TutorialController.Stage.WaitingForDeadlock)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new Exception("Deadlock demonstration did not finish.");

                if (Vector2.Distance(overlay.Card.anchoredPosition, cardPosition) > cardLayoutTolerance ||
                    Vector2.Distance(overlay.Card.sizeDelta, cardSize) > cardLayoutTolerance)
                    throw new Exception("Tip 2 moved or resized before tip 3 was ready.");

                yield return null;
            }

            CheckThat(
                tutorial.CurrentStage == TutorialController.Stage.FullTrack,
                "Tip 2 stays in its original position until tip 3 replaces it");
        }

        yield return Seconds(layoutSettleSeconds);
    }

    private static IEnumerator CheckLesson(TutorialController tutorial, TutorialOverlay overlay, TutorialController.Stage stage, int holes)
    {
        const float layoutSettleSeconds = 0.2f;
        const float inputSettleSeconds = 0.1f;
        const float cardObservationSeconds = 3f;

        yield return Seconds(layoutSettleSeconds);
        CheckThat(tutorial.CurrentStage == stage && tutorial.Target == null, "Reading lesson: " + stage);
        yield return CheckTranslations(overlay, stage.ToString());
        CheckThat(overlay.SpotlightRects.Count == holes, "Expected focus areas: " + holes);
        yield return StableCard(overlay, cardObservationSeconds);
        CheckThat(tutorial.CurrentStage == stage, "Reading lesson cannot advance on a timer");
        var outside = new PointerEventData(EventSystem.current) { position = Vector2.one * 2, pointerId = -1 };
        overlay.OnPointerDown(outside);
        overlay.OnPointerUp(outside);
        overlay.OnPointerClick(outside);
        CheckThat(tutorial.CurrentStage == stage, "Background click cannot dismiss lesson");
        CheckThat(UnityEngine.Object.FindObjectsOfType<Subscriber>().All(c => c.Clicks.InteractionAllowed == false), "Reading lesson blocks cat launches");
        var button = overlay.transform.Find("Instruction/Continue").GetComponent<UnityEngine.UI.Button>();
        UnityEngine.Object.FindObjectOfType<YandexBridge>().OnGameApiPause(string.Empty);
        yield return Seconds(inputSettleSeconds);
        button.onClick.Invoke();
        CheckThat(tutorial.CurrentStage == stage, "Paused Continue cannot dismiss lesson");
        UnityEngine.Object.FindObjectOfType<YandexBridge>().OnGameApiResume(string.Empty);
        yield return Seconds(inputSettleSeconds);
    }

    private static IEnumerator CheckTranslations(TutorialOverlay overlay, string name)
    {
        const float translationSettleSeconds = 0.25f;
        const float screenshotSettleSeconds = 0.15f;

        foreach (string language in new[] { "ru", "en", "tr" })
        {
            Localization.Select(language);
            yield return Seconds(translationSettleSeconds);
            CheckThat(overlay.GetComponentsInChildren<TMP_Text>().Any(t => t.isTextOverflowing) == false, name + " text fits: " + language);
            CheckLayout(overlay);
            ScreenCapture.CaptureScreenshot("Library/Tutorial-" + Screen.width + "x" + Screen.height + "-" + name + "-" + language + ".png");
            yield return Seconds(screenshotSettleSeconds);
        }
    }

    private static void CheckLayout(TutorialOverlay overlay)
    {
        CheckGameFraming();
        var root = (RectTransform)overlay.transform;
        var corners = new Vector3[4];
        overlay.Card.GetWorldCorners(corners);
        Vector2 min = root.InverseTransformPoint(corners[0]), max = root.InverseTransformPoint(corners[2]);
        Rect card = Rect.MinMaxRect(min.x, min.y, max.x, max.y), screen = root.rect;
        CheckThat(
            card.xMin >= screen.xMin && card.xMax <= screen.xMax && card.yMin >= screen.yMin && card.yMax <= screen.yMax,
            "Instruction stays on screen");

        foreach (Rect hole in overlay.SpotlightRects)
            CheckThat(card.Overlaps(hole) == false, "Instruction does not hide an accent");

        foreach (Subscriber cat in UnityEngine.Object.FindObjectsOfType<Subscriber>())
        {
            if (cat.Parking.isActiveAndEnabled == false || cat.Parking.IsAtBorderCell)
                continue;

            SpriteRenderer sprite = cat.CatSprite;
            Vector2 lower = Vector2.positiveInfinity, upper = Vector2.negativeInfinity;

            foreach (Vector2 vertex in sprite.sprite.vertices)
            {
                float pointX;

                if (sprite.flipX)
                {
                    pointX = -vertex.x;
                }
                else
                {
                    pointX = vertex.x;
                }

                float pointY;

                if (sprite.flipY)
                {
                    pointY = -vertex.y;
                }
                else
                {
                    pointY = vertex.y;
                }

                Vector3 point = new Vector3(pointX, pointY);
                Vector2 projected = Camera.main.WorldToScreenPoint(sprite.transform.TransformPoint(point));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, projected, null, out Vector2 local);
                lower = Vector2.Min(lower, local);
                upper = Vector2.Max(upper, local);
            }

            Rect bounds = Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
            CheckThat(screen.Contains(bounds.min) && screen.Contains(bounds.max), "All parked cats remain on screen");
        }
    }

    private static void CheckSpriteAccent(TutorialOverlay overlay, SpriteRenderer sprite)
    {
        var points = sprite.sprite.vertices.Select(vertex =>
        {
            float pointX;

            if (sprite.flipX)
            {
                pointX = -vertex.x;
            }
            else
            {
                pointX = vertex.x;
            }

            float pointY;

            if (sprite.flipY)
            {
                pointY = -vertex.y;
            }
            else
            {
                pointY = vertex.y;
            }

            var point = new Vector3(pointX, pointY);
            Vector2 screen = Camera.main.WorldToScreenPoint(sprite.transform.TransformPoint(point));
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)overlay.transform, screen, null, out Vector2 local);
            return local;
        }).ToArray();
        CheckThat(overlay.SpotlightRects.Any(hole => points.All(hole.Contains)), "Complete artwork is accented: " + sprite.name);
    }

    private static void CheckButtonAccent(TutorialOverlay overlay, UnityEngine.UI.Button button)
    {
        var rect = (RectTransform)button.transform;
        Vector2 center = overlay.transform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        CheckThat(overlay.SpotlightRects.Any(hole => hole.Contains(center)), "Real button is accented: " + button.name);
    }

    private static void CaptureGameFraming()
    {
        _cameraPosition = Camera.main.transform.position;
        _cameraSize = Camera.main.orthographicSize;
        HudCorners.Clear();
        var safe = UnityEngine.Object.FindObjectOfType<ResponsiveGameUI>().SafeRoot;

        foreach (string path in new[] { "CatsCounterPanel/Counter", "LevelCounter", "WalletPanel/Total", "Options/OptionsButton", "Shop/ShopButton" })
        {
            var rect = (RectTransform)safe.Find(path);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            HudCorners.Add(rect, corners);
        }
    }

    private static void CheckGameFraming()
    {
        const float cameraTolerance = 0.001f;
        const float hudPositionTolerance = 0.01f;

        CheckThat(
            Vector3.Distance(Camera.main.transform.position, _cameraPosition) < cameraTolerance &&
            Mathf.Abs(Camera.main.orthographicSize - _cameraSize) < cameraTolerance,
            "Tutorial leaves camera position and zoom unchanged");

        foreach (var pair in HudCorners)
        {
            var corners = new Vector3[4];
            pair.Key.GetWorldCorners(corners);
            CheckThat(
                corners.Zip(pair.Value, (a, b) => Vector3.Distance(a, b)).All(d => d < hudPositionTolerance),
                "Tutorial leaves HUD placement unchanged: " + pair.Key.name);
        }
    }

    private static void Continue(TutorialOverlay overlay)
    {
        var button = overlay.transform.Find("Instruction/Continue").GetComponent<UnityEngine.UI.Button>();
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)), pointerId = -1 };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        CheckThat(hits.Count > 0 && hits[0].gameObject == button.gameObject, "Continue receives a real UI raycast");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static PointerEventData FocusPointer(TutorialOverlay overlay) => new(EventSystem.current)
    {
        position = RectTransformUtility.WorldToScreenPoint(null, overlay.transform.TransformPoint(overlay.SpotlightRect.center)), pointerId = -1,
    };

    private static void TapFocus(TutorialOverlay overlay)
    {
        var pointer = FocusPointer(overlay);
        AssertOverlayRaycast(overlay, pointer);
        overlay.OnPointerDown(pointer);
        overlay.OnPointerUp(pointer);
        overlay.OnPointerClick(pointer);
    }

    private static void AssertOverlayRaycast(TutorialOverlay overlay, PointerEventData pointer)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        CheckThat(hits.Count > 0 && hits[0].gameObject == overlay.gameObject, "Spotlight receives a real UI raycast");
    }

    private static IEnumerator CheckWin(TutorialController tutorial, int level)
    {
        const int winTimeoutSeconds = 60;

        CheckGameFraming();
        CheckThat(tutorial.IsRunning == false, "Final click finishes tutorial");
        CheckThat(UnityEngine.Object.FindObjectsOfType<Subscriber>().All(c => c.Clicks.InteractionAllowed), "Input restored");
        yield return Wait(() => UnityEngine.Object.FindObjectOfType<WinUI>(true).IsVisible, winTimeoutSeconds);
        CheckThat(UnityEngine.Object.FindObjectOfType<LooseUI>(true).IsVisible == false, "Following the guide wins level " + level);
    }

    private static IEnumerator StableCard(TutorialOverlay overlay, float seconds)
    {
        const float cardLayoutTolerance = 0.1f;

        Vector2 position = overlay.Card.anchoredPosition, size = overlay.Card.sizeDelta;
        double end = EditorApplication.timeSinceStartup + seconds;

        while (EditorApplication.timeSinceStartup < end)
        {
            CheckThat(
                Vector2.Distance(position, overlay.Card.anchoredPosition) < cardLayoutTolerance && Vector2.Distance(size, overlay.Card.sizeDelta) < cardLayoutTolerance,
                "Tip stays fixed while cats animate",
                false);
            var corners = new Vector3[4];
            overlay.Card.GetWorldCorners(corners);
            Vector2 min = overlay.transform.InverseTransformPoint(corners[0]), max = overlay.transform.InverseTransformPoint(corners[2]);
            var card = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            CheckThat(overlay.SpotlightRects.All(hole => card.Overlaps(hole) == false), "Animated accents stay clear of the fixed tip", false);
            yield return null;
        }

        Report.Add("PASS: tip stays fixed while cats animate");
    }

    private static TutorialController Tutorial() => UnityEngine.Object.FindObjectOfType<TutorialController>();

    private static TutorialOverlay Overlay() => UnityEngine.Object.FindObjectOfType<TutorialOverlay>();

    private static IEnumerator LoadLevel(int index)
    {
        const int sceneLoadTimeoutSeconds = 15;
        const float sceneSettleSeconds = 0.2f;

        LevelProgress.EditorPreviewIndex = index;
        var operation = SceneManager.LoadSceneAsync(ScenePath);
        yield return Wait(() => operation.isDone, sceneLoadTimeoutSeconds);
        yield return Seconds(sceneSettleSeconds);
        CaptureGameFraming();
    }

    private static IEnumerator Wait(Func<bool> condition, float timeout)
    {
        double end = EditorApplication.timeSinceStartup + timeout;

        while (condition() == false)
        {
            CheckThat(EditorApplication.timeSinceStartup < end, "Timed out waiting for gameplay", false);
            yield return null;
        }
    }

    private static IEnumerator Seconds(float seconds)
    {
        double end = EditorApplication.timeSinceStartup + seconds;

        while (EditorApplication.timeSinceStartup < end)
            yield return null;
    }

    private static void CheckThat(bool condition, string message, bool log = true)
    {
        if (condition == false)
        {
            Report.Add("FAIL: " + message);
            File.WriteAllLines(ReportPath, Report);
            throw new Exception("Tutorial: " + message);
        }

        if (log)
            Report.Add("PASS: " + message);
    }
}
