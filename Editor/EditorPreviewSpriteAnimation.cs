using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Runtime.Animation
{
    [CustomEditor(typeof(SpriteAnimation))]
    public class EditorPreviewSpriteAnimation : Editor
    {
        #region Members

        protected const float PANNING_SPEED = 0.5f;
        protected const string FPS_EDITOR_PREFS = "FPS";

        protected bool init = false;
        protected bool enabled = false;
        protected bool isPlaying = false;
        protected bool forceRepaint = false;
        protected bool loop = true;
        protected bool isPanning = false;
        protected bool saveToDisk = false;
        protected int currentFrame = 0;
        protected int loadedFPS = 30;
        protected int framesPerSecond = 30;
        protected int frameDurationCounter = 0;
        protected int frameListSelectedIndex = -1;
        protected float animationTimer = 0;
        protected float lastFrameEditorTime = 0;
        protected float deltaTime;
        protected Vector2 scrollWindowPosition;
        protected SpriteAnimation animation = null;
        protected ReorderableList frameList;
        protected List<AnimationFrame> frames;

        protected GUIStyle previewButtonSettings;
        protected GUIStyle preSlider;
        protected GUIStyle preSliderThumb;
        protected GUIStyle preLabel;
        protected GUIContent speedScale;
        protected GUIContent playButtonContent;
        protected GUIContent pauseButtonContent;
        protected GUIContent loopIcon;
        protected GUIContent loopIconActive;
        protected GameObject previewGameObject;
        protected GameObject cameraGameObject;
        protected Camera camera;
        protected SpriteRenderer spriteRenderer;
        
        protected string folderPath = "";

        #endregion Members

        #region Properties

        public int FramesPerSecond
        {
            get { return framesPerSecond; }
            set { framesPerSecond = value; }
        }

        public bool IsPlaying
        {
            get { return isPlaying; }
            set { isPlaying = value; }
        }

        public bool ForceRepaint
        {
            get { return forceRepaint; }
            set { forceRepaint = value; }
        }

        public SpriteAnimation CurrentAnimation
        {
            get { return animation; }
        }

        public int CurrentFrame
        {
            set { currentFrame = value; }
        }

        public bool Loop
        {
            get { return loop; }
            set { loop = value; }
        }

        public float Zoom
        {
            set
            {
                if (camera != null)
                {
                    float z = value / 50f;
                    if (camera.orthographicSize + z >= 0.1f &&
                        camera.orthographicSize + z <= 100)
                    {
                        camera.orthographicSize += z;
                    }
                }
            }
        }

        public bool IsPanning
        {
            get { return isPanning; }
            set { isPanning = value; }
        }

        #endregion Properties

        #region API Methods

        private void OnEnable()
        {
            if (!enabled)
            {
                enabled = true;
                currentFrame = 0;

                if (target == null)
                    return;

                if (animation == null)
                {
                    animation = (SpriteAnimation)target;
                    animation.Setup();
                }

                EditorApplication.update += Update;

                // Load last used settings.
                loadedFPS = framesPerSecond = EditorPrefs.GetInt(FPS_EDITOR_PREFS, 30);

                // Set up preview object and camera.
                previewGameObject = EditorUtility.CreateGameObjectWithHideFlags("previewGO", HideFlags.HideAndDontSave, typeof(SpriteRenderer));
                cameraGameObject = EditorUtility.CreateGameObjectWithHideFlags("cameraGO", HideFlags.HideAndDontSave, typeof(Camera));
                spriteRenderer = previewGameObject.GetComponent<SpriteRenderer>();
                camera = cameraGameObject.GetComponent<Camera>();

                // Set camera.
                camera.cameraType = CameraType.Preview;
                camera.clearFlags = CameraClearFlags.Depth;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = 3;
                camera.nearClipPlane = -10;
                camera.farClipPlane = 10;
                camera.targetDisplay = -1;
                camera.depth = -999;

                // Set renderer.
                if (animation != null && animation.FramesCount > 0)
                {
                    spriteRenderer.sprite = animation.Frames[0];
                    cameraGameObject.transform.position = Vector2.zero;
                }

                // Get preview culling layer in order to render only the preview object and nothing more.
                camera.cullingMask = -2147483648;
                previewGameObject.layer = 0x1f;

                // Also, disable the object to prevent render on scene/game views.
                previewGameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (enabled)
            {
                enabled = false;
                EditorApplication.update -= Update;

                if (frameList != null)
                {
                    frameList.drawHeaderCallback -= DrawFrameListHeader;
                    frameList.drawElementCallback -= DrawFrameListElement;
                    frameList.onAddCallback -= AddFrameListItem;
                    frameList.onRemoveCallback -= RemoveFrameListItem;
                    frameList.onSelectCallback -= SelectFrameListItem;
                    frameList.onReorderCallback -= ReorderFrameListItem;
                }

                if (previewGameObject != null)
                    DestroyImmediate(previewGameObject);

                if (cameraGameObject != null)
                    DestroyImmediate(cameraGameObject);
            }
        }

        #endregion API Methods

        #region Class Methods

        private void Update()
        {
            // Calculate deltaTime.
            float timeSinceStartup = (float)EditorApplication.timeSinceStartup;
            deltaTime = timeSinceStartup - lastFrameEditorTime;
            lastFrameEditorTime = timeSinceStartup;

            if (animation == null)
                return;

            if (frameList == null)
                InitializeReorderableList();

            CheckListOutOfSync();

            // Check animation bounds.
            if (currentFrame < 0)
                currentFrame = 0;
            else if (currentFrame > animation.FramesCount)
                currentFrame = animation.FramesCount - 1;

            // Check if playing and use the editor time to change frames.
            if (isPlaying)
            {
                animationTimer += deltaTime;
                float timePerFrame = 1f / framesPerSecond;

                if (animationTimer >= timePerFrame)
                {
                    // Check frame skips.
                    while (animationTimer >= timePerFrame)
                    {
                        frameDurationCounter++;
                        animationTimer -= timePerFrame;
                    }

                    if (frameDurationCounter >= animation.FramesDuration[currentFrame])
                    {
                        while (frameDurationCounter >= animation.FramesDuration[currentFrame])
                        {
                            frameDurationCounter -= animation.FramesDuration[currentFrame];
                            currentFrame++;
                            if (currentFrame >= animation.FramesCount)
                            {
                                currentFrame = 0;
                                if (!loop)
                                {
                                    isPlaying = false;
                                    return;
                                }
                            }

                            frameDurationCounter = 0;
                            Repaint();
                            forceRepaint = true;
                        }
                    }
                }
            }

            // Save preview FPS value on the editorPrefs.
            if (framesPerSecond != loadedFPS)
            {
                loadedFPS = framesPerSecond;
                EditorPrefs.SetInt(FPS_EDITOR_PREFS, framesPerSecond);
            }
        }

        private void CheckListOutOfSync()
        {
            bool outOfSync = false;

            if (frames.Count != animation.Frames.Count)
            {
                outOfSync = true;
            }
            else
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    if (frames[i].Duration != animation.FramesDuration[i] ||
                       frames[i].Frame != animation.Frames[i])
                    {
                        outOfSync = true;
                        break;
                    }
                }
            }

            if (outOfSync)
                InitializeReorderableList();
        }

        public override void OnInspectorGUI()
        {
            saveToDisk = false;

            Undo.RecordObject(animation, "Change FPS");
            animation.FPS = EditorGUILayout.IntField("FPS", animation.FPS);

            Undo.RecordObject(animation, "Postfix Duration");
            animation.UseSpriteNamePostFixAsDuration = EditorGUILayout.Toggle("Duration From Sprite Name", animation.UseSpriteNamePostFixAsDuration);
            if (animation.UseSpriteNamePostFixAsDuration)
            {
                if (animation != null && frames != null)
                {
                    for (int i = 0; i < animation.FramesCount; i++)
                    {
                        var frameName = animation.Frames[i].name;
                        var frameDuration = 1;
                        Match match = Regex.Match(frameName, @"\((\d+)\)");
                        if (match.Success)
                        {
                            string numberString = match.Groups[1].Value;
                            int number = int.Parse(numberString);
                            frameDuration = number;
                        }
                        animation.FramesDuration[i] = frameDuration;
                    }
                }
            }
            
            // FolderPathSelector folderSelector = (FolderPathSelector)target;
            
            EditorGUILayout.LabelField("Select a Folder", EditorStyles.boldLabel);
            Object obj = EditorGUILayout.ObjectField("Folder", null, typeof(DefaultAsset), false);

            // Check if the dragged object is a folder
            if (obj != null)
            {
                string pathTemp = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(pathTemp))
                {
                    // folderSelector.folderPath = path;
                    Debug.Log("Selected folder path: " + pathTemp);
                    folderPath = pathTemp;
                    EditorGUILayout.LabelField("Folder Path:", folderPath);
                }
                else
                {
                    Debug.LogWarning("The selected object is not a folder.");
                }
            }
            EditorGUILayout.LabelField("Folder Path:", folderPath);
        
            DrawButtons();

            if (frameList != null)
            {
                scrollWindowPosition = EditorGUILayout.BeginScrollView(scrollWindowPosition);

                // Individual frames.
                frameList.displayRemove = (animation.FramesCount > 0);
                frameList.DoLayoutList();
                EditorGUILayout.Space();

                EditorGUILayout.EndScrollView();
            }

            DrawButtons();

            if (GUI.changed || saveToDisk)
            {
                animation.Setup();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(animation);
                if (saveToDisk)
                    AssetDatabase.SaveAssets();
            }
        }

        private void DrawButtons()
        {
            if (animation.FramesCount > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    if (GUILayout.Button("Delete All Frames"))
                    {
                        Undo.RecordObject(animation, "Delete All Frames");

                        animation.Frames.Clear();
                        animation.FramesDuration.Clear();
                        InitializeReorderableList();
                        saveToDisk = true;
                    }

                    if (GUILayout.Button("Reverse Frames"))
                    {
                        Undo.RecordObject(animation, "Reverse Frames");

                        List<Sprite> prevFrames = new List<Sprite>(animation.Frames);
                        List<int> prevFramesDuration = new List<int>(animation.FramesDuration);

                        animation.Frames.Clear();
                        animation.FramesDuration.Clear();

                        for (int i = prevFrames.Count - 1; i >= 0; i--)
                        {
                            animation.Frames.Add(prevFrames[i]);
                            animation.FramesDuration.Add(prevFramesDuration[i]);
                        }

                        InitializeReorderableList();
                        saveToDisk = true;
                    }

                    if (GUILayout.Button("Load From Folder"))
                    {

                        if (string.IsNullOrEmpty(folderPath))
                        {
                            Debug.LogError("Folder path is empty. Please select a valid folder.");
                            return;
                        }

                        Undo.RecordObject(animation, "Load From Folder");

                        animation.Frames.Clear();
                        animation.FramesDuration.Clear();
                        InitializeReorderableList();
                        saveToDisk = true;

                        // Get all asset paths in the folder
                        string[] assetPaths = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

                        // Load each sprite and add to the list
                        int count = 0;
                        foreach (string assetGUID in assetPaths)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            if (sprite != null)
                            {
                                count++;
                                animation.Frames.Add(sprite);
                                animation.FramesDuration.Add((count % 2 ==0)? 7 : 8);
                                Debug.Log($"Loaded sprite: {sprite.name}");
                            }
                        }

                        InitializeReorderableList();
                        saveToDisk = true;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }

        public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
        {
            if (currentFrame >= 0 &&
                animation != null &&
                animation.FramesCount > 0 &&
                currentFrame < animation.FramesCount)
            {
                // Draw camera.
                spriteRenderer.sprite = animation.Frames[currentFrame];
                previewGameObject.SetActive(true);
                Handles.DrawCamera(rect, camera);
                previewGameObject.SetActive(false);

                // Check events.
                Event currentEvent = Event.current;

                // Zoom preview window with scrollwheel.
                if (currentEvent.type == EventType.ScrollWheel)
                {
                    Vector2 mpos = Event.current.mousePosition;
                    if (mpos.x >= rect.x && mpos.x <= rect.x + rect.width &&
                        mpos.y >= rect.y && mpos.y <= rect.y + rect.height)
                    {
                        Zoom = currentEvent.delta.y;
                    }
                    forceRepaint = true;
                    Repaint();
                }
                // Stop panning on mouse up.
                else if (currentEvent.type == EventType.MouseUp)
                {
                    isPanning = false;
                }
                // Pan the camera with mouse drag.
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    Vector2 mpos = Event.current.mousePosition;
                    if ((mpos.x >= rect.x && mpos.x <= rect.x + rect.width &&
                        mpos.y >= rect.y && mpos.y <= rect.y + rect.height) ||
                        isPanning)
                    {
                        Vector2 panning = Vector2.zero;
                        panning.x -= Event.current.delta.x;
                        panning.y += Event.current.delta.y;
                        cameraGameObject.transform.Translate(panning * PANNING_SPEED * deltaTime);
                        forceRepaint = true;
                        isPanning = true;
                        Repaint();
                    }
                }
                // Reset camera pressing F.
                else if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F)
                {
                    cameraGameObject.transform.position = Vector2.zero;
                    forceRepaint = true;
                    isPanning = true;
                    Repaint();
                }
            }
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Animation Preview");
        }

        public override void OnPreviewSettings()
        {
            if (!init)
            {
                // Define styles.
                previewButtonSettings = new GUIStyle("preButton");
                preSlider = new GUIStyle("preSlider");
                preSliderThumb = new GUIStyle("preSliderThumb");
                preLabel = new GUIStyle("preLabel");
                speedScale = EditorGUIUtility.IconContent("SpeedScale");
                playButtonContent = EditorGUIUtility.IconContent("PlayButton");
                pauseButtonContent = EditorGUIUtility.IconContent("PauseButton");
                loopIcon = EditorGUIUtility.IconContent("RotateTool");
                loopIconActive = EditorGUIUtility.IconContent("RotateTool On");
                init = true;
            }

            // Play Button.
            GUIContent buttonContent = isPlaying ? pauseButtonContent : playButtonContent;
            isPlaying = GUILayout.Toggle(isPlaying, buttonContent, previewButtonSettings);

            // Loop Button.
            GUIContent loopContent = loop ? loopIconActive : loopIcon;
            loop = GUILayout.Toggle(loop, loopContent, previewButtonSettings);

            // FPS Slider.
            GUILayout.Box(speedScale, preLabel);
            framesPerSecond = EditorGUILayout.IntField("FPS", framesPerSecond);
        }

        public override bool HasPreviewGUI()
        {
            return false; // (animation != null && animation.FramesCount > 0);
        }

        private void InitializeReorderableList()
        {
            if (animation == null)
                return;

            if (frames == null)
                frames = new List<AnimationFrame>();

            frames.Clear();

            for (int i = 0; i < animation.FramesCount; i++)
                frames.Add(new AnimationFrame(animation.Frames[i], animation.FramesDuration[i]));

            // Kill listener of the previous list.
            if (frameList != null)
            {
                frameList.drawHeaderCallback -= DrawFrameListHeader;
                frameList.drawElementCallback -= DrawFrameListElement;
                frameList.onAddCallback -= AddFrameListItem;
                frameList.onRemoveCallback -= RemoveFrameListItem;
                frameList.onSelectCallback -= SelectFrameListItem;
                frameList.onReorderCallback -= ReorderFrameListItem;
            }

            frameList = new ReorderableList(frames, typeof(AnimationFrame));
            frameList.drawHeaderCallback += DrawFrameListHeader;
            frameList.drawElementCallback += DrawFrameListElement;
            frameList.onAddCallback += AddFrameListItem;
            frameList.onRemoveCallback += RemoveFrameListItem;
            frameList.onSelectCallback += SelectFrameListItem;
            frameList.onReorderCallback += ReorderFrameListItem;
        }

        private void DrawFrameListHeader(Rect r)
        {
            GUI.Label(r, "Frame List");
        }

        private void DrawFrameListElement(Rect r, int i, bool active, bool focused)
        {
            if (speedScale == null)
                speedScale = EditorGUIUtility.IconContent("SpeedScale");

            if (i < animation.FramesCount)
            {
                EditorGUI.BeginChangeCheck();

                string spriteName = (animation.Frames[i] != null) ? animation.Frames[i].name : "No sprite selected";
                EditorGUIUtility.labelWidth = r.width - 105;
                animation.Frames[i] = EditorGUI.ObjectField(new Rect(r.x + 10, r.y + 1, r.width - 85, r.height - 4), spriteName, animation.Frames[i], typeof(Sprite), false) as Sprite;

                EditorGUIUtility.labelWidth = 20;
                animation.FramesDuration[i] = EditorGUI.IntField(new Rect(r.x + r.width - 50, r.y + 1, 50, r.height - 4), speedScale, animation.FramesDuration[i]);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(animation);
            }
        }

        private void AddFrameListItem(ReorderableList list)
        {
            Undo.RecordObject(animation, "Add Frame");
            AddFrame();
            EditorUtility.SetDirty(animation);
        }

        private void AddFrame()
        {
            frameList.list.Add(new AnimationFrame(null, 1));
            animation.Frames.Add(null);
            animation.FramesDuration.Add(1);
        }

        private void RemoveFrameListItem(ReorderableList list)
        {
            Undo.RecordObject(animation, "Remove Frame");

            int i = list.index;
            animation.Frames.RemoveAt(i);
            animation.FramesDuration.RemoveAt(i);
            frameList.list.RemoveAt(i);
            frameListSelectedIndex = frameList.index;

            if (i >= animation.FramesCount)
            {
                frameList.index -= 1;
                frameListSelectedIndex -= 1;
                currentFrame = frameListSelectedIndex;
                frameList.GrabKeyboardFocus();
            }

            EditorUtility.SetDirty(animation);
            Repaint();
        }

        private void ReorderFrameListItem(ReorderableList list)
        {
            Undo.RecordObject(animation, "Reorder Frames");

            Sprite s = animation.Frames[frameListSelectedIndex];
            animation.Frames.RemoveAt(frameListSelectedIndex);
            animation.Frames.Insert(list.index, s);

            int i = animation.FramesDuration[frameListSelectedIndex];
            animation.FramesDuration.RemoveAt(frameListSelectedIndex);
            animation.FramesDuration.Insert(list.index, i);

            EditorUtility.SetDirty(animation);
        }

        private void SelectFrameListItem(ReorderableList list)
        {
            currentFrame = list.index;
            forceRepaint = true;
            frameListSelectedIndex = list.index;
        }

        #endregion Class Methods
    }
}

public class FolderPathSelector : MonoBehaviour
{
    // The path of the selected folder
    public string folderPath;
}