using UnityEditor;
using UnityEngine;

namespace UIModule.Editor
{
    /// <summary>
    /// UIModuleSettings의 커스텀 에디터
    /// 원본 경로(Assets 경로)를 읽기 전용으로 표시
    /// </summary>
    [CustomEditor(typeof(UIModuleSettings))]
    public class UIModuleSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _prefabPathPrefixProp;
        private SerializedProperty _assetsFolderPathProp;
        
        private void OnEnable()
        {
            _prefabPathPrefixProp = serializedObject.FindProperty("_prefabPathPrefix");
            _assetsFolderPathProp = serializedObject.FindProperty("_assetsFolderPath");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("UI Module Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 프리팹 경로 (읽기 전용)
            EditorGUILayout.LabelField("프리팹 설정", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_prefabPathPrefixProp, new GUIContent("Resources 경로", "Resources 폴더 기준 프리팹 경로 (예: UIPrefabs/)"));
            EditorGUILayout.PropertyField(_assetsFolderPathProp, new GUIContent("Assets 경로", "Assets 기준 전체 경로 (에디터 전용)"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // 안내 메시지
            EditorGUILayout.HelpBox(
                "이 설정은 UI Dashboard에서 기준 폴더를 변경하면 자동으로 업데이트됩니다.\n" +
                "직접 수정하지 마세요.",
                MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // UI Dashboard 열기 버튼
            if (GUILayout.Button("UI Dashboard 열기", GUILayout.Height(25)))
            {
                EditorWindow.GetWindow<CustomUIDashboard>("UI Dashboard");
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}





