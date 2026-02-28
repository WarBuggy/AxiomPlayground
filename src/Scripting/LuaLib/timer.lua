-- ============================================
-- Engine Utility — Timer
-- Frame/time-based scheduling. Auto-ticked by the
-- engine core loop — no manual wiring needed.
--
-- Usage:
--   Timer.after(2.0, function() print("2s later") end)
--   Timer.frames(60, function() print("60 frames later") end)
--   Timer.every(1.0, function() print("each second") end)
--   Timer.every(0.5, function() print("5 times") end, 5)
--   local h = Timer.after(3, fn)  Timer.cancel(h)
-- ============================================

Timer = {}

local entries = {}
local nextId = 1

local function add(entry)
    local id = nextId
    nextId = nextId + 1
    entry.id = id
    entries[id] = entry
    return id
end

--- Execute callback after `delay` seconds.
function Timer.after(delay, callback)
    return add({
        type = "time",
        remaining = delay,
        callback = callback,
        repeating = false,
    })
end

--- Execute callback after `count` frames.
function Timer.frames(count, callback)
    return add({
        type = "frames",
        remaining = count,
        callback = callback,
        repeating = false,
    })
end

--- Execute callback every `interval` seconds.
--- Optional `count` limits repetitions (nil = infinite).
function Timer.every(interval, callback, count)
    return add({
        type = "time",
        remaining = interval,
        interval = interval,
        callback = callback,
        repeating = true,
        limit = count,
        executed = 0,
    })
end

--- Cancel a scheduled timer by handle.
function Timer.cancel(handle)
    entries[handle] = nil
end

--- Cancel all timers.
function Timer.clear()
    entries = {}
end

--- Tick all timers. Call once per frame with delta time.
function Timer.update(dt)
    local toRemove = {}

    for id, e in pairs(entries) do
        if e.type == "frames" then
            e.remaining = e.remaining - 1
            if e.remaining <= 0 then
                e.callback()
                toRemove[#toRemove + 1] = id
            end
        else
            e.remaining = e.remaining - dt
            if e.remaining <= 0 then
                e.callback()
                if e.repeating then
                    e.executed = e.executed + 1
                    if e.limit and e.executed >= e.limit then
                        toRemove[#toRemove + 1] = id
                    else
                        e.remaining = e.remaining + e.interval
                    end
                else
                    toRemove[#toRemove + 1] = id
                end
            end
        end
    end

    for _, id in ipairs(toRemove) do
        entries[id] = nil
    end
end

--- Number of active timers.
function Timer.count()
    local n = 0
    for _ in pairs(entries) do n = n + 1 end
    return n
end

-- Auto-register with the engine core loop
__engineCoreRegister(function(dt)
    Timer.update(dt)
end)
