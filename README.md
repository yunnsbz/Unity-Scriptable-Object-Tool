# Unity ScriptableObject Editor

A custom Unity Editor tool designed to streamline the process of viewing and editing multiple ScriptableObjects (SOs) in a single window. This editor allows you to display ScriptableObjects of the same type side-by-side for quick comparison and modification, as well as list different types of ScriptableObjects vertically in a structured layout.

## Features and todos:
- [x] **Side-by-Side Editing**: View and edit multiple ScriptableObjects of the same type in a single window for faster workflows.
- [x] **Multi-Type Support**: Display different types of ScriptableObjects in a vertical list for easy access.
- [x] **Basic Filters**: Easily switch between viewing configurations and types.
- [x] **Intuitive Interface**: Built with Unity's Editor API for a seamless integration into your Unity projects.
- [x] **Adding and Removing ScriptableObjects**
- [x] **Ability to switch between vertical and horizontal table layout**
- [ ] Advanced Filtering Options
- [ ] Search options
- [ ] Data operations (copy, paste)
- [ ] Tool menu options
- [ ] Dynamic ScriptableObject Loading: <br/> A functionality to dynamically locate and load ScriptableObjects within the project, removing the dependency on a specific folder structure (e.g., Assets/Resources/ScriptableObjects/).

<br/> 
 
## Installation
1. Install the ``.unitypackage`` file from latest release.
2. Open Unity and go to ``Assets > import package > custom package`` menu and select the ``.unitypackage`` file.
3. Import the package.
4. For this version, your ScriptableObjects must be placed in the ``Assets/Resources/ScriptableObjects/`` folder (the program will automatically fetch all ScriptableObjects from this directory).
5. In Unity, go to the Window menu and find the new "Game Config Editor" window.

## Other Installation
1. Copy and paste the script located at ``Assets/Editor/Editor Windows/ScriptableObjectEditorWindow.cs`` into your Unity project's ``Assets/Editor`` folder.
2. If you want the icons to appear in the editor window, copy only the Icons folder from  ``Assets/Editor/Editor Windows/Icons`` into your project's ``Assets/Editor/Editor Windows`` directory.
Alternatively, you can clone this repository and move the Assets/Editor/Editor Windows folder as-is, but make sure to avoid overwriting existing scripts if you've customized them.
3. For this version, your ScriptableObjects must be placed in the ``Assets/Resources/ScriptableObjects/`` folder (the program will automatically fetch all ScriptableObjects from this directory).
4. In Unity, go to the Window menu and find the new "Game Config Editor" window.

<br/> 

## Usage
1. **Open the Editor Window**:
   - Navigate to the Unity menu and open the "Game Configuration Editor" window.
2. **Select Config Types**:
   - Click the filter button and choose the ScriptableObject types you want to see.
3. **Edit Side-by-Side**:
   - For the selected types, view multiple instances side-by-side.
   - Modify fields such as `int`, `string`, `vector3`, `gameObject`, `array`, `enum` etc., directly in the editor.
4. **Refresh and Filter**:
   - Use the "Refresh" button to reload the ScriptableObjects and the "Filter" option to narrow down your selection.

<br/> 

## Example
In the screenshot below, you can see two `TestSO` ScriptableObjects (`TestConfig` and `TestConfig1`) being edited side-by-side. Fields like `number`, `text`, `position`, and references to `GameObject`, `Transform`, or `Material` are displayed for quick modification.

![ScriptableObject Editor Screenshot](https://github.com/yunnsbz/Unity-Scriptable-Object-Tool/blob/main/preview.png)

<br/> 

## Contributing
Feel free to fork this repository and submit pull requests with improvements or bug fixes. If you encounter any issues, please open an issue on GitHub.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
