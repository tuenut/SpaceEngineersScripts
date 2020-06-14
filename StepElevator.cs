public class ProgramKlass { // для вставки в программируемый блок в игре, убрать эту строку и последнюю
    /*
    OBJECT_NAME_PREFIX - имя лифта, используется как префикс ко всем значимым блокам
        имена блоков пормируются по шаблону $"{OBJECT_NAME_PREFIX}.имя_блока.опция"
    Имена коннекторов со стороны базы, т.е. коннекторов, которые составляют шахту лифта, ожидаются в формате $"имя_базы.имя_блока.NNN", 
        где NNN - порядковый номер, который используется для вычисления этажа (Например, "base.conn.000",  "base.conn.001",  "base.conn.-001").
        Этажом считается любая позиция, где дифт может оказаться в состоянии STAGE_DOCKED или STAGE_DOCKED_BETWEEN.
        Номер этажа хранится в current_level
    MAX_LEVEL - крайний верхний этаж, после которого должна быть инициирована остановка
    MIN_LEVEL - крайний нижний этаж, после которого должна быть инициирована остановка
    current_level - текущий этаж, последний определенный (пока определеяется по верхнему коннектору)
    */

public const string OBJECT_NAME_PREFIX = "Elevator";

public const MyShipConnectorStatus CONNECTED = MyShipConnectorStatus.Connected;
public const MyShipConnectorStatus DISCONNECTED = MyShipConnectorStatus.Unconnected;
public const MyShipConnectorStatus CONNECTABLE = MyShipConnectorStatus.Connectable;

public static string TOP_CONNECTOR_NAME = $"{OBJECT_NAME_PREFIX}.connector.top";
public static string MIDDLE_CONNECTOR_NAME = $"{OBJECT_NAME_PREFIX}.connector.middle";
public static string BOTTOM_CONNECTOR_NAME = $"{OBJECT_NAME_PREFIX}.connector.bottom";
public static string TOP_PISTON = $"{OBJECT_NAME_PREFIX}.piston.top";
public static string BOTTOM_PISTON = $"{OBJECT_NAME_PREFIX}.piston.bottom";
public static string PROGRAMMABLE_BLOCK_NAME = $"{OBJECT_NAME_PREFIX}.program";
public static string BLOCK_WITH_TEXT_SURFACE_PATTERN = $"{OBJECT_NAME_PREFIX}*text_output";

public const float PISTON_VELOCITY = 2.5f;

// TODO сделать калибровку положения и проверку настроек
public const float PISTON_MAX_DISTANCE = 9.835f;
public const float PISTON_MIN_DISTANCE = 1.1f;

public const string FONT_FAMILY = "Monospace";
public const float FONT_SIZE = 0.7f;

public static int current_level;
public const int MAX_LEVEL = 0;
public const int MIN_LEVEL = -3;

public const string EVENT_MOVING_UP = "MOVING_UP";
public const string EVENT_MOVING_DOWN = "MOVING_DOWN";
public const string EVENT_MOVING_STOP = "MOVING_STOP";

public List<PistonStatus> PISTON_INPROCESS_STATUSES = new List<PistonStatus> { PistonStatus.Extending, PistonStatus.Retracting };

public Dictionary<string, string> State = new Dictionary<string, string> { { $"{TOP_CONNECTOR_NAME}.status", "" },
    { $"{MIDDLE_CONNECTOR_NAME}.status", "" },
    { $"{BOTTOM_CONNECTOR_NAME}.status", "" },
    { $"{TOP_CONNECTOR_NAME}.level", "" },
    { $"{MIDDLE_CONNECTOR_NAME}.level", "" },
    { $"{BOTTOM_CONNECTOR_NAME}.level", "" },
    { "stage", "" },
};

public static int sequence_index;
public static int calculated_sequence_index;

public List<string> Events = new List<string>(0);

public static IMyShipConnector top_connector;
public static IMyShipConnector middle_connector;
public static IMyShipConnector bottom_connector;
public static IMyPistonBase top_piston;
public static IMyPistonBase bottom_piston;

public Dictionary<string, IMyShipConnector> connectors = new Dictionary<string, IMyShipConnector>();
public static IMyProgrammableBlock programmable_block;
public static List<IMyTextSurface> text_output_surfaces;

public const string STAGE_DOCKED = "DOCKED";
public const string STAGE_BOTTOM_CONNECTED = "BOTTOM_CONNECTED";
public const string STAGE_RETRACTING = "RETRACTING";
public const string STAGE_READY_TO_DOCK_BETWEEN = "READY_TO_DOCK_BETWEEN";
public const string STAGE_DOCKED_BETWEEN = "DOCKED_BETWEEN";
public const string STAGE_TOP_CONNECTED = "TOP_CONNECTED";
public const string STAGE_EXTENDING = "EXTENDING";
public const string STAGE_READY_TO_DOCK = "READY_TO_DOCK";
public const string STAGE_STOP = "STAGE_STOP";

public delegate bool CheckDelegate();
public delegate void Action();

public static List<MyTuple<string, Action>> sequence;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {}

public void Main(string argument, UpdateType updateSource) {
    Init();
    HandleEvent(argument);
    if(Events.Count > 0){
        HandleEventsQueue();
    }
}

public void Init() {
    top_connector = (IMyShipConnector) GridTerminalSystem.GetBlockWithName(TOP_CONNECTOR_NAME);
    middle_connector = (IMyShipConnector) GridTerminalSystem.GetBlockWithName(MIDDLE_CONNECTOR_NAME);
    bottom_connector = (IMyShipConnector) GridTerminalSystem.GetBlockWithName(BOTTOM_CONNECTOR_NAME);

    connectors = new Dictionary<string, IMyShipConnector> { { TOP_CONNECTOR_NAME, top_connector },
        { MIDDLE_CONNECTOR_NAME, middle_connector },
        { BOTTOM_CONNECTOR_NAME, bottom_connector }
    };

    top_piston = (IMyPistonBase) GridTerminalSystem.GetBlockWithName(TOP_PISTON);
    bottom_piston = (IMyPistonBase) GridTerminalSystem.GetBlockWithName(BOTTOM_PISTON);

    programmable_block = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName(PROGRAMMABLE_BLOCK_NAME);

    top_piston.MaxLimit = PISTON_MAX_DISTANCE;
    top_piston.MinLimit = PISTON_MIN_DISTANCE;
    bottom_piston.MaxLimit = PISTON_MAX_DISTANCE;
    bottom_piston.MinLimit = PISTON_MIN_DISTANCE;

    SetupDisplay();
    GetStatus();
    PrintStatus();
}


public void HandleEvent(string event_) {
    if (!Events.Contains(event_)) {
        switch (event_) {
            case EVENT_MOVING_STOP:
                Events = new List<string> { event_ };
                break;
            case EVENT_MOVING_UP:
            case EVENT_MOVING_DOWN:
                if (Events.Count > 0) {
                    Events.Insert(1, event_);
                    Events = new List<string> { Events[0], Events[1] };
                }
                else {
                    Events.Add(event_);
                }

                break;
        }
    }
}

public void HandleEventsQueue() {
    MyTuple<string, Action> action_;

    switch (Events[0]) {
        case EVENT_MOVING_DOWN:
            if (current_level > MIN_LEVEL) {
                PrintLine("Exec <ElevatorMoveDown>.");
                sequence = new List<MyTuple<string, Action>> {
                    new MyTuple<string, Action>(STAGE_DOCKED, ActionUndock),
                    new MyTuple<string, Action>(STAGE_BOTTOM_CONNECTED, ActionRetractPistons),
                    new MyTuple<string, Action>(STAGE_RETRACTING, ActionPass),
                    new MyTuple<string, Action>(STAGE_READY_TO_DOCK_BETWEEN, ActionDock),
                    new MyTuple<string, Action>(STAGE_DOCKED_BETWEEN, ActionUndock),
                    new MyTuple<string, Action>(STAGE_TOP_CONNECTED, ActionExtendPistons),
                    new MyTuple<string, Action>(STAGE_EXTENDING, ActionPass),
                    new MyTuple<string, Action>(STAGE_READY_TO_DOCK, ActionDock),
                };
                action_ = GetSequencedAction();
            }
            else {
                goto case EVENT_MOVING_STOP;
            }
            break;

        case EVENT_MOVING_UP:
            if (current_level < MAX_LEVEL) {
                PrintLine("Exec <ElevatorMoveUp>.");
                sequence = new List<MyTuple<string, Action>> {
                    new MyTuple<string, Action>(STAGE_DOCKED, ActionUndock),
                    new MyTuple<string, Action>(STAGE_TOP_CONNECTED, ActionRetractPistons),
                    new MyTuple<string, Action>(STAGE_RETRACTING, ActionPass),
                    new MyTuple<string, Action>(STAGE_READY_TO_DOCK_BETWEEN, ActionDock),
                    new MyTuple<string, Action>(STAGE_DOCKED_BETWEEN, ActionUndock),
                    new MyTuple<string, Action>(STAGE_BOTTOM_CONNECTED, ActionExtendPistons),
                    new MyTuple<string, Action>(STAGE_EXTENDING, ActionPass),
                    new MyTuple<string, Action>(STAGE_READY_TO_DOCK, ActionDock),
                };
                action_ = GetSequencedAction();
            }
            else {
                PrintLine($"Current level({current_level}) > max{MAX_LEVEL}");
                goto case EVENT_MOVING_STOP;
            }
            break;

        case EVENT_MOVING_STOP:
            PrintLine("Exec <ElevatorStop>.");
            action_ = new MyTuple<string, Action>(STAGE_STOP, ActionStopPistons);
            break;
        default:
            return;
    }

    PrintLine($"Job found! <{Events[0]}>");

    DispatchAction(action_);

}

// TODO добавить инициализацию LCD
// TODO вообще, если делать реализацию для выборочного множества IMyTextSurface, то лучше делать отдельный скрипт для самой панели и запускать его на ней.
public void SetupDisplay() {
    List<IMyTextSurfaceProvider> text_providers = new List<IMyTextSurfaceProvider>();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(text_providers);
    text_providers.RemoveAll(_TextSurfacesOutputPredicate);

    text_output_surfaces = new List<IMyTextSurface>();
    foreach (IMyTextSurfaceProvider provider in text_providers){
        for (int i = 0; i < provider.SurfaceCount; i++){
            IMyTextSurface surf = provider.GetSurface(i);
            if (surf.ContentType == ContentType.TEXT_AND_IMAGE){
                text_output_surfaces.Add(surf);
            }
        }
    }

    foreach (IMyTextSurface surf in text_output_surfaces)
    {
        surf.Font = FONT_FAMILY;
        surf.FontSize = FONT_SIZE;
        surf.WriteText("", false);
    }
}

public bool _TextSurfacesOutputPredicate(IMyTextSurfaceProvider provider){
    IMyTerminalBlock block = (IMyTerminalBlock) provider;
    string name = block.CustomName;
    string prefix = BLOCK_WITH_TEXT_SURFACE_PATTERN.Split('*')[0];
    string postfix = BLOCK_WITH_TEXT_SURFACE_PATTERN.Split('*')[1];
    
    return !(name.StartsWith(prefix) && name.EndsWith(postfix));
}

public void GetStatus() {
    foreach (IMyShipConnector connector in connectors.Values) {
        State[$"{connector.CustomName}.status"] = connector.Status.ToString();

        if (connector.Status == MyShipConnectorStatus.Connected) {
            State[$"{connector.CustomName}.level"] = connector.OtherConnector.CustomName;
        }
    }

    State["stage"] = GetCurrentStage();

    if (top_connector.Status == CONNECTED) {
        bool success = Int32.TryParse(State[$"{TOP_CONNECTOR_NAME}.level"].Split('.') [2], out current_level);
        PrintLine($"Get current level is {success}: {current_level}");
    }
}

public void PrintStatus() {
    // foreach (string connector_name in connectors.Keys)
    // {
    //     string short_name = connector_name.Split('.')[2];
    //     string status = "<" + State[$"{connector_name}.status"] + ">";
    //     string connector_last_level = State[$"{connector_name}.level"] == "" ? "Unknown" : State[$"{connector_name}.level"].Split('.')[2];
    //     connector_last_level = $"({connector_last_level})";

    //     PrintLine($"{short_name,-6}:    {status} {connector_last_level,8}");
    // }

    var current_event = Events.Count > 0 ? Events[0] : "None";
    PrintLine($"Current level <{current_level.ToString()}>");
    PrintLine($"Current event <{current_event}>");
    PrintLine($"Current sqeuence <{sequence_index}>");
    PrintLine("");

    string event_queue = "";
    foreach (string e in Events) {
        event_queue += $"    <{e}>,";
    }
    PrintLine($"Queue [{Events.Count}]:");
    PrintLine(event_queue);

    PrintLine($"Current stage: <" + State["stage"] + ">");
}

public void PrintLine(string line) {
    foreach (IMyTextSurface surf in text_output_surfaces){
        surf.WriteText(line + "\n", true);
    }
}

public MyTuple<string, Action> GetSequencedAction() {
    calculated_sequence_index = sequence.IndexOf(sequence.Find(_CurrentStagePredicate));

    if (sequence_index == -1) {
        PrintLine($"Reset seq index to <{calculated_sequence_index}>");
        if (calculated_sequence_index == -1) {
            PrintLine($"Cant calculete current sequence.>");
            PrintLine($"Try find FALLBACK.>");
            calculated_sequence_index = sequence.IndexOf(sequence.Find(_FallbackStagePredicate));
            PrintLine($"FALLBACK found at {calculated_sequence_index}.>");
        }
        sequence_index = calculated_sequence_index;
    }

    return sequence[sequence_index];
}

public void DispatchAction(MyTuple<string, Action> action) {
    PrintLine($"Dispatch Action <{action.Item1}>");
    action.Item2();

    if (action.Item2 == ActionStopPistons) {
        sequence_index = -1;
        Events.Clear();
    }
    else if (action.Item2 == ActionPass){
        if (sequence_index != calculated_sequence_index){
            sequence_index = calculated_sequence_index;
        }
    }
    else {
        sequence_index++;

        if (sequence_index >= sequence.Count - 1) {
            sequence_index = 0;
        }
    }
}

public void ActionPass() {
    PrintLine($"Do Action <ActionPass>");
}

// TODO надо переработать
public void ActionUndock() {    
    PrintLine($"Do Action <ActionUndock>");

    PrintLine("Undocking...");
    switch (State["stage"]) {
        case STAGE_DOCKED:
            if (Events[0] == EVENT_MOVING_DOWN && bottom_connector.Status == CONNECTED) {
                PrintLine("Disconnect TOP, MID.");
                top_connector.Disconnect();
                middle_connector.Disconnect();
            }
            else if(Events[0] == EVENT_MOVING_UP && top_connector.Status == CONNECTED){
                middle_connector.Disconnect();
                bottom_connector.Disconnect();

            }
            else {
                PrintLine("<Error while Trying undock>");
            }
            break;
        case STAGE_DOCKED_BETWEEN:
            if (Events[0] == EVENT_MOVING_DOWN && top_connector.Status == CONNECTED) {
                PrintLine("Disconnect MID, BOT.");
                bottom_connector.Disconnect();
            }
            else if(Events[0] == EVENT_MOVING_UP && bottom_connector.Status == CONNECTED){
                top_connector.Disconnect();
            }
            else {
                PrintLine("<Error while Trying undock>");
            }
            break;
    }
}

public void ActionDock() {
    PrintLine($"Do Action <ActionDock>");

    foreach (IMyShipConnector connector in connectors.Values) {
        connector.Connect();
    }
}

public void ActionRetractPistons() {
    PrintLine($"Do Action <ActionRetractPistons>");

    top_piston.Velocity = PISTON_VELOCITY;
    bottom_piston.Velocity = PISTON_VELOCITY;

    top_piston.Retract();
    bottom_piston.Retract();
}

public void ActionExtendPistons() {
    PrintLine($"Do Action <ActionExtendPistons>");

    top_piston.Velocity = PISTON_VELOCITY;
    bottom_piston.Velocity = PISTON_VELOCITY;

    top_piston.Extend();
    bottom_piston.Extend();
}

public void ActionStopPistons() {
    PrintLine($"Do Action <ActionStopPistons>");

    top_piston.Velocity = 0;
    bottom_piston.Velocity = 0;
}

public string GetCurrentStage() {
    var checkers = new Dictionary<string, CheckDelegate> { 
        { STAGE_DOCKED, IsElevatorInStageDocked },
        { STAGE_TOP_CONNECTED, IsElevatorInStageTopConnected },
        { STAGE_BOTTOM_CONNECTED, IsElevatorInStageBottomConnected },
        { STAGE_DOCKED_BETWEEN, IsElevatorInStageDockedBetween },
        { STAGE_EXTENDING, IsElevatorInStageExtending },
        { STAGE_RETRACTING, IsElevatorInStageRetracting },
        { STAGE_READY_TO_DOCK_BETWEEN, IsElevatorInStageReadyToDockBetween },
        { STAGE_READY_TO_DOCK, IsElevatorInStageReadyToDock },
    };

    string result = "Unknown";

    foreach (string stage in checkers.Keys) {
        if (checkers[stage]()) {
            result = stage;
            break;
        }
    }

    return result;
}

public bool IsElevatorInStageDocked() {
    // all connectors are connected. Pistons are not important.
    return top_connector.Status == CONNECTED
            && middle_connector.Status == CONNECTED
            && bottom_connector.Status == CONNECTED;
}

public bool IsElevatorInStageTopConnected() {
    // only top connector connected, pistons are stopped.
    return top_connector.Status == CONNECTED
            && middle_connector.Status == DISCONNECTED
            && bottom_connector.Status == DISCONNECTED
            && IsPistonsStopped();
}

public bool IsElevatorInStageBottomConnected() {
    // Only bottom_connector is CONNECTED, pistons are stopped.
    return top_connector.Status == DISCONNECTED
            && middle_connector.Status == DISCONNECTED
            && bottom_connector.Status == CONNECTED
            && IsPistonsStopped();
}

public bool IsElevatorInStageDockedBetween() {
    // Top and bottom connectors connected.
    return top_connector.Status == CONNECTED
            && middle_connector.Status == DISCONNECTED
            && bottom_connector.Status == CONNECTED;
}

public bool IsPistonsStopped() {
    return !PISTON_INPROCESS_STATUSES.Contains(top_piston.Status) && !PISTON_INPROCESS_STATUSES.Contains(bottom_piston.Status);
}

public bool IsElevatorInStageRetracting() {
    return top_piston.Status == PistonStatus.Retracting || bottom_piston.Status == PistonStatus.Retracting;
}

public bool IsElevatorInStageExtending() {
    return top_piston.Status == PistonStatus.Extending || bottom_piston.Status == PistonStatus.Extending;
}

public bool IsElevatorInStageReadyToDockBetween() {
    // One outside connector connected, other connectable and middle disconnected. Pistons are stopped.
    return middle_connector.Status == DISCONNECTED && IsOuterConnectorsConnectable() && IsPistonsStopped();
}

public bool IsElevatorInStageReadyToDock() {
    return middle_connector.Status == CONNECTABLE && IsOuterConnectorsConnectable() && IsPistonsStopped();
}

public bool IsOuterConnectorsConnectable() {
    return (top_connector.Status == CONNECTED && bottom_connector.Status == CONNECTABLE) || (top_connector.Status == CONNECTABLE && bottom_connector.Status == CONNECTED);
}

public bool _CurrentStagePredicate(MyTuple<string, Action> stage) {
    return stage.Item1 == State["stage"];
}

public bool _FallbackStagePredicate(MyTuple<string, Action> stage) {
    return stage.Item2 == ActionRetractPistons || stage.Item2 == ActionExtendPistons;
}

}
