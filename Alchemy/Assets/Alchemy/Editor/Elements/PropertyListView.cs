using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Alchemy.Inspector;
using Object = UnityEngine.Object;

namespace Alchemy.Editor.Elements
{
    /// <summary>
    /// Visual Element that draws SerializedProperty of Array or List
    /// </summary>
    public sealed class PropertyListView : BindableElement
    {
        public PropertyListView(SerializedProperty property)
        {
            Assert.IsTrue(property.isArray);

            var parentObj = property.GetDeclaredObject();
            var events = property.GetAttribute<OnListViewChangedAttribute>(true);

            listView = GUIHelper.CreateListViewFromFieldInfo(parentObj, property.GetFieldInfo());
            listView.headerTitle = ObjectNames.NicifyVariableName(property.displayName);
            listView.bindItem = (element, index) =>
            {
                var arrayElement = property.GetArrayElementAtIndex(index);
                var e = new AlchemyPropertyField(arrayElement, property.GetPropertyType(true), true);
                var elementLabelTextSelector = property.GetAttribute<ListViewSettingsAttribute>()?.ElementLabelTextSelector;
                if (!string.IsNullOrEmpty(elementLabelTextSelector))
                {
                    e.Label = (string)ReflectionHelper.Invoke(parentObj, elementLabelTextSelector, index);
                }
                element.Add(e);
                element.Bind(arrayElement.serializedObject);
                e.TrackPropertyValue(arrayElement, x =>
                {
                    if (events != null)
                    {
                        ReflectionHelper.Invoke(parentObj, events.OnItemChanged,
                            new object[] { index, x.GetValue<object>() });
                    }
                    if (!string.IsNullOrEmpty(elementLabelTextSelector))
                    {
                        e.Label = (string)ReflectionHelper.Invoke(parentObj, elementLabelTextSelector, index);
                    }
                });
            };
            listView.unbindItem = (element, index) =>
            {
                element.Clear();
                element.Unbind();
            };
            listView.onAdd += view =>
            {
                var so = property.serializedObject;
                so.Update();

                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);

                var element = property.GetArrayElementAtIndex(index);
                var elementType = property.GetPropertyType(true);

                object instance = CreateDefaultInstance(elementType);

                switch (element.propertyType)
                {
                    case SerializedPropertyType.ManagedReference:
                        element.managedReferenceValue = instance;
                        break;

                    case SerializedPropertyType.Generic:
#if UNITY_2022_2_OR_NEWER
                        element.boxedValue = instance;
#else
            CopyObjectToGenericProperty(element, instance);
#endif
                        break;

                    case SerializedPropertyType.String:
                        element.stringValue = instance as string ?? string.Empty;
                        break;

                    case SerializedPropertyType.Integer:
                        if (instance is int intValue)
                            element.intValue = intValue;
                        break;

                    case SerializedPropertyType.Boolean:
                        if (instance is bool boolValue)
                            element.boolValue = boolValue;
                        break;

                    case SerializedPropertyType.Float:
                        if (instance is float floatValue)
                            element.floatValue = floatValue;
                        break;

                    case SerializedPropertyType.Enum:
                        if (instance != null)
                            element.enumValueIndex = (int)instance;
                        break;

                    case SerializedPropertyType.ObjectReference:
                        element.objectReferenceValue = instance as Object;
                        break;
                }

                so.ApplyModifiedProperties();
                view.RefreshItems();
            };

            var label = listView.Q<Label>();
            if (label != null) label.style.unityFontStyleAndWeight = FontStyle.Bold;

            listView.BindProperty(property);
            Add(listView);
        }

        readonly ListView listView;

        public static object CreateDefaultInstance(Type type)
        {
            if (type == null)
                return null;

            if (type == typeof(string))
                return string.Empty;

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return null;

            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private static void CopyObjectToGenericProperty(SerializedProperty property, object instance)
        {
            if (property == null || instance == null)
                return;

            var type = instance.GetType();
            var flags = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                string relativeName = iterator.name;
                var field = type.GetField(relativeName, flags);
                if (field == null)
                    continue;

                object value = field.GetValue(instance);

                switch (iterator.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if (value is int i) iterator.intValue = i;
                        break;
                    case SerializedPropertyType.Boolean:
                        if (value is bool b) iterator.boolValue = b;
                        break;
                    case SerializedPropertyType.Float:
                        if (value is float f) iterator.floatValue = f;
                        break;
                    case SerializedPropertyType.String:
                        iterator.stringValue = value as string ?? string.Empty;
                        break;
                    case SerializedPropertyType.Enum:
                        if (value != null) iterator.enumValueIndex = (int)value;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        iterator.objectReferenceValue = value as Object;
                        break;
                }
            }
        }

        public string Label
        {
            get => listView.headerTitle;
            set => listView.headerTitle = value;
        }
    }
}