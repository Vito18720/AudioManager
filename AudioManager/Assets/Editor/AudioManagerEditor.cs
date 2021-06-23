using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

//Own Namespaces
using AudioEngine;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine.Audio;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : UnityEditor.Editor
{
     AudioManager manager;

     private SerializedProperty s_mixers;
     private SerializedProperty s_tracks;
     
     private ReorderableList _reorderableTracks;
     
     private string dropdownLabelTracks;

     private List<string> mixerGroupPopup = new List<string>();
     private List<int> mixerIndex = new List<int>();

     private void OnEnable()
     {
         manager = target as AudioManager;

         //Properties
         s_tracks = serializedObject.FindProperty(nameof(manager.tracks));

         #region ReorderableListTracks

         _reorderableTracks = new ReorderableList(serializedObject, s_tracks, true, true, false, false);
         _reorderableTracks.drawHeaderCallback = DrawHeaderTracks;
         _reorderableTracks.drawElementCallback = DrawListTracks;
         _reorderableTracks.drawFooterCallback = DrawFooterTracks;
         _reorderableTracks.drawNoneElementCallback = DrawBackgroundNoTracks;

         _reorderableTracks.elementHeightCallback = delegate(int index) {
             var element = _reorderableTracks.serializedProperty.GetArrayElementAtIndex(index);
             var margin = EditorGUIUtility.standardVerticalSpacing;
             if (element.isExpanded) return 260 + margin;
             else return 20 + margin;
         };
         
         #endregion
         
     }

     public override void OnInspectorGUI()
     {
         //Vertical Space for the Audio Manager
         using (new EditorGUILayout.VerticalScope())
         {
             serializedObject.Update();
             
             //Horizontal Space for add and remove mixers
             using (new EditorGUILayout.HorizontalScope("Box"))
             {
                 if (GUILayout.Button("Add Mixer"))
                 {
                     manager.mixers.Add(new AudioManager.AudioTrackMixer());
                     mixerGroupPopup.Add("Default Mixer (You should put any name)");
                 }

                 GUILayout.Space(10f);

                 if (GUILayout.Button("Remove Mixer"))
                 {
                     manager.mixers.Remove(manager.mixers.ElementAt(manager.mixers.Count - 1));
                     mixerGroupPopup.RemoveAt(mixerGroupPopup.Count - 1);
                 }
             }

             var buttonStyle = new GUIStyle(GUI.skin.button);
             buttonStyle.normal.textColor = new Color(.5f, .5f, 1);
             //Draw Mixers
             //_reorderableMixers.DoLayoutList();
             var  blueStylePreset = new GUIStyle(GUI.skin.box);
             blueStylePreset.normal.textColor = new Color(.1f, .6f, .8f);
             var greenStylePreset = new GUIStyle(GUI.skin.button);
             greenStylePreset.normal.textColor = new Color(.05f, .9f, .2f);
             var redStylePreset = new GUIStyle(GUI.skin.button);
             greenStylePreset.normal.textColor = new Color(1f, .2f, .2f);
             
             for (int i = 0; i < manager.mixers.Count; i++)
             {
                 using (new EditorGUILayout.HorizontalScope("HelpBox"))
                 {
                     using (new EditorGUILayout.VerticalScope())
                     {
                         manager.mixers[i].dropdownMixer = EditorGUILayout.Foldout(manager.mixers[i].dropdownMixer, string.Empty);
                         EditorGUILayout.LabelField($"Mixer Group: {manager.mixers[i].name}", blueStylePreset);

                         mixerGroupPopup[i] = manager.mixers[i].name;

                         if (manager.mixers[i].dropdownMixer)
                         {
                             manager.mixers[i].name = EditorGUILayout.TextField(manager.mixers[i].name);

                             manager.mixers[i].mixerGroup = (AudioMixerGroup) EditorGUILayout.ObjectField(manager.mixers[i].mixerGroup, typeof(AudioMixerGroup), true);
                         }
                         
                         if (GUILayout.Button("X", greenStylePreset))
                         {
                             manager.mixers.RemoveAt(i);
                             mixerGroupPopup.RemoveAt(i);
                         }
                     }
                 }
             }
             
             EditorGUILayout.Space();
             
             //Horizontal Space for add and remove tracks
             using (new EditorGUILayout.HorizontalScope())
             {
                 if (GUILayout.Button("Add Track"))
                 {
                     manager.tracks.Add(new AudioManager.AudioTrack());
                     mixerIndex.Add(0);
                 }

                 GUILayout.Space(10f);

                 if (GUILayout.Button("Remove Track"))
                 {
                     manager.tracks.Remove(manager.tracks.ElementAt(manager.tracks.Count - 1));
                     mixerIndex.RemoveAt(mixerIndex.Count - 1);
                 }
             }

             _reorderableTracks.DoLayoutList();

             serializedObject.ApplyModifiedProperties();
         }
     }

     //Property Drawer for the tracks class
     public void DrawListTracks(Rect position, int index, bool isActive, bool isFocused)
    {
        SerializedProperty property = _reorderableTracks.serializedProperty.GetArrayElementAtIndex(index);

        position.width -= 34;
        position.height = 18;
        
        Rect dropdownRect = new Rect(position);
        dropdownRect.width = 10;
        dropdownRect.height = 10;
        dropdownRect.x += 10;
        dropdownRect.y += 5;
        
        property.isExpanded = EditorGUI.Foldout(dropdownRect, property.isExpanded, dropdownLabelTracks);
        
        position.x += 50;
        position.width -= 15;
        
        Rect fieldRect = new Rect(position);
        
        SerializedProperty clipField = property.FindPropertyRelative("clip");
        
        SerializedProperty nameField = property.FindPropertyRelative(nameof(AudioManager.AudioTrack.name));
        
        if (clipField.objectReferenceValue != null)
        {
            nameField.stringValue = ((AudioClip) clipField.objectReferenceValue).name;
        }
        
        if (property.isExpanded)
        {
            Space(ref fieldRect, 20f);
            var mixerField = property.FindPropertyRelative("mixer");
            
            EditorGUI.LabelField(fieldRect, "Mixer Group: ");

            var x = fieldRect.x;
            fieldRect.x += (EditorGUIUtility.currentViewWidth - 495);

            Rect popupRect = new Rect(fieldRect.position, new Vector2(300, 10));

            mixerIndex[index] = EditorGUI.Popup(popupRect, mixerIndex[index], mixerGroupPopup.ToArray());

            fieldRect.x = x;
            Space(ref fieldRect);
            //Draw Clip
            EditorGUI.PropertyField(fieldRect, clipField);
            
            Space(ref fieldRect);
            //Draw Name
            EditorGUI.TextField(fieldRect, "Name" , nameField.stringValue);

            var loopField = property.FindPropertyRelative("loop");
            
            SerializedProperty priorityField = property.FindPropertyRelative("priority");
            SerializedProperty volumeField = property.FindPropertyRelative("volume");
            SerializedProperty pitchField = property.FindPropertyRelative("pitch");
            SerializedProperty SpatialBlendField = property.FindPropertyRelative("spatialBlend");

            var customLabel = new Label("LABEL");
            //Draw Values
            Space(ref fieldRect);
            EditorGUI.Slider(fieldRect, priorityField, 0f, 256f);
            Space(ref fieldRect);
            EditorGUI.Slider(fieldRect, volumeField, 0f, 1);
            Space(ref fieldRect);
            EditorGUI.Slider(fieldRect, pitchField, -3f, 3);
            Space(ref fieldRect);
            EditorGUI.Slider(fieldRect, SpatialBlendField, 0f, 1f);
            Space(ref fieldRect);

            Rect buttonRect = new Rect(fieldRect.position, new Vector2(50, fieldRect.height));
            buttonRect.x += (EditorGUIUtility.currentViewWidth * 0.5f)-buttonRect.x;
            if (GUI.Button(buttonRect, "X"))
            {
                manager.tracks.Remove(manager.tracks.ElementAt(index));
                mixerIndex.RemoveAt(index);
            }
            Space(ref fieldRect, 15);
            DrawUILine(fieldRect.x, fieldRect.y);
            Space(ref fieldRect);
        }
        else
        {
            Rect buttonRect = new Rect(dropdownRect.position, new Vector2(50, 20));
            buttonRect.y -= 5;
            buttonRect.x += (EditorGUIUtility.currentViewWidth - (buttonRect.x * 3));
            if (GUI.Button(buttonRect, "X"))
            {
                manager.tracks.Remove(manager.tracks.ElementAt(index));
                mixerIndex.RemoveAt(index);
            }
        }
        
        GetDropdownLabelTracks(index);
    }

     void GetDropdownLabelTracks(int index)
    {
        int i = index;

        i++;

        if (i > _reorderableTracks.count - 1)
        {
            i = 0;
        }
        
        SerializedProperty property = _reorderableTracks.serializedProperty.GetArrayElementAtIndex(i);

        if (property.isExpanded)
        {
            dropdownLabelTracks = string.Empty;
        }
        else
        {
            var clipT = property.FindPropertyRelative(nameof(AudioManager.AudioTrack.clip));
            string clipName = string.Empty;
            if (clipT.objectReferenceValue != null)
            {
                clipName = ((AudioClip) clipT.objectReferenceValue).name;
            }
            else
            {
                clipName = string.Empty;
            }
            dropdownLabelTracks = clipName != string.Empty ? $"Track: {clipName}" : "Default Track";
        }
    }

     void DrawHeaderTracks(Rect rect)
    {
        var  blueStylePreset = new GUIStyle(GUI.skin.label);
        blueStylePreset.normal.textColor = new Color(.1f, .6f, .8f);
        string name = "Audio Manager Tracks";
        EditorGUI.LabelField(rect, name, blueStylePreset);
    }
    
     void DrawFooterTracks(Rect rect)
    {
        var  blueStylePreset = new GUIStyle(GUI.skin.label);
        blueStylePreset.normal.textColor = new Color(.1f, .6f, .8f);
        string name = "By @babelgames_es";
        EditorGUI.LabelField(rect, name, blueStylePreset);
    }
    
     void DrawBackgroundNoTracks(Rect rect)
    {
        var greenStylePreset = new GUIStyle(GUI.skin.label);
        greenStylePreset.normal.textColor = new Color(.05f, .9f, .2f);
        string name = "Add tracks for setting the audio in your game";
        EditorGUI.LabelField(rect, name, greenStylePreset);
    }
    
     public void Space(ref Rect pos, float space = 30f)
    {
        pos.y += space;
    }
    
     public static void DrawUILine(float posX, float posY, float thickness = 38, float padding = 30)
    {
        Rect r = new Rect(posX, posY, thickness, padding);
        r.width = EditorGUIUtility.currentViewWidth;
        r.height = 2;
        r.y+=padding * 0.3f;
        r.x-=70;
        r.width -= thickness;
        EditorGUI.DrawRect(r, Color.cyan);
    }
}