using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class ImageAlphaEditor : EditorWindow
{
    private UnityEditor.UIElements.ObjectField texture_field;
    private DropdownField alpha_dropdown;
    private GradientField alpha_gradient;
    private VisualElement image_preview;
    private SliderInt alpha_slider;
    private Button export_button;
    private string output_name;
    private Texture2D selected_texture;
    private Texture2D output_texture;
    private VisualElement custom_texture_values;
    private DropdownField texture_option;
    private IntegerField width_field;
    private IntegerField height_field;
    private Button create_texture_button;
    private ColorField tint;
    private ComputeShader shader;
    private IntegerField alpha_input;

    //public Button CreateTexture { get; private set; }

    [MenuItem("Tools/Image Editor")]
    public static void OpenEditorWindow()
    {
        ImageAlphaEditor window = GetWindow <ImageAlphaEditor>();
        window.titleContent = new GUIContent("Image Editor");
        window.maxSize = new Vector2(320, 500);
        window.minSize = window.maxSize;
    }

    private void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        var visual_tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ImageAlphaTest/Resources/UI Documents/ImageAlphaEditorWindow.uxml");

        VisualElement tree = visual_tree.Instantiate();
        root.Add(tree);

        //Assign elements
        texture_field = root.Q<UnityEditor.UIElements.ObjectField>("texture-field");
        alpha_dropdown = root.Q<DropdownField>("alpha-dropdown");
        texture_option = root.Q<DropdownField>("texture-option");
        alpha_gradient = root.Q<GradientField>(); //only 1 so doesnt need to be specified
        image_preview = root.Q<VisualElement>("image-preview");
        custom_texture_values = root.Q<VisualElement>("custom-tex-values");
        alpha_slider = root.Q<SliderInt>(); //only 1 so doesnt need to be specified
        export_button = root.Q<Button>("export-button");
        create_texture_button = root.Q<Button>("create-tex-button");
        width_field = root.Q<IntegerField>("width-field");
        height_field = root.Q<IntegerField>("height-field");
        tint = root.Q<ColorField>("tint");
        alpha_input = root.Q<IntegerField>("alpha-input");

        //assign callbacks
        texture_field.RegisterValueChangedCallback<Object>(TextureSelected);
        alpha_dropdown.RegisterValueChangedCallback<string>(AlphaOptionSelected);
        texture_option.RegisterValueChangedCallback<string>(TextureOptionSelected);
        alpha_slider.RegisterValueChangedCallback<int>(AlphaSliderChanged);
        alpha_input.RegisterValueChangedCallback<int>(AlphaInputChanged);
        alpha_gradient.RegisterValueChangedCallback<Gradient>(AlphaGradientChanged);
        tint.RegisterValueChangedCallback<Color>(TintChanged);
        export_button.clicked += () => ExportImage(output_texture);
        create_texture_button.clicked += CreateTexture;

        image_preview.style.backgroundImage = null;
        TextureOptionSelected(null);
        AlphaOptionSelected(null);

    }

    private void CreateTexture()
    {
        int texture_width = width_field.value;
        int texture_height = height_field.value;
        selected_texture = new Texture2D(texture_width, texture_height, TextureFormat.RGBA32, false);
        //goes through each pixel in texture, makes color white
        for (int i = 0; i < texture_width; i++)
        {
            for (int y = 0; y < texture_height; y++)
            {
                selected_texture.SetPixel(i, y, Color.white);
            }
        }
        selected_texture.Apply();
        output_name = "CustomTexture";
        SetPreviewDimensions(texture_width, texture_height);
        ApplyAlphaGradient();
    }

    private void SetPreviewDimensions(int texture_width, int texture_height)
    {
        bool greater_width = (texture_width > texture_height);
        float x_ratio = 1;
        float y_ratio = 1;
        if (greater_width)
        {
            y_ratio = (float)texture_height / (float)texture_width;
        }
        else
        {
            x_ratio = (float)texture_width / (float)texture_height;
        }
        image_preview.style.width = 300 * x_ratio;
        image_preview.style.height = 300 * y_ratio;
    }

    private void ExportImage(Texture2D output_texture)
    {
        var path = EditorUtility.SaveFilePanel(
            "Save Edited Texture",
            Application.dataPath,
            output_name + ".png",
            "png");
        byte[] bytes = output_texture.EncodeToPNG();
        if(string.IsNullOrEmpty(path))
        {
            return;
        }
        File.WriteAllBytes(path, bytes);

        string path_string = path;
        int asset_index = path_string.IndexOf("Assets", System.StringComparison.Ordinal);
        string file_path = path_string.Substring(asset_index, path.Length - asset_index);
        AssetDatabase.ImportAsset(file_path);
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = (Texture2D)AssetDatabase.LoadAssetAtPath(file_path, typeof(Texture2D));
        Debug.Log("Filepath is: " + file_path);
    }

    private void ApplyAlphaGradient()
    {
        if(selected_texture == null)
        {
            export_button.SetEnabled(false);
            return;
        }
        export_button.SetEnabled(true);

        output_texture = selected_texture;
        image_preview.style.backgroundImage = output_texture;
    }

    private void TintChanged(ChangeEvent<Color> evt)
    {
        ApplyAlphaGradient();
    }

    private void AlphaGradientChanged(ChangeEvent<Gradient> evt)
    {
        ApplyAlphaGradient();
    }

    private void AlphaInputChanged(ChangeEvent<int> evt)
    {
        alpha_slider.SetValueWithoutNotify(evt.newValue);
        ApplyAlphaGradient();
    }

    private void AlphaSliderChanged(ChangeEvent<int> evt)
    {
        alpha_input.SetValueWithoutNotify(evt.newValue);
        ApplyAlphaGradient();
    }

    private void TextureOptionSelected(ChangeEvent<string> evt)
    {
        if(texture_option.value != texture_option.choices[0])
        {
            texture_field.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            custom_texture_values.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
        else
        {
            texture_field.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            custom_texture_values.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }
        selected_texture = null;
        texture_field.value = null;
        image_preview.style.backgroundImage = null;
        ApplyAlphaGradient();
    }

    private void AlphaOptionSelected(ChangeEvent<string> evt)
    {
        if(alpha_dropdown.value != alpha_dropdown.choices[0])
        {
            alpha_slider.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            alpha_gradient.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }
        else
        {
            alpha_slider.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            alpha_gradient.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
        ApplyAlphaGradient();
    }

    private void TextureSelected(ChangeEvent<Object> evt)
    {
        if(evt.newValue == null)
        {
            selected_texture = null;
            image_preview.style.backgroundImage = null;
            return;
        }
        output_name = evt.newValue.name + "Changed";
        selected_texture = evt.newValue as Texture2D;
        SetPreviewDimensions(selected_texture.width, selected_texture.height);
        ApplyAlphaGradient();
    }
}
