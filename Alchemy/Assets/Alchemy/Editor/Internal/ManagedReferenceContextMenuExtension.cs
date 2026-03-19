#if UNITY_2021_3_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
//sadasd
namespace Alchemy.Editor
{
    public static class ManagedReferenceContextMenuExtension
    {
        private const string CLIPBOARD_KEY = "AlchemyExtensions.CopyAndPasteManagedProperty";
        private const string CLIPBOARD_TYPE_KEY = "AlchemyExtensions.CopyAndPasteManagedPropertyType";

        private static readonly GUIContent CopyProperty = new("Copy Property");
        private static readonly GUIContent PasteProperty = new("Paste Property");

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
        }

        private static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return;

            SerializedProperty clonedProperty = property.Copy();

            if (property.managedReferenceValue != null)
                menu.AddItem(CopyProperty, false, Copy, clonedProperty);
            else
                menu.AddDisabledItem(CopyProperty);

            string json = SessionState.GetString(CLIPBOARD_KEY, string.Empty);
            string typeName = SessionState.GetString(CLIPBOARD_TYPE_KEY, string.Empty);

            if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(typeName))
                menu.AddItem(PasteProperty, false, Paste, clonedProperty);
            else
                menu.AddDisabledItem(PasteProperty);

            menu.AddSeparator("");
        }

        private static void Copy(object serializedPropertyObject)
        {
            var property = (SerializedProperty)serializedPropertyObject;
            object value = property.managedReferenceValue;
            if (value == null)
                return;

            SessionState.SetString(CLIPBOARD_KEY, JsonUtility.ToJson(value));
            SessionState.SetString(CLIPBOARD_TYPE_KEY, value.GetType().AssemblyQualifiedName);
        }

        private static void Paste(object serializedPropertyObject)
        {
            var property = (SerializedProperty)serializedPropertyObject;

            string json = SessionState.GetString(CLIPBOARD_KEY, string.Empty);
            string typeName = SessionState.GetString(CLIPBOARD_TYPE_KEY, string.Empty);

            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(typeName))
                return;

            Type type = Type.GetType(typeName);
            if (type == null)
                return;

            var serializedObject = property.serializedObject;

            Undo.RecordObject(serializedObject.targetObject, PasteProperty.text);

            serializedObject.Update();

            object instance = property.managedReferenceValue;
            if (instance == null || instance.GetType() != type)
            {
                instance = Activator.CreateInstance(type);
                property.managedReferenceValue = instance;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                instance = property.managedReferenceValue;
            }

            JsonUtility.FromJsonOverwrite(json, instance);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            EditorUtility.SetDirty(serializedObject.targetObject);

            RebuildVisibleListViews();
        }

        private static void RebuildVisibleListViews()
        {
            EditorApplication.delayCall += () =>
            {
                var window = EditorWindow.focusedWindow;
                if (window?.rootVisualElement == null)
                {
                    ActiveEditorTracker.sharedTracker.ForceRebuild();
                    return;
                }

                bool rebuiltAny = false;

                foreach (var listView in window.rootVisualElement.Query<ListView>().ToList())
                {
                    listView.RefreshItems();
                    listView.Rebuild();
                    rebuiltAny = true;
                }

                if (!rebuiltAny)
                    ActiveEditorTracker.sharedTracker.ForceRebuild();
            };
        }
    }
}
#endif