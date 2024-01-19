using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.AI;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
//using static UnityEditor.Rendering.FilterWindow;
//using static UnityEngine.GraphicsBuffer;

public class DebugTools : MonoBehaviour
{
    public bool console_active;
    TextField console_input;
    VisualElement console;
    float activate_limit;
    bool limit;

    //console suggestion test
    VisualElement suggestion_visual;

    //performance stats
    public bool perf_stats_enable = false;
    VisualElement perf_stats;
    Label label_1;
    Label label_2;
    Label label_3;
    Label label_4;
    Label label_5;
    float polling_time = 1.0f;
    float time;
    int frame_count;
    int frame_rate;
    int processor_count;
    //cpu usage calc
    float cpu_usage;
    Thread cpu_thread;
    float last_cpu_usage;
    VisualElement chart;
    List<int> frame_list;
    float graph_delay = 0.1f;
    float graph_timer;


    public GameObject player;
    public PlayerMovement player_movement;

    public List<object> command_list;
    public static DebugCommand<float> set_speed;
    public static DebugCommand perf_panel_enable;
    public static DebugCommand wireframe_mode;
    public static DebugCommand entity_inspector_command;
    public static DebugCommand placeholder_command;
    //public static DebugCommand 

    Color blue = new Color(0f, 0, 1f, 1f);
    Color green = new Color(0f, 1f, 0f, 1f);
    Color yellow = new Color(1f, 0.92f, 0.016f, 1f);
    Color red = new Color(1f, 0f, 0f, 1f);

    //model/entity inspector
    bool entity_inspector_enable = false;
    [SerializeField] private UIDocument inspector_document;
    [SerializeField] private StyleSheet entity_inspector_style;
    //List<GameObject> game_objects_list = new List<GameObject>();
    VisualElement entity_inspector;
    VisualElement container;
    VisualElement view_box;
    Label vertex_count;
    VisualElement control_box;  
    DropdownField selector;
    Slider rotate_x_slider;
    Slider rotate_y_slider;
    [SerializeField] Camera view_cam;
    RenderTexture model_view_texture;
    Image view_box_image;
    public GameObject targeted_entity;
    public MeshFilter target_mesh_filter;
    public Mesh target_mesh;
    bool mouse_in;
    //public Transform player_transform;
    //public static event Action<float> RotationChanged;

    private void Awake()
    {
        player_movement = player.GetComponent<PlayerMovement>();
        set_speed = new DebugCommand<float>("set_speed", "Sets the player move speed", "set_speed <speed_value>", (x) =>
        {
            player_movement.SetMoveSpeed(x);
            UnityEngine.Debug.Log("Player Move Speed: " + x);
        });

        perf_panel_enable = new DebugCommand("perf_panel_enable", "Enables/disables the performance panel", "perf_panel_enable", () =>
        {
            perf_stats_enable = !perf_stats_enable;
        });

        wireframe_mode = new DebugCommand("wireframe_mode", "Enables/disables wireframe rendering", "wireframe_enable", () =>
        {
            //this does nothing, needs to be linked to camera
            //GL.wireframe = !GL.wireframe;
        });

        entity_inspector_command = new DebugCommand("entity_inspector", "Enables/disables the entity inspector", "entity_inspector", () =>
        {
            entity_inspector_enable = !entity_inspector_enable;
        });

        placeholder_command = new DebugCommand("placeholder_command", "Honed is the blade that severs the villain's head. Endless is the path that leads him from hell."
            , "placeholder_command", ()=>
        {
            UnityEngine.Debug.Log("They who dwell aloft have spoken. Let their words echo in your empty soul. Ruination is come!");
        });

        command_list = new List<object>
        {
            set_speed,
            perf_panel_enable,
            wireframe_mode,
            entity_inspector_command,
            placeholder_command
        };
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        cpu_thread = new Thread(UpdateCPUUsage);
        //physical cores only
        processor_count = SystemInfo.processorCount / 2;

        cpu_thread.Start();
    }
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        console_active = false;
        console = root.Q<VisualElement>("Console");
        console_input = root.Q<TextField>("ConsoleInput");
        console.SetEnabled(false);
        console_input.style.display = DisplayStyle.None;
        //console_input.focusable = false;

        limit = false;
        activate_limit = 0f;

        perf_stats = root.Q("PerfStats");
        perf_stats.style.display = DisplayStyle.None;
        label_1 = perf_stats.Q<Label>("fps_count");
        label_2 = perf_stats.Q<Label>("gpu_name");
        label_3 = perf_stats.Q<Label>("cpu_name");
        label_4 = perf_stats.Q<Label>("vram");
        label_5 = perf_stats.Q<Label>("cpu_usage");
        //this will not be in referenced in the ui builder
        chart = new VisualElement();
        perf_stats.Add(chart);
        frame_list = new List<int>();

        //entity_inspector.AddManipulator(new DragManipulator());

        EntityInspectorSetup();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(console_active);
        ConsoleHandling();
        ConsoleEnable();
        SuggestionBox();
        PerfomancePanel();
        EntityInspector();
        
        if(console_active || entity_inspector_enable)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void ConsoleEnable()
    {
        if (console_active)
        {
            console.SetEnabled(true);
            console_input.style.display = DisplayStyle.Flex;
            console_input.SetEnabled(true);
            console_input.focusable = true;
        }
        else
        {
            //console.SetEnabled(false);
            console_input.style.display = DisplayStyle.None;
            console_input.SetEnabled(false);
            console_input.focusable = false;
        }
    }

    void ConsoleHandling()
    {
        //Debug.Log(activate_limit);
        if (limit)
        {
            activate_limit -= Time.deltaTime;
            if (activate_limit == 0.0f)
            {
                limit = false;
            }
        }

        if (Input.GetKey(KeyCode.Slash))
        {
            if (activate_limit <= 0)
            {
                console_active = !console_active;
                activate_limit = 0.5f;
                limit = true;
            }
        }

        if(console_active)
        {
            if (Input.GetKey(KeyCode.Return))
            {
                HandleInput();
                console_input.value = "";
            }
        }
    }

    public bool GetConsoleState()
    {
        return console_active;
    }

    private void HandleInput()
    {
        string[] properties = console_input.text.Split(' ');

        for(int i = 0; i < command_list.Count; i++)
        {
            DebugCommandBase command_base = command_list[i] as DebugCommandBase;
            if (console_input.text.Contains(command_base.CommandID))
            {
                if (command_list[i] as DebugCommand != null)
                {
                    (command_list[i] as DebugCommand).Invoke();
                }
                else if (command_list[i] as DebugCommand<float> != null)
                {
                    (command_list[i] as DebugCommand<float>).Invoke(float.Parse(properties[1]));
                }
            }
        }
    }

    //command suggestions for console user types
    void SuggestionBox()
    {
        if (console_active)
        {
            if (suggestion_visual == null)
            {
                suggestion_visual = new VisualElement
                {
                    style =
                {
                    backgroundColor = new Color(1.0f, 1.0f, 1.0f),
                    borderTopColor = Color.black,
                    borderBottomColor = Color.black,
                    borderLeftColor = Color.black,
                    borderRightColor = Color.black,
                    borderLeftWidth = 1,
                    borderBottomWidth = 1,
                    borderRightWidth = 1,
                    borderTopWidth = 1,
                    position = Position.Relative,
                }
                };
                console.Add(suggestion_visual);
                //suggestion_visual.layout.position = console_input.layout.position;
            }

            suggestion_visual.style.display = DisplayStyle.Flex;

            suggestion_visual.Clear();
            for (int i = 0; i < command_list.Count; i++)
            {
                DebugCommandBase command_base = command_list[i] as DebugCommandBase;
                var text = command_base.CommandID;
                var text_2 = ": " + command_base.CommandDescription;
                var label = new Label();
                if(text.Contains(console_input.text))
                {
                    // var text = System.Guid.NewGuid().ToString();
                    label = new Label(text + text_2);
                    label.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        console_input.SetValueWithoutNotify(text);
                        suggestion_visual.RemoveFromHierarchy();
                        suggestion_visual = null;
                    });
                    label.RegisterCallback<MouseOverEvent>((type) =>
                    {
                        label.style.color = Color.white;
                        label.style.backgroundColor = Color.black;
                    });
                    suggestion_visual.Add(label);
                }
            }
            //suggestion_visual.style.top = console_input.layout.yMax;
        }
        else
        {
            if (suggestion_visual != null)
            {
                suggestion_visual.style.display = DisplayStyle.None;
            }
        }
    }


    void PerfomancePanel()
    {
        if (perf_stats_enable == true)
        {
            //Debug.Log("Performance Panel True");
            perf_stats.style.display = DisplayStyle.Flex;

            label_1.text = "FPS: " + GetFPS(); 
            if(GetFPS() > 144)
            {
                label_1.style.color = blue;
            }
            if(GetFPS() >= 60 && GetFPS() < 144)
            {
                label_1.style.color = green;
            }
            if (GetFPS() >= 30 && GetFPS() < 60)
            {
                label_1.style.color = yellow;
            }
            if (GetFPS() > 0 && GetFPS() < 30)
            {
                label_1.style.color = red;
            }
            label_2.text = "GPU: " + SystemInfo.graphicsDeviceName;
            label_3.text = "CPU: " + SystemInfo.processorType;
            label_4.text = "Total VRAM: " + SystemInfo.graphicsMemorySize + "MB";
            // label_5.text = "CPU Frequency: " + SystemInfo.processorFrequency + "MHz";
            if (Mathf.Approximately(last_cpu_usage, cpu_usage)) return;

            if (cpu_usage < 0 || cpu_usage > 100) return;
            label_5.text = "CPU Usage: " + Mathf.RoundToInt(cpu_usage) + "%";
            last_cpu_usage = cpu_usage;

            StartCoroutine(FPSListUpdate());
            StartCoroutine(BeginDrawingChart());
            
        }
        else
        {
            perf_stats.style.display = DisplayStyle.None;
        }
    }

    void DrawChart(MeshGenerationContext ctx)
    {
        graph_timer += Time.deltaTime;
        var painter = ctx.painter2D;
        /*painter.fillColor = Color.red;
        painter.strokeColor = Color.black;
        painter.lineWidth = 10;
        painter.lineJoin = LineJoin.Round;*/

        /*painter.BeginPath();
        painter.MoveTo(new Vector2(100, 100));
        painter.LineTo(new Vector2(150, 150));
        painter.LineTo(new Vector2(200, 50));
        painter.ArcTo(new Vector2(300, 100), new Vector2(300, 200), 100.0f);
        painter.LineTo(new Vector2(150, 250));
        painter.ClosePath();
        painter.Fill();
        painter.Stroke();*/

        /*var chart = new float[] {
            0.5f, 0.7f, 0.67f, 0.9f, 0.81f, 0.84f, 0.67f, 0.53f, 0.21f, 0.34f
        };

        var chartPos = new Vector2(0, 150);
        var chartWidth = 300.0f;
        var chartHeight = 150.0f;

        painter.fillColor = Color.green;
        painter.BeginPath();
        painter.MoveTo(chartPos);
        float dx = chartWidth / chart.Length;
        float x = 0;
        foreach (var v in chart)
        {
            painter.LineTo(chartPos + new Vector2(x, -v * chartHeight));
            x += dx;
        }
        painter.LineTo(new Vector2(chartWidth, chartPos.y));
        painter.ClosePath();
        painter.Fill();*/

        painter.lineWidth = 2;
        painter.lineJoin = LineJoin.Miter;
        painter.strokeColor = Color.green;
        var graph_pos = new Vector2(0, 250);
        var graph_width = 300.0f;
        var graph_height = 150.0f;

        if(graph_timer >= graph_delay)
        {

            graph_timer -= graph_delay;
        }

        painter.fillColor = Color.green;
        painter.BeginPath();
        painter.MoveTo(graph_pos);
        float dx = graph_width / 10;
        float x = 0;
        foreach (var f in frame_list)
        {
            painter.LineTo(graph_pos + new Vector2(x, (-f * graph_height) / 500));
            x += dx * 1.2f;
            //UnityEngine.Debug.Log(f + " FPS");
        }
        //painter.LineTo(new Vector2(graph_width, graph_pos.y));
        //painter.ClosePath();
        //painter.Fill();
        UnityEngine.Debug.Log(frame_list.Count());
        painter.Stroke();
    }

    IEnumerator BeginDrawingChart()
    {
        chart.generateVisualContent += DrawChart;
        chart.MarkDirtyRepaint();
        //yield return new WaitForSeconds(0.2f);
        yield return null;
    }

    IEnumerator FPSListUpdate()
    {
        frame_list.Add(GetFPS());
        if(frame_list.Count > 10)
        {
            //UnityEngine.Debug.Log("FPS record list over 100, wiping!");
            frame_list.Clear();
        }
        //UnityEngine.Debug.Log(frame_list.Count());
        //yield return new WaitForSeconds(0.2f);
        yield return null;
    }

    private void EntityInspectorSetup()
    {
        //yield return null;

        //entity inspector
        entity_inspector = inspector_document.rootVisualElement;
        entity_inspector.styleSheets.Add(entity_inspector_style);
        entity_inspector.style.position = Position.Absolute;
        //entity_inspector.style.top = //960f; //Screen.currentResolution.height / 2;
        entity_inspector.style.left = Screen.currentResolution.width / 3f;

        //FUCKED!
        //entity_inspector.AddManipulator(new DragManipulator());
        //entity_inspector.RegisterCallback<DropEvent>(evt =>
        //UnityEngine.Debug.Log($"{evt.target} dropped on {evt.droppable}"));

        container = new VisualElement();
        container.AddToClassList("Container");
        container.AddToClassList("BorderedBox");

        view_box = new VisualElement();
        view_box.AddToClassList("ViewBox");
        view_box.AddToClassList("BorderedBox");
        //view_box.Add(target_mesh);

        vertex_count = new Label();
        view_box.Add(vertex_count);

        //render texture setup
        view_cam = gameObject.AddComponent(typeof(Camera)) as Camera;
        if(view_cam != null)
        {
            UnityEngine.Debug.Log("Camera exists");
        }
        else
        {
            UnityEngine.Debug.Log("Camera fucked");
        }
        view_cam.depth = Camera.main.depth + 1;
        view_cam.clearFlags = CameraClearFlags.SolidColor;
        //view_cam.enabled = true;
        //view_cam.Render();
        model_view_texture = new RenderTexture(300, 300, 16, RenderTextureFormat.ARGB32);
        model_view_texture.Create();
        UnityEngine.Debug.Log("Is render texture created?" + model_view_texture.Create());
        view_box_image = new Image();
        view_box_image.image = model_view_texture;
        UnityEngine.Debug.Log("Render texture assigned to view image");
        //setting target texture to model_view_texture render texture causes null reference exception?!
        if (model_view_texture != null)
        {
            if(view_cam != null)
            {
                view_cam.targetTexture = model_view_texture;
                UnityEngine.Debug.Log("Render texture assigned to view_cam targetTexture");
            }
            else
            {
                UnityEngine.Debug.Log("CAMERA DOES NOT EXIST!");
            }
        }
        else
        {
            UnityEngine.Debug.Log("CANNOT SET TARGET TEXTURE TO RENDER TEXTURE!");
        }

        view_box.Add(view_box_image);

        container.Add(view_box);

        control_box = new VisualElement();
        control_box.AddToClassList("ControlBox");
        control_box.AddToClassList("BorderedBox");
        selector = new DropdownField();
        GameObject[] all_objects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        foreach (GameObject i in all_objects)
        {
            if (i.name != "targeted_entity")
            {
                if (i.GetComponent<MeshRenderer>() != null)
                {
                    selector.choices.Add(i.name);
                }
            }
        }

        control_box.Add(selector);

        var rotation_x_label = new Label();
        rotation_x_label.text = "X Rotation";
        control_box.Add(rotation_x_label);

        rotate_x_slider = new Slider();
        rotate_x_slider.lowValue = 0;
        rotate_x_slider.highValue = 360;
        rotate_x_slider.value = 0;
        //rotate_x_slider.RegisterValueChangedCallback(v => RotationChanged?.Invoke(v.newValue));
        control_box.Add(rotate_x_slider);

        var rotation_y_label = new Label();
        rotation_y_label.text = "Y Rotation";
        control_box.Add(rotation_y_label);

        rotate_y_slider = new Slider();
        rotate_y_slider.lowValue = 0;
        rotate_y_slider.highValue = 360;
        rotate_y_slider.value = 0;
        control_box.Add(rotate_y_slider);


        container.Add(control_box);

        entity_inspector.Add(container);

        entity_inspector.style.display = DisplayStyle.None;

        targeted_entity = new GameObject();
        targeted_entity.AddComponent<MeshFilter>();
        targeted_entity.AddComponent<MeshRenderer>();
    }

    //https://forum.unity.com/threads/how-can-i-move-a-visualelement-to-the-position-of-the-mouse.1187890/
    //from magnetic_scho. TIL cant just use screen space coords, ui elements have their own coords.
    private Vector2 ScreenToPanel(Vector3 mousePosition)
    {
        return UnityEngine.UIElements.RuntimePanelUtils.ScreenToPanel(entity_inspector.panel,
            new Vector2(mousePosition.x, Screen.height - mousePosition.y));
    }

    private void MoveVisualElement(VisualElement element, Vector2 mouse_position)
    {
        var ui_mouse_pos = ScreenToPanel(mouse_position);
        element.style.top = ui_mouse_pos.y;
        element.style.left = ui_mouse_pos.x;
        UnityEngine.Debug.Log("Element being moved");
    }

    private void EntityInspector()
    {
        if(entity_inspector_enable)
        {
            entity_inspector.style.display = DisplayStyle.Flex;
            //container.RegisterCallback<MouseDownEvent> (x => MoveVisualElement(entity_inspector, Input.mousePosition));
            container.RegisterCallback<MouseEnterEvent> (x => mouse_in = true );
            container.RegisterCallback<MouseLeaveEvent>(x => mouse_in = false);
            control_box.RegisterCallback<MouseEnterEvent>(x => mouse_in = false);

            if (Input.GetMouseButton(0))
            {
                UnityEngine.Debug.Log("Clicked!");

                /*.Debug.Log(Input.mousePosition.x + " " + entity_inspector.transform.position.x + " " + entity_inspector.transform.position.x +
                    entity_inspector.transform.scale.x);*/
                if (mouse_in)
                {
                    MoveVisualElement(entity_inspector, Input.mousePosition);
                }
                /*if (Input.mousePosition.x >= entity_inspector.transform.position.x && Input.mousePosition.x <= (entity_inspector.transform.position.x +
                    entity_inspector.transform.scale.x))
                {
                    if (Input.mousePosition.y >= entity_inspector.transform.position.y && Input.mousePosition.y <= (entity_inspector.transform.position.y +
                    entity_inspector.transform.scale.y))
                    {
                        MoveVisualElement(entity_inspector, Input.mousePosition);
                    }
                }*/
            }
            else 
            {
                //UnityEngine.Debug.Log("Not Clicked!");
            }
        }
        else
        {
            entity_inspector.style.display = DisplayStyle.None;
        }

        entity_inspector.RegisterCallback<DropEvent>(evt =>
        UnityEngine.Debug.Log($"{evt.target} dropped on {evt.droppable}"));

        //functionality
        selector.RegisterValueChangedCallback<string>(SelectedModel);
        if(target_mesh != null)
        {
            vertex_count.text = "Vertex Count: " + target_mesh.vertexCount;
            //targeted_entity.GetComponent<MeshFilter>().mesh.r
            //target_mesh.
        }
        //Vector3 cam_pos = (player.transform.position.x, player.transform.position.y - 1000, player.transform.position.z)
        view_cam.transform.position =  new Vector3(player.transform.position.x, player.transform.position.y - 1000, player.transform.position.z);
        targeted_entity.transform.position = new Vector3(view_cam.transform.position.x, view_cam.transform.position.y, view_cam.transform.position.z + 5);
        targeted_entity.transform.rotation = Quaternion.Euler(rotate_y_slider.value, rotate_x_slider.value, targeted_entity.transform.rotation.z);
    }


    private void SelectedModel(ChangeEvent<string> evt)
    {
        UnityEngine.Debug.Log(selector.value.ToString());
        string target_model = selector.value;
        if(GameObject.Find(target_model).GetComponent<MeshFilter>() != null)
        {
            target_mesh_filter = GameObject.Find(target_model).GetComponent<MeshFilter>();
            if(target_mesh_filter.mesh)
            {
                target_mesh = target_mesh_filter.mesh;
                targeted_entity.GetComponent<MeshFilter>().mesh = target_mesh;
                targeted_entity.GetComponent<Renderer>().material = GameObject.Find(target_model).GetComponent<Renderer>().material;
            }
            else
            {
                UnityEngine.Debug.Log("Selected GameObject has no mesh!");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Selected GameObject has no mesh filter component!");
        }
    }

    void UpdateCPUUsage()
    {
        var last_cpu_time = new TimeSpan(0);

        while(true)
        {
            var cpu_time = new TimeSpan(0);

            //get list of all running processes
            var all_processes = Process.GetProcesses();

            //sum all total processor time of all running processes
            cpu_time = all_processes.Aggregate(cpu_time, (current, process) => current + process.TotalProcessorTime);

            //get diff between total sum of processor times and last time we called this
            var new_cpu_time = cpu_time - last_cpu_time;

            //update value of last_cpu_time
            last_cpu_time = cpu_time;

            // The value we look for is the difference, so the processor time all processes together used
            // since the last time we called this divided by the time we waited
            // Then since the performance was optionally spread equally over all physical CPUs
            // we also divide by the physical CPU count
            cpu_usage = 100f * (float)new_cpu_time.TotalSeconds / polling_time / processor_count;

            //wait for update interval
            Thread.Sleep(Mathf.RoundToInt(polling_time * 1000));
        }
    }

    int GetFPS()
    {
        time += Time.deltaTime;

        frame_count++;

        if(time >= polling_time)
        {
            frame_rate = Mathf.RoundToInt(1.0f / Time.unscaledDeltaTime);

            time -= polling_time;
        }

        return frame_rate;
    }
}
