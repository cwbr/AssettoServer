-- LeaderboardPlugin: reports driver assists & input info to server on lap completion

local lapInfoEvent = ac.OnlineEvent(
    {
        ac.StructItem.key('AS_LB_LapInfo'),
        absLevel = ac.StructItem.byte(),
        tcLevel = ac.StructItem.byte(),
        stabilityControl = ac.StructItem.float(),
        autoShifting = ac.StructItem.boolean(),
        inputMethod = ac.StructItem.byte(),
        tyreCompound = ac.StructItem.byte()
    }, function(sender, message)
        -- Server doesn't send anything back; no-op
    end)

-- Send assist/input state when a lap is completed
function script.lapCompleted()
    lapInfoEvent({
        absLevel = car.absMode,
        tcLevel = car.tractionControlMode,
        stabilityControl = car.stabilityControl,
        autoShifting = car.autoShifting,
        inputMethod = ac.getInputMode(),
        tyreCompound = car.compoundIndex
    })
end
