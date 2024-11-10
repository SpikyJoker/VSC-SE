// Component Names
string topPistonName = "[MINE] Piston Top";
string grabPistonName = "[MINE] Piston Grab";
string welderGroupName = "[MINE] Welder Top";
string drillProjectorName = "[MINE] Projector Top";
string conveyorProjectorName = "[MINE] Projector Conveyor";
string topMergeBlockName = "[MINE] Merge Top";
string grabMergeBlockName = "[MINE] Merge Grab";
string lcdScreenName = "[MINE] LCD Screen";


// Declare the controller instances at the class level
PistonController pistonController;
MergeController mergeController;
ProjectionController projectionController;
WelderController welderController;

// Class-level fields for components
IMyPistonBase topPiston;
IMyPistonBase grabPiston;
IMyBlockGroup welderGroup;
IMyProjector projector;
IMyProjector conveyorProjector;
IMyShipMergeBlock topMergeBlock;
IMyShipMergeBlock grabMergeBlock;
IMyTextPanel lcdScreen;

// Action Speed Settings
float topPistonSpeed = 5.0f;
float grabPistonSpeed = 2.0f;
float pistonTopExtendLimit = 9.9f;
float pistonTopConnectLimit = 2.5f;
float pistonRetractSpeed = 5.0f;
float pistonGrabExtendLimit = 2.3f;
float drillPistonSpeed = 0.1f;


// State and Action Mappings
string errorMessage = "";
bool running = false;
string direction;

// Interfaces
public interface IPistonController
{
    void ExtendPiston(IMyPistonBase piston, float limit, float speed);
    void RetractPiston(IMyPistonBase piston, float speed);
}

public interface IMergeController
{
    bool CheckMergeEngaged(IMyShipMergeBlock mergeBlock);
    void ToggleMergeBlock(IMyShipMergeBlock mergeBlock, bool status);
}

public interface IProjectionController
{
    bool IsProjectionComplete(IMyProjector projector);
    void EnableProjector(IMyProjector projector, bool status);
}

public interface IWelderController
{
    void ToggleWelders(IMyBlockGroup welderGroup, bool status);
}

// Service Implementations
public class PistonController : IPistonController
{
    private Program _program;

    public PistonController(Program program)
    {
        _program = program;
    }

    public void ExtendPiston(IMyPistonBase piston, float limit, float speed)
    {
        piston.MaxLimit = limit;
        piston.Velocity = speed;
        _program.WriteToScreen($"Extending piston {piston.CustomName} to {limit}m at {speed}m/s");
    }

    public void RetractPiston(IMyPistonBase piston, float speed)
    {
        piston.Velocity = -speed;
        _program.WriteToScreen($"Retracting piston {piston.CustomName} at {speed}m/s");
    }
}

public class MergeController : IMergeController
{
    private Program _program;

    public MergeController(Program program)
    {
        _program = program;
    }

    public bool CheckMergeEngaged(IMyShipMergeBlock mergeBlock)
    {
        bool engaged = !mergeBlock.IsConnected;
        _program.WriteToScreen($"Merge block {mergeBlock.CustomName} is {(engaged ? "engaged" : "disengaged")}");
        return engaged;
    }

    public void ToggleMergeBlock(IMyShipMergeBlock mergeBlock, bool status)
    {
        mergeBlock.Enabled = status;
        _program.WriteToScreen($"Merge block {mergeBlock.CustomName} is now {(status ? "engaged" : "disengaged")}");
    }
}

public class ProjectionController : IProjectionController
{
    private Program _program;

    public ProjectionController(Program program)
    {
        _program = program;
    }

    public bool IsProjectionComplete(IMyProjector projector)
    {
        bool isComplete = projector.IsProjecting && projector.RemainingBlocks == 0;
        _program.WriteToScreen($"Projection on {projector.CustomName} is complete: {isComplete}");
        return isComplete;
    }

    public void EnableProjector(IMyProjector projector, bool status)
    {
        projector.Enabled = status;
        _program.WriteToScreen($"Projector {projector.CustomName} is now {(status ? "enabled" : "disabled")}");
    }
}

public class WelderController : IWelderController
{
    private Program _program;

    public WelderController(Program program)
    {
        _program = program;
    }

    public void ToggleWelders(IMyBlockGroup welderGroup, bool status)
    {
        // Ensure welderGroup is not null
        if (welderGroup != null)
        {
            List<IMyTerminalBlock> welders = new List<IMyTerminalBlock>();
            welderGroup.GetBlocks(welders);  // Populate welders list with blocks in the group

            // Iterate through the blocks and enable/disable them
            foreach (IMyTerminalBlock block in welders)
            {
                IMyShipWelder welder = block as IMyShipWelder;
                if (welder != null)  // Check if block is a welder
                {
                    welder.Enabled = status;  // Enable or disable the welder
                }
            }

            _program.WriteToScreen($"Welders in group {welderGroup.Name} are now {(status ? "enabled" : "disabled")}");
        }
        else
        {
            _program.WriteToScreen($"No group found with name {welderGroup.Name}");
        }
    }
}


// Program Setup
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100; // Update every 100 ticks
    

    // Initialize controllers with reference to this Program instance
    lcdScreen = GridTerminalSystem.GetBlockWithName(lcdScreenName) as IMyTextPanel;
    InitializeSystem();
    Echo($"lcdScreen is {(lcdScreen != null ? "initialized" : "null")}");

}

// Main Entry Point
public void Main(string argument)
{
    
    WriteToScreen(""); //writes an empty string to keep the status

    if (argument == "Dig")
    {
        running = true;
        direction = "Dig";
    }
    else if (argument == "Retract")
    {
        running = true;
        direction = "Retract";
    }
    else if (argument == "Stop")
    {
        StopAll();
        running = false;
    }
    else if (argument == "ResetLCD")
    {
        WriteToScreen("reset");
        running=false;
    }

    if (running){
        if (direction == "Dig")
        {
            WriteToScreen("Digging1");
            if (mergeController.CheckMergeEngaged(grabMergeBlock))
            {
                projectionController.EnableProjector(conveyorProjector, true);
                WriteToScreen("Digging2");
                welderController.ToggleWelders(welderGroup, true);
                WriteToScreen("Digging3");
                mergeController.ToggleMergeBlock(topMergeBlock, true);

                // Instead of a blocking while loop, use a simple state check
                if (projectionController.IsProjectionComplete(conveyorProjector))
                {
                    welderController.ToggleWelders(welderGroup, false);
                    projectionController.EnableProjector(conveyorProjector, false);

                    pistonController.ExtendPiston(topPiston, pistonTopConnectLimit, drillPistonSpeed);
                    mergeController.ToggleMergeBlock(grabMergeBlock, false);
                    pistonController.ExtendPiston(topPiston, pistonTopExtendLimit, drillPistonSpeed);
                    mergeController.ToggleMergeBlock(topMergeBlock, false);
                    pistonController.RetractPiston(topPiston, pistonRetractSpeed);
                }
            }
        }
        else
        {
            projectionController.EnableProjector(projector, true);
            welderController.ToggleWelders(welderGroup, true);
            mergeController.ToggleMergeBlock(topMergeBlock, true);

            if (projectionController.IsProjectionComplete(projector))
            {
                welderController.ToggleWelders(welderGroup, false);
                projectionController.EnableProjector(projector, false);

                pistonController.ExtendPiston(topPiston, pistonTopExtendLimit, topPistonSpeed);
                pistonController.ExtendPiston(grabPiston, pistonGrabExtendLimit, grabPistonSpeed);
                mergeController.ToggleMergeBlock(topMergeBlock, false);
                pistonController.RetractPiston(topPiston, pistonRetractSpeed);
            }
        }
    }
}
// Initialize System
void InitializeSystem()
{
    try
    {
        topPiston = GridTerminalSystem.GetBlockWithName(topPistonName) as IMyPistonBase;
        Echo($"Top Piston: {(topPiston != null ? "found" : "not found")}");
        if (topPiston == null) throw new Exception("Top Piston not found.");

        grabPiston = GridTerminalSystem.GetBlockWithName(grabPistonName) as IMyPistonBase;
        Echo($"Grab Piston: {(grabPiston != null ? "found" : "not found")}");
        if (grabPiston == null) throw new Exception("Grab Piston not found.");

        welderGroup = GridTerminalSystem.GetBlockGroupWithName(welderGroupName);
        Echo($"Welder Group: {(welderGroup != null ? "found" : "not found")}");
        if (welderGroup == null) throw new Exception("Welder Group not found.");

        projector = GridTerminalSystem.GetBlockWithName(drillProjectorName) as IMyProjector;
        Echo($"Drill Projector: {(projector != null ? "found" : "not found")}");
        if (projector == null) throw new Exception("Drill Projector not found.");

        conveyorProjector = GridTerminalSystem.GetBlockWithName(conveyorProjectorName) as IMyProjector;
        Echo($"Conveyor Projector: {(conveyorProjector != null ? "found" : "not found")}");
        if (conveyorProjector == null) throw new Exception("Conveyor Projector not found.");

        topMergeBlock = GridTerminalSystem.GetBlockWithName(topMergeBlockName) as IMyShipMergeBlock;
        Echo($"Top Merge Block: {(topMergeBlock != null ? "found" : "not found")}");
        if (topMergeBlock == null) throw new Exception("Top Merge Block not found.");

        grabMergeBlock = GridTerminalSystem.GetBlockWithName(grabMergeBlockName) as IMyShipMergeBlock;
        Echo($"Grab Merge Block: {(grabMergeBlock != null ? "found" : "not found")}");
        if (grabMergeBlock == null) throw new Exception("Grab Merge Block not found.");

        lcdScreen = GridTerminalSystem.GetBlockWithName(lcdScreenName) as IMyTextPanel;
        Echo($"LCD Screen: {(lcdScreen != null ? "found" : "not found")}");
        if (lcdScreen == null) throw new Exception("LCD Screen not found.");
    }
    catch (Exception e)
    {
        errorMessage = "System could not be initialized: " + e.Message;
        WriteToScreen(errorMessage);
        Echo(errorMessage); // Echo the error message to the console for debugging
        return; // Stop further execution if initialization fails
    }


    pistonController = new PistonController(this);
    Echo($"PistonController initialized: {pistonController != null}");

    mergeController = new MergeController(this);
    Echo($"MergeController initialized: {mergeController != null}");

    projectionController = new ProjectionController(this);
    Echo($"ProjectionController initialized: {projectionController != null}");

    welderController = new WelderController(this);
    Echo($"WelderController initialized: {welderController != null}");


    // Proceed only if all blocks were found
    WriteToScreen("reset");

    // Verify that each controller is initialized before calling methods on them
    
    mergeController.ToggleMergeBlock(topMergeBlock, false);
    mergeController.ToggleMergeBlock(grabMergeBlock, true);


    pistonController.RetractPiston(topPiston, pistonRetractSpeed);
    mergeController.ToggleMergeBlock(topMergeBlock, true);
    projectionController.EnableProjector(projector, false);
    projectionController.EnableProjector(conveyorProjector, false);
  

    welderController.ToggleWelders(welderGroup, false);




}


// Helper Methods
void WriteToScreen(string message)
{
    int maxLines = 23; // Maximum number of lines to display on the LCD
    string statusText = $"Running: {running}\nDirection: {direction}\n"; // Status lines for the top of the screen

    if (lcdScreen != null)
    {
        // Reset the screen if the message is "reset"
        if (message == "reset")
        {
            lcdScreen.WriteText(statusText, false); // Clear and add only status lines
        }
        else
        {
            // Retrieve current text without the status lines
            string currentText = lcdScreen.GetText();
            var lines = currentText.Split(new[] { '\n' }, StringSplitOptions.None).Skip(3).ToList();

            // If there are more lines than maxLines - 3 (account for status lines), remove the oldest lines
            if (lines.Count >= maxLines - 3)
            {
                lines = lines.Skip(lines.Count - (maxLines - 3) + 1).ToList();
            }

            // Add the new message to the log
            if (!string.IsNullOrEmpty(message))
            {
                lines.Add(message);
            }

            // Combine status lines and message log
            string updatedText = statusText + string.Join("\n", lines);
            lcdScreen.WriteText(updatedText);
        }
    }
    else
    {
        Echo("LCD Screen is not initialized."); // Debugging statement
    }
}


void StopAll()
{
    mergeController.ToggleMergeBlock(topMergeBlock, false);
    mergeController.ToggleMergeBlock(grabMergeBlock, true);
    pistonController.RetractPiston(topPiston, pistonRetractSpeed);
    projectionController.EnableProjector(projector, false);
}
