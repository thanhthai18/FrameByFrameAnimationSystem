using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Runtime.Animation
{
    public class EditorSpriteAnimation : EditorWindow
    {
        #region Members

        protected const float DROP_AREA_HEIGHT = 50;
        protected const float MIN_WINDOW_WIDTH = 500f;
        protected const float MIN_WINDOW_HEIGHT = 200f;

        protected bool init = false;
        protected bool justCreatedAnim = false;
        protected int frameListSelectedIndex = -1;
        protected int fps;
        protected bool useSpriteNamePostFixAsDuration = false;
        protected Texture2D clockIcon = null;
        protected SpriteAnimation selectedAnimation = null;
        protected Vector2 scrollWindowPosition = Vector2.zero;
        protected List<Sprite> draggedSprites = null;
        protected EditorPreviewSpriteAnimation spritePreview = null;
        protected ReorderableList frameList;
        protected List<AnimationFrame> frames;

        protected GUIStyle box;
        protected GUIStyle dragAndDropBox;
        protected GUIStyle lowPaddingBox;
        protected GUIStyle buttonStyle;
        protected GUIStyle sliderStyle;
        protected GUIStyle sliderThumbStyle;
        protected GUIStyle labelStyle;
        protected GUIStyle previewToolBar;
        protected GUIStyle preview;
        protected GUIContent playButtonContent;
        protected GUIContent pauseButtonContent;
        protected GUIContent speedScaleIcon;
        protected GUIContent loopIcon;
        protected GUIContent loopIconActive;

        #endregion Members

        #region API Methods

        private void OnEnable()
        {
            // Get clock icon.
            if (clockIcon == null)
                clockIcon = Resources.Load<Texture2D>("clockIcon");

            // Initialize.
            draggedSprites = new List<Sprite>();
            init = false;

            // Events.
            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
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
        }

        private void OnSelectionChange()
        {
            // Change animation if we select an animation on the project.
            if (Selection.activeObject != null && Selection.activeObject.GetType() == typeof(SpriteAnimation))
            {
                SpriteAnimation sa = Selection.activeObject as SpriteAnimation;
                if (sa != selectedAnimation)
                {
                    selectedAnimation = sa;
                    spritePreview = null;
                    InitializeReorderableList();
                    Repaint();
                }
            }
        }

        private void Update()
        {
            if (selectedAnimation != null && frames != null)
            {
                if (selectedAnimation.FPS != fps || selectedAnimation.UseSpriteNamePostFixAsDuration != useSpriteNamePostFixAsDuration)
                    Repaint();
                CheckListOutOfSync();
            }

            if (spritePreview != null)
            {
                if (spritePreview.IsPlaying && frames.Count == 0)
                    spritePreview.IsPlaying = false;
            }

            // Only force repaint on update if the preview is playing and has changed the frame.
            if (spritePreview != null &&
               (spritePreview.IsPlaying || spritePreview.IsPanning) &&
                spritePreview.ForceRepaint)
            {
                spritePreview.ForceRepaint = false;
                Repaint();
            }
        }

        #endregion API Methods

        private void OnGUI()
        {
            // Style initialization.
            if (!init)
            {
                Initialize();
                init = true;
            }

            // Create animation box.
            NewAnimationBox();

            if (justCreatedAnim)
            {
                justCreatedAnim = false;
                return;
            }

            // Edit animation box.
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            {
                // Animation asset field.
                if (selectedAnimation == null)
                {
                    EditorGUILayout.BeginVertical(box);
                    selectedAnimation = EditorGUILayout.ObjectField("Animation", selectedAnimation, typeof(SpriteAnimation), false) as SpriteAnimation;
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    // Init reorderable list.
                    if (frameList == null)
                        InitializeReorderableList();

                    // Add the frames dropped on the drag and drop box.
                    if (draggedSprites != null && draggedSprites.Count > 0)
                    {
                        for (int i = 0; i < draggedSprites.Count; i++)
                            AddFrame(draggedSprites[i]);
                        draggedSprites.Clear();

                        SaveFile(true);
                    }

                    // Retrocompatibility check for the new frames duration field.
                    if (selectedAnimation.FramesCount != selectedAnimation.FramesDuration.Count)
                    {
                        selectedAnimation.FramesDuration.Clear();
                        for (int i = 0; i < selectedAnimation.FramesCount; i++)
                            selectedAnimation.FramesDuration.Add(1);
                    }

                    // Config settings.
                    ConfigBox();

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    {
                        // Preview window setup.
                        Rect previewRect = EditorGUILayout.BeginVertical(lowPaddingBox, GUILayout.MaxWidth(position.width / 2));
                        PreviewBox(previewRect);
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical();
                        {
                            // FPS .
                            EditorGUI.BeginChangeCheck();
                            {
                                Undo.RecordObject(selectedAnimation, "Change FPS");
                                fps = EditorGUILayout.IntField("FPS", selectedAnimation.FPS);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(selectedAnimation, "Change FPS");
                                selectedAnimation.FPS = fps;
                                if (selectedAnimation.FPS < 0)
                                    selectedAnimation.FPS = 0;
                            }

                            EditorGUILayout.Space();

                            // Duration from sprite name's postfix.
                            EditorGUI.BeginChangeCheck();
                            {
                                Undo.RecordObject(selectedAnimation, "Postfix Duration");
                                useSpriteNamePostFixAsDuration = EditorGUILayout.Toggle("Duration From Sprite Name", selectedAnimation.UseSpriteNamePostFixAsDuration);
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(selectedAnimation, "Postfix Duration");
                                selectedAnimation.UseSpriteNamePostFixAsDuration = useSpriteNamePostFixAsDuration;
                                if (selectedAnimation.UseSpriteNamePostFixAsDuration)
                                {
                                    if (selectedAnimation != null && frames != null)
                                    {
                                        for (int i = 0; i < selectedAnimation.FramesCount; i++)
                                        {
                                            var frameName = selectedAnimation.Frames[i].name;
                                            var frameDuration = 1;
                                            Match match = Regex.Match(frameName, @"\((\d+)\)");
                                            if (match.Success)
                                            {
                                                string numberString = match.Groups[1].Value;
                                                int number = int.Parse(numberString);
                                                frameDuration = number;
                                            }
                                            selectedAnimation.FramesDuration[i] = frameDuration;
                                        }
                                    }
                                }
                            }

                            EditorGUILayout.Space();

                            scrollWindowPosition = EditorGUILayout.BeginScrollView(scrollWindowPosition);
                            {
                                // Individual frames.
                                frameList.displayRemove = (selectedAnimation.FramesCount > 0);
                                frameList.DoLayoutList();
                                EditorGUILayout.Space();
                            }
                            EditorGUILayout.EndScrollView();

                            if (selectedAnimation.FramesCount > 0)
                            {
                                // Display total animation frames count.
                                var totalAnimationFrameCount = selectedAnimation.FramesDuration.Sum(x => x );
                                EditorGUILayout.LabelField($"Total Frames Count: {totalAnimationFrameCount}");
                                EditorGUILayout.Space();

                                // Display the animation time.
                                var animationTime = totalAnimationFrameCount * 1.0f / selectedAnimation.FPS;
                                EditorGUILayout.LabelField($"Animation Time: {animationTime}s");
                                EditorGUILayout.Space();

                                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                                {
                                    if (GUILayout.Button("Delete All Frames"))
                                    {
                                        Undo.RecordObject(selectedAnimation, "Delete All Frames");

                                        spritePreview.IsPlaying = false;
                                        selectedAnimation.Frames.Clear();
                                        selectedAnimation.FramesDuration.Clear();
                                        InitializeReorderableList();
                                        SaveFile(true);
                                    }

                                    if (GUILayout.Button("Reverse Frames"))
                                    {
                                        Undo.RecordObject(selectedAnimation, "Reverse Frames");

                                        List<Sprite> prevFrames = new List<Sprite>(selectedAnimation.Frames);
                                        List<int> prevFramesDuration = new List<int>(selectedAnimation.FramesDuration);

                                        selectedAnimation.Frames.Clear();
                                        selectedAnimation.FramesDuration.Clear();

                                        for (int i = prevFrames.Count - 1; i >= 0; i--)
                                        {
                                            selectedAnimation.Frames.Add(prevFrames[i]);
                                            selectedAnimation.FramesDuration.Add(prevFramesDuration[i]);
                                        }

                                        InitializeReorderableList();
                                        SaveFile(true);
                                    }

                                    if (GUILayout.Button("Load From Folder"))
                                    {
                                        Undo.RecordObject(selectedAnimation, "Load From Folder");

                                        // List<Sprite> prevFrames = new List<Sprite>(selectedAnimation.Frames);
                                        // List<int> prevFramesDuration = new List<int>(selectedAnimation.FramesDuration);

                                        // selectedAnimation.Frames.Clear();
                                        // selectedAnimation.FramesDuration.Clear();

                                        // for (int i = prevFrames.Count - 1; i >= 0; i--)
                                        // {
                                        //     selectedAnimation.Frames.Add(prevFrames[i]);
                                        //     selectedAnimation.FramesDuration.Add(prevFramesDuration[i]);
                                        // }

                                        // InitializeReorderableList();
                                        // SaveFile(true);
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.Space();
                        }
                        EditorGUILayout.EndVertical();

                        // Check Events.
                        Event evt = Event.current;
                        switch (evt.type)
                        {
                            // Delete frames.
                            case EventType.KeyDown:
                                if (Event.current.keyCode == KeyCode.Delete &&
                                    selectedAnimation.FramesCount > 0 &&
                                    frameList.HasKeyboardControl() &&
                                    frameListSelectedIndex != -1)
                                {
                                    RemoveFrameListItem(frameList);
                                }
                                break;

                            // Zoom preview window with scrollwheel.
                            case EventType.ScrollWheel:
                                if (spritePreview != null)
                                {
                                    Vector2 mpos = Event.current.mousePosition;
                                    if (mpos.x >= previewRect.x && mpos.x <= previewRect.x + previewRect.width &&
                                        mpos.y >= previewRect.y && mpos.y <= previewRect.y + previewRect.height)
                                    {
                                        Repaint();
                                        spritePreview.Zoom = evt.delta.y;
                                    }
                                }
                                break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            if (GUI.changed && selectedAnimation != null)
                SaveFile();
        }

        #region Class Methods

        [MenuItem("Tools/Sprite Animation/Sprite Editor", false, 0)]
        private static void ShowWindow()
        {
            GetWindow(typeof(EditorSpriteAnimation), false, "Sprite Animation");
        }

        [MenuItem("Tools/Sprite Animation/Create Sprite Animation")]
        public static void CreateAsset()
        {
            SpriteAnimation asset = CreateInstance<SpriteAnimation>();
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
                path = "Assets";
            else if (System.IO.Path.GetExtension(path) != "")
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New Animation.asset");
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CheckListOutOfSync()
        {
            bool outOfSync = false;

            if (selectedAnimation.Frames == null || frames.Count != selectedAnimation.Frames.Count)
            {
                outOfSync = true;
            }
            else
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    if (frames[i].Duration != selectedAnimation.FramesDuration[i] ||
                        frames[i].Frame != selectedAnimation.Frames[i])
                    {
                        outOfSync = true;
                        break;
                    }
                }
            }

            if (outOfSync)
            {
                InitializeReorderableList();
                Repaint();
            }
        }

        private void Initialize()
        {
            minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            buttonStyle = new GUIStyle("preButton");
            sliderStyle = new GUIStyle("preSlider");
            sliderThumbStyle = new GUIStyle("preSliderThumb");
            labelStyle = new GUIStyle("preLabel");
            box = new GUIStyle(EditorStyles.helpBox);
            playButtonContent = EditorGUIUtility.IconContent("PlayButton");
            pauseButtonContent = EditorGUIUtility.IconContent("PauseButton");
            speedScaleIcon = EditorGUIUtility.IconContent("SpeedScale");
            loopIcon = EditorGUIUtility.IconContent("RotateTool");
            loopIconActive = EditorGUIUtility.IconContent("RotateTool On");
            lowPaddingBox = new GUIStyle(EditorStyles.helpBox);
            lowPaddingBox.padding = new RectOffset(1, 1, 1, 1);
            lowPaddingBox.stretchWidth = true;
            lowPaddingBox.stretchHeight = true;
            previewToolBar = new GUIStyle("RectangleToolHBar");
            preview = new GUIStyle("CurveEditorBackground");
            dragAndDropBox = new GUIStyle(EditorStyles.helpBox);
            dragAndDropBox.richText = true;
            dragAndDropBox.alignment = TextAnchor.MiddleCenter;
        }

        private void InitializeReorderableList()
        {
            if (frames == null)
                frames = new List<AnimationFrame>();

            frames.Clear();

            if (selectedAnimation == null)
                return;

            for (int i = 0; i < selectedAnimation.FramesCount; i++)
                frames.Add(new AnimationFrame(selectedAnimation.Frames[i], selectedAnimation.FramesDuration[i]));

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

        /// <summary>
        /// Draws the new animation box.
        /// </summary>
        private void NewAnimationBox()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            {
                GUILayout.FlexibleSpace();
                // New animaton button.
                if (GUILayout.Button("Create Animation", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    CreateAnimation();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the box with the name and file of the animation.
        /// </summary>
        private void ConfigBox()
        {
            EditorGUILayout.BeginVertical(box);
            {
                SpriteAnimation newSpriteAnimation = EditorGUILayout.ObjectField("Animation", selectedAnimation, typeof(SpriteAnimation), false) as SpriteAnimation;
                if (newSpriteAnimation == null)
                    return;

                // Reset preview and list if we select a new animation.
                if (newSpriteAnimation != selectedAnimation)
                {
                    selectedAnimation = newSpriteAnimation;
                    InitializeReorderableList();
                    spritePreview = (EditorPreviewSpriteAnimation)Editor.CreateEditor(selectedAnimation, typeof(EditorPreviewSpriteAnimation));
                }

                EditorGUILayout.Space();
                DragAndDropBox();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the drag and drop box and saves the dragged objects.
        /// </summary>
        private void DragAndDropBox()
        {
            // Drag and drop box for sprite frames.
            Rect dropArea = GUILayoutUtility.GetRect(0f, DROP_AREA_HEIGHT, GUILayout.ExpandWidth(true));
            Event evt = Event.current;
            GUI.Box(dropArea, "Drop sprites <b>HERE</b> to add frames automatically.", dragAndDropBox);
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            DragAndDrop.AcceptDrag();
                            draggedSprites.Clear();
                            foreach (var draggedObject in DragAndDrop.objectReferences)
                            {
                                // Get dragged sprites.
                                Sprite s = draggedObject as Sprite;
                                if (s != null)
                                {
                                    draggedSprites.Add(s);
                                }
                                else
                                {
                                    // If the object is a complete texture, get all the sprites in it.
                                    Texture2D t = draggedObject as Texture2D;
                                    if (t != null)
                                    {
                                        string texturePath = AssetDatabase.GetAssetPath(t);
                                        Sprite[] spritesInTexture = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToArray();
                                        for (int i = 0; i < spritesInTexture.Length; i++)
                                            draggedSprites.Add(spritesInTexture[i]);
                                    }
                                }
                            }

                            if (draggedSprites.Count > 1)
                                draggedSprites.Sort(new SpriteSorter());
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Draws the preview window.
        /// </summary>
        /// <param name="r">Draw rect.</param>
        private void PreviewBox(Rect r)
        {
            if (spritePreview == null || spritePreview.CurrentAnimation != selectedAnimation)
                spritePreview = (EditorPreviewSpriteAnimation)Editor.CreateEditor(selectedAnimation, typeof(EditorPreviewSpriteAnimation));

            if (spritePreview != null)
            {
                EditorGUILayout.BeginVertical(preview, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                {
                    r.height -= 21;
                    r.width -= 2;
                    r.y += 1;
                    r.x += 1;
                    spritePreview.OnInteractivePreviewGUI(r, EditorStyles.whiteLabel);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal(previewToolBar);
                {
                    // Play Button.
                    GUIContent buttonContent = spritePreview.IsPlaying ? pauseButtonContent : playButtonContent;
                    spritePreview.IsPlaying = GUILayout.Toggle(spritePreview.IsPlaying, buttonContent, buttonStyle, GUILayout.Width(40));

                    // Loop Button.
                    GUIContent loopContent = spritePreview.Loop ? loopIconActive : loopIcon;
                    spritePreview.Loop = GUILayout.Toggle(spritePreview.Loop, loopContent, buttonStyle, GUILayout.Width(40));

                    // FPS Field.
                    spritePreview.FramesPerSecond = EditorGUILayout.IntField("FPS (1 Sprite = 1.0f / FPS):", spritePreview.FramesPerSecond);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFrameListHeader(Rect r)
        {
            GUI.Label(r, "Frame List");
        }

        private void DrawFrameListElement(Rect r, int i, bool active, bool focused)
        {
            EditorGUI.BeginChangeCheck();
            {
                string spriteName = (selectedAnimation.Frames[i] != null) ? selectedAnimation.Frames[i].name : "No sprite selected";
                EditorGUI.LabelField(new Rect(r.x, r.y + 2, r.width, r.height), spriteName);
                selectedAnimation.Frames[i] = EditorGUI.ObjectField(new Rect(r.x + r.width - 120, r.y + 1, 50, r.height - 4), "", selectedAnimation.Frames[i], typeof(Sprite), false) as Sprite;
                EditorGUIUtility.labelWidth = 20;
                selectedAnimation.FramesDuration[i] = EditorGUI.IntField(new Rect(r.x + r.width - 50, r.y + 1, 50, r.height - 4), speedScaleIcon, selectedAnimation.FramesDuration[i]);
            }
            if (EditorGUI.EndChangeCheck())
                SaveFile(true);
        }

        private void AddFrameListItem(ReorderableList list)
        {
            Undo.RecordObject(selectedAnimation, "Add Frame");
            AddFrame();
            SaveFile(true);
        }

        private void RemoveFrameListItem(ReorderableList list)
        {
            Undo.RecordObject(selectedAnimation, "Remove Frame");

            int i = list.index;
            selectedAnimation.Frames.RemoveAt(i);
            selectedAnimation.FramesDuration.RemoveAt(i);
            frameList.list.RemoveAt(i);
            frameListSelectedIndex = frameList.index;

            if (i >= selectedAnimation.FramesCount)
            {
                frameList.index -= 1;
                frameListSelectedIndex -= 1;
                spritePreview.CurrentFrame = frameListSelectedIndex;
                frameList.GrabKeyboardFocus();
            }

            Repaint();
            SaveFile(true);
        }

        private void ReorderFrameListItem(ReorderableList list)
        {
            Undo.RecordObject(selectedAnimation, "Reorder Frames");

            Sprite s = selectedAnimation.Frames[frameListSelectedIndex];
            selectedAnimation.Frames.RemoveAt(frameListSelectedIndex);
            selectedAnimation.Frames.Insert(list.index, s);

            int i = selectedAnimation.FramesDuration[frameListSelectedIndex];
            selectedAnimation.FramesDuration.RemoveAt(frameListSelectedIndex);
            selectedAnimation.FramesDuration.Insert(list.index, i);

            SaveFile(true);
        }

        private void SelectFrameListItem(ReorderableList list)
        {
            spritePreview.CurrentFrame = list.index;
            spritePreview.ForceRepaint = true;
            frameListSelectedIndex = list.index;
        }

        /// <summary>
        /// Adds an empty frame.
        /// </summary>
        private void AddFrame()
        {
            frameList.list.Add(new AnimationFrame(null, 1));
            selectedAnimation.Frames.Add(null);
            selectedAnimation.FramesDuration.Add(1);
        }

        /// <summary>
        /// Adds a frame with specified sprite.
        /// </summary>
        /// <param name="sprite">Sprite to add.</param>
        private void AddFrame(Sprite sprite)
        {
            frameList.list.Add(new AnimationFrame(sprite, 1));
            selectedAnimation.Frames.Add(sprite);
            selectedAnimation.FramesDuration.Add(1);
        }

        /// <summary>
        /// Creates the animation asset with a prompt.
        /// </summary>
        private void CreateAnimation()
        {
            string folder = EditorUtility.SaveFilePanel("New Animation", "Assets", "New Animation", "asset");
            string relativeFolder = folder;

            if (folder.Length > 0)
            {
                int folderPosition = folder.IndexOf("Assets/", System.StringComparison.InvariantCulture);
                if (folderPosition > 0)
                {
                    relativeFolder = folder.Substring(folderPosition);

                    // Create the animation.
                    SpriteAnimation asset = CreateInstance<SpriteAnimation>();
                    AssetDatabase.CreateAsset(asset, relativeFolder);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    selectedAnimation = AssetDatabase.LoadAssetAtPath<SpriteAnimation>(relativeFolder);
                    InitializeReorderableList();
                    justCreatedAnim = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Path", "Select a path inside the Assets folder", "OK");
                }
            }
        }

        /// <summary>
        /// Forces serialization of the current animation.
        /// </summary>
        /// <param name="toDisk">If true, it forces the asset database to save the file to disk.</param>
        private void SaveFile(bool toDisk = false)
        {
            selectedAnimation.Setup();
            EditorUtility.SetDirty(selectedAnimation);

            if (toDisk)
                AssetDatabase.SaveAssets();
        }

        #endregion Class Methods
    }
}