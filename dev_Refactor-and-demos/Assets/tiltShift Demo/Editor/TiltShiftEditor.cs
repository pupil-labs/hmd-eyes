namespace PostFXEditor
{
	using UnityEditor;

	[CustomEditor(typeof(PostFX.TiltShift))]
	public class TiltShiftEditor : Editor
	{
		SerializedProperty m_Preview;
		SerializedProperty m_Offset;
		SerializedProperty m_Area;
		SerializedProperty m_Spread;
		SerializedProperty m_Samples;
		SerializedProperty m_Radius;
		SerializedProperty m_UseDistortion;
		SerializedProperty m_CubicDistortion;
		SerializedProperty m_DistortionScale;
		SerializedProperty m_Shader;

		void OnEnable()
		{
			m_Shader = serializedObject.FindProperty("Shader");
			m_Preview = serializedObject.FindProperty("Preview");
			m_Offset = serializedObject.FindProperty("Offset");
			m_Area = serializedObject.FindProperty("Area");
			m_Spread = serializedObject.FindProperty("Spread");
			m_Samples = serializedObject.FindProperty("Samples");
			m_Radius = serializedObject.FindProperty("Radius");
			m_UseDistortion = serializedObject.FindProperty("UseDistortion");
			m_CubicDistortion = serializedObject.FindProperty("CubicDistortion");
			m_DistortionScale = serializedObject.FindProperty("DistortionScale");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_Shader);
			EditorGUILayout.PropertyField(m_Preview);
			EditorGUILayout.PropertyField(m_Offset);
			EditorGUILayout.PropertyField(m_Area);
			EditorGUILayout.PropertyField(m_Spread);
			EditorGUILayout.PropertyField(m_Samples);
			EditorGUILayout.PropertyField(m_Radius);
			EditorGUILayout.PropertyField(m_UseDistortion);

			if (m_UseDistortion.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(m_CubicDistortion);
				EditorGUILayout.PropertyField(m_DistortionScale);
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();

			base.DrawDefaultInspector ();

		}
	}
}
