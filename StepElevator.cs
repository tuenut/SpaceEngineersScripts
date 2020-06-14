public class ProgramKlass
{

public const MyShipConnectorStatus CONNECTED = MyShipConnectorStatus.Connected;
public const MyShipConnectorStatus DISCONNECTED = MyShipConnectorStatus.Unconnected;
public const MyShipConnectorStatus CONNECTABLE = MyShipConnectorStatus.Connectable;

public const string TOP_CONNECTOR_NAME = "connector.platform.top";
public const string MIDDLE_CONNECTOR_NAME = "connector.platform.middle";
public const string BOTTOM_CONNECTOR_NAME = "connector.platform.bottom";
public const string TOP_PISTON = "piston.platform.top";
public const string BOTTOM_PISTON = "piston.platform.bottom";

public const string PROGRAMMABLE_BLOCK_NAME = "program.platform";

public const float PISTON_VELOCITY = 0.5f;

public const string FONT_FAMILY = "Monospace";
public const float FONT_SIZE = 0.7f;

public const string EVENT_MOVING_UP = "MOVING_UP";
public const string EVENT_MOVING_DOWN = "MOVING_DOWN";
public const string EVENT_MOVING_STOP = "MOVING_STOP";

public List<PistonStatus> PISTON_INPROCESS_STATUSES = new List<PistonStatus> {PistonStatus.Extending, PistonStatus.Retracting};

public Dictionary<string, string> State = new Dictionary<string, string> {
    {$"{TOP_CONNECTOR_NAME}.status", ""},
    {$"{MIDDLE_CONNECTOR_NAME}.status", ""},
    {$"{BOTTOM_CONNECTOR_NAME}.status", ""},
    {$"{TOP_CONNECTOR_NAME}.level", ""},
    {$"{MIDDLE_CONNECTOR_NAME}.level", ""},
    {$"{BOTTOM_CONNECTOR_NAME}.level", ""},
    {"level", ""},
    {"stage", ""},
    {"prev_starge", ""}
};

public List<string> Events = new List<string>(0);

public static IMyShipConnector top_connector;
public static IMyShipConnector middle_connector;
public static IMyShipConnector bottom_connector;
public static IMyPistonBase top_piston;
public static IMyPistonBase bottom_piston;

public Dictionary<string, IMyShipConnector> connectors = new Dictionary<string, IMyShipConnector>();
public static IMyProgrammableBlock programmable_block;
public static IMyTextSurface text_surface;

public const string STAGE_DOCKED = "DOCKED";
public const string STAGE_BOTTOM_CONNECTED = "BOTTOM_CONNECTED";
public const string STAGE_RETRACTING = "RETRACTING";
public const string STAGE_READY_TO_DOCK_BETWEEN = "READY_TO_DOCK_BETWEEN";
public const string STAGE_DOCKED_BETWEEN = "DOCKED_BETWEEN";
public const string STAGE_TOP_CONNECTED = "TOP_CONNECTED";
public const string STAGE_EXTENDING = "EXTENDING";
public const string STAGE_READY_TO_DOCK = "READY_TO_DOCK";

public delegate bool CheckDelegate();
public delegate void Action();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {}

public void Main(string argument, UpdateType updateSource)
{
    Init();

    HandleEvents(argument);

    DoElevatorWork();
}

public void Init()
{
    top_connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName(TOP_CONNECTOR_NAME);
    middle_connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName(MIDDLE_CONNECTOR_NAME);
    bottom_connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName(BOTTOM_CONNECTOR_NAME);

    connectors = new Dictionary<string, IMyShipConnector> { 
        {TOP_CONNECTOR_NAME, top_connector}, 
        {MIDDLE_CONNECTOR_NAME, middle_connector}, 
        {BOTTOM_CONNECTOR_NAME, bottom_connector}
    };

    top_piston = (IMyPistonBase)GridTerminalSystem.GetBlockWithName(TOP_PISTON);
    bottom_piston = (IMyPistonBase)GridTerminalSystem.GetBlockWithName(BOTTOM_PISTON);

    programmable_block = (IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName(PROGRAMMABLE_BLOCK_NAME);
    text_surface = (IMyTextSurface)programmable_block.GetSurface(0);


    SetupDisplay();
    GetStatus();
    PrintStatus();
}

public void HandleEvents(string elevator_event)
{
    if (!Events.Contains(elevator_event))
    {
        switch(elevator_event)
        {
            case EVENT_MOVING_STOP:
            case EVENT_MOVING_UP:
            case EVENT_MOVING_DOWN:
                if (Events.Count > 0)
                {
                    Events.Insert(1, elevator_event);
                    Events = new List<string> {Events[0], Events[1]};
                }
                else
                {
                    Events.Add(elevator_event);
                }
                
                break;
        }
    }
}

public void DoElevatorWork()
{
    if (Events.Count > 0)
    {
        PrintLine($"Has work to do! <{Events[0]}>");

        switch (Events[0])
        {
            case EVENT_MOVING_UP:
                ElevatorMoveUp();
                break;
            case EVENT_MOVING_DOWN:
                ElevatorMoveDown();
                break;
            case EVENT_MOVING_STOP:
                ElevatorStop();
                break;
        }
    }
    /* get current_event
       check current state
       if current_work not done: pass
     
       else: check for next_event
       if not next_event: check for continue for current_event
          if can continue: StartWork(current_event, next_stage)
          else: stop (do nothing) 
       else:
          StartWork(current_event, next_stage)
    */
}

public void SetupDisplay()
{
    text_surface.Font = FONT_FAMILY;
    text_surface.FontSize = FONT_SIZE;
    text_surface.WriteText("", false);
}

public void GetStatus()
{
    foreach (IMyShipConnector connector in connectors.Values)
    {
        State[$"{connector.CustomName}.status"] = connector.Status.ToString();

        if (connector.Status == MyShipConnectorStatus.Connected)
        {
            State[$"{connector.CustomName}.level"] = connector.OtherConnector.CustomName;
        }
    }

    State["stage"] = GetCurrentStage();
}

public void PrintStatus()
{
    foreach (string connector_name in connectors.Keys)
    {
        string short_name = connector_name.Split('.')[2];
        string status = "<" + State[$"{connector_name}.status"] + ">";
        string connector_last_level = State[$"{connector_name}.level"] == "" ? "Unknown" : State[$"{connector_name}.level"].Split('.')[2];
        connector_last_level = $"({connector_last_level})";

        PrintLine($"{short_name,-6}:    {status} {connector_last_level,8}");
    }

    var current_event = Events.Count > 0 ? Events[0] : "None";
    PrintLine($"Current event <{current_event}>");

    string event_queue = "";
    foreach (string e in Events)
    {
        event_queue += $"    <{e}>,";
    }
    PrintLine($"Queue [{Events.Count}]:");
    PrintLine(event_queue);

    PrintLine($"Current stage: <" + State["stage"] + ">");
}

public void PrintLine(string line)
{
    text_surface.WriteText(line+"\n", true);
}

public void ElevatorStop()
{
    ActionStopPistons();
    Events.Clear();
}

public void ElevatorMoveUp()
{
    // var GO_UP_SEQUENCE = new Dictionary<string, Action> {
    //     {STAGE_DOCKED},
    //     {STAGE_READY_TO_DOCK},
    //     {STAGE_EXTENDING}, 
    //     {STAGE_TOP_CONNECTED}, 
    //     {STAGE_DOCKED_BETWEEN}, 
    //     {STAGE_READY_TO_DOCK_BETWEEN}, 
    //     {STAGE_RETRACTING}, 
    //     {STAGE_BOTTOM_CONNECTED}
    // };

    // int current_action_index = GO_UP_SEQUENCE.Find(CurrentStagePredicate);
}

public void ElevatorMoveDown()
{
    var GO_DOWN_SEQUENCE = new List<MyTuple<string, Action>> {
        new MyTuple<string, Action>(STAGE_DOCKED, ActionUndock), 
        new MyTuple<string, Action>(STAGE_BOTTOM_CONNECTED, ActionRetractPistons), 
        // {STAGE_RETRACTING}, //waiting next stage
        new MyTuple<string, Action>(STAGE_READY_TO_DOCK_BETWEEN, ActionDock), 
        new MyTuple<string, Action>(STAGE_DOCKED_BETWEEN, ActionUndock), 
        new MyTuple<string, Action>(STAGE_TOP_CONNECTED, ActionExtendPistons), 
        // {STAGE_EXTENDING}, //waiting next stage
        new MyTuple<string, Action>(STAGE_READY_TO_DOCK, ActionDock),
    };
    // TODO: Надо не этой хуйней заниматься, а сделать глобальный индекс последовательности и инкрементить его с учетом перемолнения

    var sequence_step = GO_DOWN_SEQUENCE.Find(_CurrentStagePredicate);
    switch(sequence_step.Item1){
        case STAGE_READY_TO_DOCK_BETWEEN:
        case STAGE_READY_TO_DOCK:
            if (State["prev_starge"] != "docked"){
                GO_DOWN_SEQUENCE[GO_DOWN_SEQUENCE.IndexOf(sequence_step)+1].Item2();
            }
            else {
                sequence_step.Item2();
            }
            break;
        default:
            sequence_step.Item2();
            break;
    }
}

public bool _CurrentStagePredicate(MyTuple<string, Action> stage)
{
    return stage.Item1 == State["stage"];
}

public void ActionUndock(){
    PrintLine("Undocking...");
    switch (GetCurrentStage())
    {
        case STAGE_DOCKED:
            if (Events[0] == EVENT_MOVING_DOWN) {
                if (bottom_connector.Status == CONNECTED){
                    PrintLine("Disconnect TOP, MID.");
                    top_connector.Disconnect();
                    middle_connector.Disconnect();
                }
                else{
                    PrintLine("<Error>; Trying undock while going down, but bottom connector UNCONNECTED.");
                }
            }
            break;
        case STAGE_DOCKED_BETWEEN:
            if (Events[0] == EVENT_MOVING_DOWN) {
                if (top_connector.Status == CONNECTED){
                    PrintLine("Disconnect MID, BOT.");
                    middle_connector.Disconnect();
                    bottom_connector.Disconnect();
                }
                else{
                    PrintLine("<Error>; Trying undock from between while going down, but top connector UNCONNECTED.");
                }
            }
            break;
    }
}

public void ActionDock(){
    foreach (IMyShipConnector connector in connectors.Values) {
        connector.Connect();
    }
    State["prev_starge"] = "docked";    
}

public void ActionRetractPistons()
{
    top_piston.Velocity = PISTON_VELOCITY;
    bottom_piston.Velocity = PISTON_VELOCITY;

    top_piston.Retract();
    bottom_piston.Retract();
    State["prev_starge"] = "moving";
}

public void ActionExtendPistons()
{
    top_piston.Velocity = PISTON_VELOCITY;
    bottom_piston.Velocity = PISTON_VELOCITY;

    top_piston.Extend();
    bottom_piston.Extend();
    State["prev_starge"] = "moving";
}

public void ActionStopPistons()
{
    top_piston.Velocity = 0;
    bottom_piston.Velocity = 0;
}

public string GetCurrentStage()
{
    var checkers = new Dictionary<string, CheckDelegate> {
        {STAGE_DOCKED, IsElevatorInStageDocked},
        {STAGE_BOTTOM_CONNECTED, IsElevatorInStageBottomConnected},
        {STAGE_RETRACTING, IsElevatorInStageRetracting}, 
        {STAGE_READY_TO_DOCK_BETWEEN, IsElevatorInStageReadyToDockBetween}, 
        {STAGE_DOCKED_BETWEEN, IsElevatorInStageDockedBetween}, 
        {STAGE_TOP_CONNECTED, IsElevatorInStageTopConnected},
        {STAGE_EXTENDING, IsElevatorInStageExtending},
        {STAGE_READY_TO_DOCK, IsElevatorInStageReadyToDock},
    };

    string result = "Unknown";

    foreach (string stage in checkers.Keys)
    {
        if (checkers[stage]())
        {
            result = stage;
            break;
        }
    }

    return result;
}

public bool IsElevatorInStageDocked()
{
    // all connectors are connected. Pistons are not important.
    return top_connector.Status == CONNECTED && middle_connector.Status == CONNECTED && bottom_connector.Status == CONNECTED;
}

public bool IsElevatorInStageBottomConnected()
{
    // Only bottom_connector is CONNECTED, pistons are stopped.
    return top_connector.Status == CONNECTABLE && middle_connector.Status == CONNECTABLE && bottom_connector.Status == CONNECTED && IsPistonsStopped();
}

public bool IsElevatorInStageRetracting()
{
    return top_piston.Status == PistonStatus.Retracting || bottom_piston.Status == PistonStatus.Retracting;
}

public bool IsElevatorInStageReadyToDockBetween()
{
    // One outside connector connected, other connectable and middle disconnected. Pistons are stopped.
    return middle_connector.Status == DISCONNECTED && IsOuterConnectorsConnectable() && IsPistonsStopped();
}

public bool IsElevatorInStageDockedBetween()
{
    // Top and bottom connectors connected.
    return top_connector.Status == CONNECTED && middle_connector.Status == DISCONNECTED && bottom_connector.Status == CONNECTED;
}

public bool IsElevatorInStageTopConnected()
{
    // only top connector connected, pistons are stopped.
    return top_connector.Status == CONNECTED && middle_connector.Status == DISCONNECTED && bottom_connector.Status == DISCONNECTED && IsPistonsStopped() && !IsElevatorInStageReadyToDock();
}

public bool IsElevatorInStageExtending()
{
    return top_piston.Status == PistonStatus.Extending || bottom_piston.Status == PistonStatus.Extending;
}

public bool IsElevatorInStageReadyToDock()
{
    return middle_connector.Status == CONNECTABLE && IsOuterConnectorsConnectable() && IsPistonsStopped();
}

public bool IsPistonsStopped()
{
    return !PISTON_INPROCESS_STATUSES.Contains(top_piston.Status) && !PISTON_INPROCESS_STATUSES.Contains(bottom_piston.Status);
}

public bool IsOuterConnectorsConnectable()
{
    return (top_connector.Status == CONNECTED && bottom_connector.Status == CONNECTABLE) || (top_connector.Status == CONNECTABLE && bottom_connector.Status == CONNECTED);
}


}