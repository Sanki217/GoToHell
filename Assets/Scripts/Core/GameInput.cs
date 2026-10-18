using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
#endif

/// <summary>Every player-facing input action. Used for rebinding and prompt text.</summary>
public enum GameAction
{
    Move, Jump, Attack, AltAttack, Skill, Interact, LevelUp, Pause, ToggleStats, Aim
}

/// <summary>What the player is currently using. Drives aiming and button prompts.</summary>
public enum InputScheme { KeyboardMouse, Gamepad, Touch }

/// <summary>
/// Single place that maps player intent → physical input.
///
/// Gameplay asks for an ACTION (GameInput.JumpPressed, GameInput.AimDirectionFrom)
/// and never touches devices. This file runs on Unity's Input System package
/// (keyboard + mouse, any gamepad, touch through on-screen controls) and falls
/// back to the legacy Input Manager automatically if the project is switched
/// back (Project Settings → Player → Active Input Handling).
///
/// BINDINGS live in Assets/Resources/GameInputActions.inputactions — open it
/// in Unity's Input Actions editor to change keys. Players can rebind at
/// runtime (Settings → Controls); their overrides persist in PlayerPrefs.
///
/// AIM: keyboard/mouse aims at the cursor on the gameplay plane (z = 0);
/// gamepad and touch aim with the right stick, remembering the last direction
/// so releasing the stick on the same frame as the trigger still fires where
/// you were aiming.
///
/// MOBILE: on-screen sticks/buttons (MobileControls) feed a virtual gamepad,
/// so every binding below already works on touch — no gameplay changes needed.
/// </summary>
public static class GameInput
{
    /// <summary>Stick magnitude below which aim input is ignored.</summary>
    public const float AimDeadzone = 0.35f;

    public static InputScheme CurrentScheme { get; private set; } = InputScheme.KeyboardMouse;

    /// <summary>Fired when the player switches device type (prompts, cursor visibility).</summary>
    public static event Action<InputScheme> OnSchemeChanged;

    /// <summary>True when aiming follows the mouse cursor.</summary>
    public static bool UsesPointerAim => CurrentScheme == InputScheme.KeyboardMouse;

    private static Vector3 lastStickAim;

    // ================================================================
    //  ACTIONS
    // ================================================================

    public static float MoveX => Move.x;

    public static bool JumpPressed    => Pressed(GameAction.Jump);
    /// <summary>Hover shares the jump button: hold it in the air.</summary>
    public static bool HoverHeld      => Held(GameAction.Jump);

    public static bool AttackPressed  => Pressed(GameAction.Attack);
    public static bool AttackHeld     => Held(GameAction.Attack);
    public static bool AttackReleased => Released(GameAction.Attack);

    /// <summary>Modifier that turns the primary attack into the sword slash.</summary>
    public static bool AltAttackHeld  => Held(GameAction.AltAttack);

    /// <summary>Class skill: dash, shield, …</summary>
    public static bool SkillPressed   => Pressed(GameAction.Skill);
    public static bool SkillHeld      => Held(GameAction.Skill);

    public static bool InteractPressed    => Pressed(GameAction.Interact);
    public static bool LevelUpPressed     => Pressed(GameAction.LevelUp);
    public static bool PausePressed       => Pressed(GameAction.Pause);
    public static bool ToggleStatsPressed => Pressed(GameAction.ToggleStats);

    // ================================================================
    //  POINTER / AIM
    // ================================================================

    /// <summary>
    /// Cursor projected onto the world plane z = planeZ. Works for perspective
    /// and orthographic cameras at any zoom. Returns <paramref name="fallback"/>
    /// without a camera or when the ray is parallel to the plane.
    /// </summary>
    public static Vector3 PointerWorld(float planeZ = 0f, Vector3 fallback = default)
    {
        Camera cam = PlayerRefs.Cam;
        if (cam == null) return fallback;

        Ray ray = cam.ScreenPointToRay(PointerScreen);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
        return plane.Raycast(ray, out float dist) ? ray.GetPoint(dist) : fallback;
    }

    /// <summary>
    /// Normalized aim direction from <paramref name="origin"/> on the gameplay
    /// plane (z = 0): toward the cursor on mouse, the right stick on gamepad /
    /// touch. <paramref name="fallback"/> when there is no aim yet.
    /// </summary>
    public static Vector3 AimDirectionFrom(Vector3 origin, Vector3 fallback)
    {
        if (UsesPointerAim)
        {
            Vector3 dir = PointerWorld(origin.z, origin) - origin;
            dir.z = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : fallback;
        }

        Vector2 stick = Aim;
        if (stick.sqrMagnitude >= AimDeadzone * AimDeadzone)
            lastStickAim = new Vector3(stick.x, stick.y, 0f).normalized;
        else if (lastStickAim == Vector3.zero)
        {
            // Never aimed yet: use movement direction, then the caller's fallback
            Vector2 move = Move;
            if (move.sqrMagnitude >= AimDeadzone * AimDeadzone)
                return new Vector3(move.x, move.y, 0f).normalized;
            return fallback;
        }
        return lastStickAim;
    }

    /// <summary>
    /// Aim TARGET point (dash teleport, ground-targeted skills): the cursor on
    /// mouse; origin + aim direction × <paramref name="maxDistance"/> on gamepad.
    /// </summary>
    public static Vector3 AimPoint(Vector3 origin, float maxDistance)
    {
        if (UsesPointerAim) return PointerWorld(origin.z, origin);
        return origin + AimDirectionFrom(origin, Vector3.right) * maxDistance;
    }

#if ENABLE_INPUT_SYSTEM
    // ================================================================
    //  INPUT SYSTEM BACKEND
    // ================================================================

    private const string BindingsPrefKey = "input.bindings.v1";

    private static InputActionAsset asset;
    private static InputAction[] actions;
    private static IDisposable anyButtonListener;
    private static int anyPressFrame = -1;

    /// <summary>The runtime action asset (for rebinding UIs or sharing with UI input).</summary>
    public static InputActionAsset Asset { get { Ensure(); return asset; } }

    public static InputAction Get(GameAction a) { Ensure(); return actions[(int)a]; }

    public static Vector2 Move => Get(GameAction.Move).ReadValue<Vector2>();
    public static Vector2 Aim  => Get(GameAction.Aim).ReadValue<Vector2>();

    public static bool AnyKeyPressed { get { Ensure(); return anyPressFrame == Time.frameCount; } }

    public static Vector3 PointerScreen
    {
        get
        {
            Pointer p = Pointer.current;
            return p != null ? (Vector3)p.position.ReadValue() : Vector3.zero;
        }
    }

    /// <summary>Raw key check for debug tools. Gameplay should use actions.</summary>
    public static bool KeyPressed(KeyCode key)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !Enum.TryParse(key.ToString(), out Key k) || k == Key.None) return false;
        return kb[k].wasPressedThisFrame;
    }

    private static bool Pressed(GameAction a)  => Get(a).WasPressedThisFrame();
    private static bool Held(GameAction a)     => Get(a).IsPressed();
    private static bool Released(GameAction a) => Get(a).WasReleasedThisFrame();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Ensure()
    {
        if (asset != null) return;

        asset = LoadActions(out actions);
        string json = PlayerPrefs.GetString(BindingsPrefKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { asset.LoadBindingOverridesFromJson(json); }
            catch (Exception e) { Debug.LogWarning($"[GameInput] Ignoring broken binding overrides: {e.Message}"); }
        }

        foreach (InputAction a in asset)
            a.performed += ctx => NoteDevice(ctx.control != null ? ctx.control.device : null);

        anyButtonListener = InputSystem.onAnyButtonPress.Call(ctrl =>
        {
            anyPressFrame = Time.frameCount;
            NoteDevice(ctrl != null ? ctrl.device : null);
        });

        asset.Enable();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        anyButtonListener?.Dispose();
        anyButtonListener = null;
        if (asset != null) { asset.Disable(); UnityEngine.Object.Destroy(asset); }
        asset = null;
        actions = null;
        anyPressFrame = -1;
        lastStickAim = Vector3.zero;
        CurrentScheme = InputScheme.KeyboardMouse;
        OnSchemeChanged = null;
    }

    /// <summary>Resources path of the editable bindings asset (Assets/Resources/GameInputActions.inputactions).</summary>
    public const string ActionsResource = "GameInputActions";

    /// <summary>
    /// The bindings come from Assets/Resources/GameInputActions.inputactions —
    /// edit them in Unity's Input Actions editor (double-click the asset).
    /// Action names must match GameAction. Without the asset, the defaults in
    /// CreateDefaultActions() are used (Tools → Go To Hell → Input → Create
    /// Input Actions Asset writes them out).
    /// </summary>
    private static InputActionAsset LoadActions(out InputAction[] list)
    {
        InputActionAsset source = Resources.Load<InputActionAsset>(ActionsResource);
        if (source != null)
        {
            // Work on a copy so runtime rebinds never dirty the project asset
            InputActionAsset copy = UnityEngine.Object.Instantiate(source);
            list = new InputAction[Enum.GetValues(typeof(GameAction)).Length];
            bool complete = true;
            foreach (GameAction a in (GameAction[])Enum.GetValues(typeof(GameAction)))
            {
                list[(int)a] = copy.FindAction(a.ToString());
                if (list[(int)a] == null)
                {
                    complete = false;
                    Debug.LogError($"[GameInput] '{ActionsResource}' has no action named '{a}' — using default bindings.");
                }
            }
            if (complete) return copy;
            UnityEngine.Object.Destroy(copy);
        }
        return CreateDefaultActions(out list);
    }

    /// <summary>Default bindings. Index of each action == (int)GameAction.</summary>
    public static InputActionAsset CreateDefaultActions() => CreateDefaultActions(out _);
    private static InputActionAsset CreateDefaultActions(out InputAction[] list)
    {
        var a = ScriptableObject.CreateInstance<InputActionAsset>();
        a.name = ActionsResource;
        InputActionMap map = a.AddActionMap("Gameplay");
        list = new InputAction[Enum.GetValues(typeof(GameAction)).Length];

        InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddBinding("<Gamepad>/dpad");
        list[(int)GameAction.Move] = move;

        list[(int)GameAction.Jump]        = Button(map, "Jump",        "<Keyboard>/space",     "<Gamepad>/buttonSouth");
        list[(int)GameAction.Attack]      = Button(map, "Attack",      "<Mouse>/leftButton",   "<Gamepad>/rightTrigger");
        list[(int)GameAction.AltAttack]   = Button(map, "AltAttack",   "<Keyboard>/leftShift", "<Gamepad>/leftTrigger");
        list[(int)GameAction.Skill]       = Button(map, "Skill",       "<Mouse>/rightButton",  "<Gamepad>/rightShoulder");
        list[(int)GameAction.Interact]    = Button(map, "Interact",    "<Keyboard>/e",         "<Gamepad>/buttonNorth");
        list[(int)GameAction.LevelUp]     = Button(map, "LevelUp",     "<Keyboard>/q",         "<Gamepad>/buttonWest");
        list[(int)GameAction.Pause]       = Button(map, "Pause",       "<Keyboard>/escape",    "<Gamepad>/start");
        list[(int)GameAction.ToggleStats] = Button(map, "ToggleStats", "<Keyboard>/tab",       "<Gamepad>/select");

        InputAction aim = map.AddAction("Aim", InputActionType.Value, expectedControlLayout: "Vector2");
        aim.AddBinding("<Gamepad>/rightStick");
        list[(int)GameAction.Aim] = aim;

        // Not a gameplay action: only lets "moved the mouse" switch the scheme
        // back to keyboard/mouse (aim follows the cursor again).
        map.AddAction("PointerMove", InputActionType.PassThrough, "<Mouse>/delta");

        return a;
    }

    private static InputAction Button(InputActionMap map, string name, string keyboardMouse, string gamepad)
    {
        InputAction act = map.AddAction(name, InputActionType.Button);
        act.AddBinding(keyboardMouse);
        act.AddBinding(gamepad);
        return act;
    }

    private static void NoteDevice(InputDevice device)
    {
        if (device == null) return;

        InputScheme scheme;
        if (device is Touchscreen) scheme = InputScheme.Touch;
        else if (device is Gamepad)
            // On-screen controls drive a virtual (non-native) gamepad
            scheme = !device.native && Touchscreen.current != null ? InputScheme.Touch : InputScheme.Gamepad;
        else if (device is Keyboard || device is Mouse) scheme = InputScheme.KeyboardMouse;
        else return;

        if (scheme == CurrentScheme) return;
        CurrentScheme = scheme;
        OnSchemeChanged?.Invoke(scheme);
    }

    // ================================================================
    //  PROMPTS & REBINDING
    // ================================================================

    /// <summary>Short label of the binding for the current device, e.g. "Q" or "X".</summary>
    public static string BindingDisplay(GameAction a)
    {
        InputAction act = Get(a);
        int i = BindingIndexFor(act, CurrentScheme == InputScheme.KeyboardMouse ? InputScheme.KeyboardMouse : InputScheme.Gamepad);
        return i >= 0 ? act.GetBindingDisplayString(i) : act.GetBindingDisplayString();
    }

    /// <summary>Binding label for a specific device family ("—" if none), for rebinding screens.</summary>
    public static string BindingDisplay(GameAction a, InputScheme scheme)
    {
        InputAction act = Get(a);
        int i = BindingIndexFor(act, scheme == InputScheme.KeyboardMouse ? InputScheme.KeyboardMouse : InputScheme.Gamepad);
        return i >= 0 ? act.GetBindingDisplayString(i) : "—";
    }

    /// <summary>
    /// Index of the first non-composite binding for a device family, or −1.
    /// Use with StartRebind to rebind "the keyboard key" or "the gamepad button".
    /// </summary>
    public static int BindingIndexFor(InputAction act, InputScheme scheme)
    {
        if (act == null) return -1;
        for (int i = 0; i < act.bindings.Count; i++)
        {
            InputBinding b = act.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            string path = b.effectivePath ?? "";
            bool pad = path.StartsWith("<Gamepad>");
            if (scheme == InputScheme.KeyboardMouse ? !pad : pad) return i;
        }
        return -1;
    }

    /// <summary>
    /// Interactive rebind: waits for the next button press and stores it.
    /// Escape cancels. <paramref name="onDone"/> gets true when a new binding
    /// was saved. Returns false if there was nothing to rebind.
    /// </summary>
    public static bool StartRebind(GameAction a, InputScheme scheme, Action<bool> onDone = null)
    {
        InputAction act = Get(a);
        int index = BindingIndexFor(act, scheme);
        if (index < 0) return false;

        act.Disable();
        act.PerformInteractiveRebinding(index)
           .WithCancelingThrough("<Keyboard>/escape")
           .OnComplete(op => { op.Dispose(); act.Enable(); SaveBindings(); onDone?.Invoke(true); })
           .OnCancel(op => { op.Dispose(); act.Enable(); onDone?.Invoke(false); })
           .Start();
        return true;
    }

    public static void ResetBindings()
    {
        Ensure();
        asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingsPrefKey);
        PlayerPrefs.Save();
    }

    private static void SaveBindings()
    {
        PlayerPrefs.SetString(BindingsPrefKey, asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

#else
    // ================================================================
    //  LEGACY INPUT MANAGER BACKEND (Active Input Handling = Old)
    // ================================================================

    private static readonly KeyCode[] keys =
    {
        KeyCode.None,        // Move (axis)
        KeyCode.Space,       // Jump
        KeyCode.Mouse0,      // Attack
        KeyCode.LeftShift,   // AltAttack
        KeyCode.Mouse1,      // Skill
        KeyCode.E,           // Interact
        KeyCode.Q,           // LevelUp
        KeyCode.Escape,      // Pause
        KeyCode.Tab,         // ToggleStats
        KeyCode.None,        // Aim (stick)
    };

    public static Vector2 Move => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public static Vector2 Aim  => Vector2.zero;

    public static bool AnyKeyPressed => Input.anyKeyDown;
    public static Vector3 PointerScreen => Input.mousePosition;
    public static bool KeyPressed(KeyCode key) => Input.GetKeyDown(key);

    private static bool Pressed(GameAction a)  => a == GameAction.Jump ? Input.GetButtonDown("Jump") : Input.GetKeyDown(keys[(int)a]);
    private static bool Held(GameAction a)     => Input.GetKey(keys[(int)a]);
    private static bool Released(GameAction a) => Input.GetKeyUp(keys[(int)a]);

    public static string BindingDisplay(GameAction a) => keys[(int)a].ToString();
    public static string BindingDisplay(GameAction a, InputScheme scheme) =>
        scheme == InputScheme.KeyboardMouse ? keys[(int)a].ToString() : "—";

    public static bool StartRebind(GameAction a, InputScheme scheme, Action<bool> onDone = null) => false;
    public static void ResetBindings() { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        lastStickAim = Vector3.zero;
        CurrentScheme = InputScheme.KeyboardMouse;
        OnSchemeChanged = null;
    }
#endif
}
